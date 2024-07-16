using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CloudStorage.API.V2.Models.DTOs;

namespace CloudStorage.API.V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly ILogger<AccountController> _logger;
        public AccountController(ILogger<AccountController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public IActionResult CreateAccount(UserDTO user)
        {
            try
            {
                if (!SecurityUtilities.ValidatePassword(user.Password))
                {
                    return BadRequest();
                }



                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create Account Failed");
                return StatusCode(500);
            }
        }
    }
}
