using CloudStorage.API.V2.Models.DTOs;
using CloudStorage.API.V2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CloudStorage.API.V2.Models;
using CloudStorage.API.V2.Security;
using Microsoft.Extensions.Options;

namespace CloudStorage.API.V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IUserService _userService;
        private readonly SignInManager<User> _signInManager;
        private readonly AppSettings _appSettings;

        public UserController(ILogger<UserController> logger, IUserService userService, SignInManager<User> signInManager, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _userService = userService;
            _signInManager = signInManager;
            _appSettings = appSettings.Value;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody]LoginDTO pLoginDto)
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

                /*
                Microsoft.AspNetCore.Identity.SignInResult loginResult = new Microsoft.AspNetCore.Identity.SignInResult();// await _signInManager.PasswordSignInAsync(user, pLoginDto.Password!, false, false);

                if (!loginResult.Succeeded)
                {
                    return BadRequest();
                }
                */



                TokenDTO token = new TokenDTO();
                token.Expires = DateTime.UtcNow.AddMinutes(60);
                token.Token = TokenUtilities.CreateToken(user, _appSettings, 60);
                token.User = Worker.Convert(user);

                return Ok(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login Failed");
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
