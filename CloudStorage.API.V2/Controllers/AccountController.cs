using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CloudStorage.API.V2.Models.DTOs;
using CloudStorage.API.V2.Models;
using CloudStorage.API.V2.Services;

namespace CloudStorage.API.V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IUserService _userService;

        public AccountController(ILogger<AccountController> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount(UserDTO pUser)
        {
            try
            {
                if (!SecurityUtilities.ValidatePassword(pUser.Password))
                {
                    return BadRequest();
                }
                User? user = Converter.Convert<UserDTO, User>(pUser);

                if (user == null)
                {
                    return BadRequest();
                }

                user.Id = Guid.NewGuid().ToString();
                user.Created = DateTime.UtcNow;
                user.Updated = DateTime.UtcNow;
                user = await _userService.CreateAsync(user);
                pUser = Converter.Convert(user) ?? new UserDTO();

                return Ok(pUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create Account Failed");
                return StatusCode(500);
            }
        }
    }
}
