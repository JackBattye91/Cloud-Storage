using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CloudStorage.API.V2.Models.DTOs;
using CloudStorage.API.V2.Models;
using CloudStorage.API.V2.Services;
using Microsoft.AspNetCore.Authorization;
using CloudStorage.API.V2.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CloudStorage.API.V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IUserService _userService;
        private readonly AppSettings _appSettings;
        private readonly SignInManager<User> _signInManager;

        public AccountController(ILogger<AccountController> logger, IUserService userService, IOptions<AppSettings> appSettings, SignInManager<User> signInManager)
        {
            _logger = logger;
            _userService = userService;
            _appSettings = appSettings.Value;
            _signInManager = signInManager;
        }

        [HttpPost]
        //[Authorize(Policy = Consts.Policies.ADMIN)]
        public async Task<IActionResult> CreateAccount(UserDTO pUser)
        {
            try
            {
                /*
                if (!SecurityUtilities.ValidatePassword(pUser.Password))
                {
                    return BadRequest();
                }*/
                User? user = Converter.Convert<UserDTO, User>(pUser);

                if (user == null)
                {
                    throw new Exception("Unable to convert UserDTO to User");
                }

                user.Id = Guid.NewGuid().ToString();
                user.Password = _signInManager.UserManager.PasswordHasher.HashPassword(user, pUser.Password);
                user.Created = DateTime.UtcNow;
                user.Updated = DateTime.UtcNow;
                user = await _userService.CreateAsync(user);
                pUser = Converter.Convert(user) ?? new UserDTO();
                pUser.Password = null;

                return Ok(pUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create Account Failed");
                return StatusCode(500);
            }
        }

        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDTO pLoginDto)
        {
            try
            {
                if (!Validate(pLoginDto))
                {
                    return BadRequest();
                }

                User user;

                if (!string.IsNullOrEmpty(pLoginDto.Username))
                {
                    user = await _userService.GetByUsernameAsync(pLoginDto.Username);
                }
                else if (!string.IsNullOrEmpty(pLoginDto.Email))
                {
                    user = await _userService.GetByEmailAsync(pLoginDto.Email);
                }
                else
                {
                    _logger.LogError("Unable to get user with username or password");
                    return BadRequest();
                }

                Microsoft.AspNetCore.Identity.SignInResult loginResult = await _signInManager.PasswordSignInAsync(user, pLoginDto.Password!, true, false);
                if (!loginResult.Succeeded)
                {
                    return BadRequest();
                }

                RefreshToken refreshToken = await _userService.CreateRefreshTokenAsync(user);

                TokenDTO token = new TokenDTO();
                token.Expires = DateTime.UtcNow.AddMinutes(60);
                token.Token = TokenUtilities.CreateToken(user, _appSettings, 60);
                token.User = Converter.Convert(user);
                token.RefreshToken = refreshToken.Id;

                user.LastLogin = DateTime.UtcNow;
                _ = _userService.UpdateAsync(user);

                return Ok(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login Failed");
                return StatusCode(500);
            }
        }

        [HttpPost]
        [Route("refresh/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromRoute(Name = "token")] string pToken)
        {
            try
            {
                if (await TokenUtilities.ValidateTokenWithoutDate(Request, _appSettings) == false)
                {
                    return Unauthorized();
                }

                string? email = TokenUtilities.GetSubjectEmail(Request);
                User user;
                if (!string.IsNullOrEmpty(email))
                {
                    user = await _userService.GetByEmailAsync(email);
                }
                else
                {
                    _logger.LogError("Unable to get user with email");
                    return BadRequest();
                }

                if (await _userService.ValidationRefreshTokenAsync(email, pToken) == false)
                {
                    return Unauthorized();
                }


                TokenDTO token = new TokenDTO();
                token.Expires = DateTime.UtcNow.AddMinutes(60);
                token.Token = TokenUtilities.CreateToken(user, _appSettings, 60);
                token.User = Converter.Convert(user);

                return Ok(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh Token Failed");
                return StatusCode(500);
            }
        }

        private bool Validate(LoginDTO pLoginDto)
        {
            if ((string.IsNullOrEmpty(pLoginDto.Username) && string.IsNullOrEmpty(pLoginDto.Email)) || string.IsNullOrEmpty(pLoginDto.Password))
            {
                return false;
            }

            return true;
        }
    }
}
