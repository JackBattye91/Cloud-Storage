using CloudStorage.API.Consts;
using CloudStorage.Consts;
using CloudStorage.Models;
using CloudStorage.API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using CloudStorage.API.Services;
using CloudStorage.Interfaces;
using Microsoft.Azure.Cosmos;
using System.Diagnostics.Eventing.Reader;

namespace CloudStorage.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserController : Controller
    {
        private ILogger<UserController> _logger;
        private IDatabaseService _database { get; set; }

        public UserController(ILogger<UserController> pLogger, IDatabaseService databaseService)
        {
            _logger = pLogger;
            _database = databaseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(Request);
                string userId = jwtPayload.Subject;
                IUser user = await _database.GetUserByIdAsync(userId);

                user!.Password = string.Empty;
                user!.PasswordSalt = string.Empty;
                return new OkObjectResult(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody]CloudStorage.Models.User pUser)
        {
            try
            {
                JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(Request);
                string userId = jwtPayload.Subject;

                if (pUser.Id != userId)
                {
                    throw new Exception("Unable to validate user");
                }

                IUser user = await _database.UpdateUserAsync(pUser);

                return new OkObjectResult(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateProfile([FromBody] CloudStorage.Models.User pUser)
        {
            try
            {
                JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(Request);
                string userId = jwtPayload.Subject;

                /*
                if (!string.Equals(jwtPayload.Permissions, Permission.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return new UnauthorizedResult();
                }
                */
                pUser.Id = Guid.NewGuid().ToString();
                pUser.PrivateKey = Encoding.UTF8.GetString(RandomNumberGenerator.GetBytes(16));
                await _database.CreateUserAsync(pUser);

                return new OkObjectResult(pUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> DeleteProfile([FromRoute(Name = "id")] string pUserId)
        {
            try
            {
                JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(Request);
                string userId = jwtPayload.Subject;

                await _database.DeleteUserAsync(userId);

                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpGet]
        [Route("/exists/username/{username}")]
        [AllowAnonymous]
        public async Task<IActionResult> UsernameExists([FromRoute(Name = "username")] string pUsername)
        {
            try
            {
                bool usernameExists = await _database.DoesUsernameExist(pUsername);

                if (usernameExists)
                {
                    return new OkResult();
                }
                else
                {
                    return new NotFoundResult();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpGet]
        [Route("exists/email/{email}")]
        [AllowAnonymous]
        public async Task<IActionResult> EmailExists([FromRoute(Name = "email")] string pEmail)
        {
            try
            {
                bool emailExists = await _database.DoesEmailExist(pEmail);

                if (emailExists) {
                    return new OkResult();
                }
                else {
                    return new NotFoundResult();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
    }
}
