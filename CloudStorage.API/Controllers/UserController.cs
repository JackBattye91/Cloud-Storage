using CloudStorage.API.Consts;
using CloudStorage.Consts;
using CloudStorage.Models;
using CloudStorage.API.Models;
using JB.Common;
using JB.Common.Errors;
using JB.NoSqlDatabase;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CloudStorage.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserController : Controller
    {

        private ILogger<UserController> Logger;
        private AppSettings AppSettings { get; set; }
        private IWrapper NoSqlWrapper { get; set; }

        public UserController(ILogger<UserController> pLogger, IOptions<AppSettings> pAppSettings)
        {
            Logger = pLogger;
            AppSettings = pAppSettings.Value;
            NoSqlWrapper = Factory.CreateNoSqlDatabaseWrapper(AppSettings.Database.ConnectionString);
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile([FromHeader(Name = "Authorization")] string pBearerToken)
        {
            IReturnCode rc = new ReturnCode();
            User? user = null;
            string? userId = null;

            try
            {
                if (rc.Success)
                {
                    JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(pBearerToken);
                    userId = jwtPayload.Subject;
                }

                if (rc.Success)
                {
                    string query = $"SELECT * FROM c WHERE c.id = '{userId}'";
                    IReturnCode<IList<User>> getUserRc = await NoSqlWrapper.GetItems<User>(AppSettings.Database.Database, Database.USER_CONTAINER_NAME, query);

                    if (getUserRc.Success)
                    {
                        if (getUserRc.Data?.Count == 1)
                        {
                            user = getUserRc.Data[0];
                        }
                        else
                        {
                            throw new JBException("Error getting user details");
                        }
                    }

                    if (getUserRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getUserRc, rc);
                    }
                }

                if (rc.Success)
                {
                    user!.Password = string.Empty;
                    user!.PasswordSalt = string.Empty;
                    return new OkObjectResult(user);
                }
            }
            catch (Exception ex)
            {
                rc.AddError(new Error(2, ex));
            }

            if (rc.Failed)
            {
                ErrorWorker.LogErrors(Logger, rc);
            }

            return StatusCode(500);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromHeader(Name = "Authorization")] string pBearerToken, [FromBody]User pUser)
        {
            IReturnCode rc = new ReturnCode();
            User? user = null;
            string? userId = null;

            try
            {
                if (rc.Success)
                {
                    JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(pBearerToken);
                    userId = jwtPayload.Subject;

                    if (pUser.Id != userId)
                    {
                        return new UnauthorizedResult();
                    }
                }

                if (rc.Success)
                {
                    string query = $"SELECT * FROM c WHERE c.userId = '{userId}'";
                    IReturnCode<User> getUserRc = await NoSqlWrapper.UpdateItem<User>(AppSettings.Database.Database, Database.USER_CONTAINER_NAME, pUser, pUser.Id, pUser.Id);

                    if (getUserRc.Success)
                    {
                        user = getUserRc.Data;
                    }

                    if (getUserRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getUserRc, rc);
                    }
                }

                if (rc.Success)
                {
                    return new OkObjectResult(user);
                }
            }
            catch (Exception ex)
            {
                rc.AddError(new Error(2, ex));
            }

            if (rc.Failed)
            {
                ErrorWorker.LogErrors(Logger, rc);
            }

            return StatusCode(500);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProfile([FromHeader(Name = "Authorization")] string pBearerToken, [FromBody] User pUser)
        {
            IReturnCode rc = new ReturnCode();
            User? user = null;
            string? userId = null;

            try
            {
                if (rc.Success)
                {
                    JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(pBearerToken);
                    userId = jwtPayload.Subject;

                    if (!string.Equals(jwtPayload.Permissions, Permission.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return new UnauthorizedResult();
                    }
                }

                if (rc.Success)
                {
                    pUser.Id = Guid.NewGuid().ToString();
                    IReturnCode<User> getUserRc = await NoSqlWrapper.AddItem<User>(AppSettings.Database.Database, Database.USER_CONTAINER_NAME, pUser);

                    if (getUserRc.Success)
                    {
                        user = getUserRc.Data;
                    }

                    if (getUserRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getUserRc, rc);
                    }
                }

                if (rc.Success)
                {
                    return new OkObjectResult(user);
                }
            }
            catch (Exception ex)
            {
                rc.AddError(new Error(2, ex));
            }

            if (rc.Failed)
            {
                ErrorWorker.LogErrors(Logger, rc);
            }

            return StatusCode(500);
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> DeleteProfile([FromHeader(Name = "Authorization")] string pBearerToken, [FromRoute(Name = "id")] string pUserId)
        {
            IReturnCode rc = new ReturnCode();
            string? userId = null;

            try
            {
                if (rc.Success)
                {
                    JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(pBearerToken);
                    userId = jwtPayload.Subject;
                }

                if (rc.Success)
                {
                    IReturnCode getUserRc = await NoSqlWrapper.DeleteItem<User>(AppSettings.Database.Database, Database.USER_CONTAINER_NAME, pUserId, pUserId);

                    if (getUserRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getUserRc, rc);
                    }
                }

                if (rc.Success)
                {
                    return new OkResult();
                }
            }
            catch (Exception ex)
            {
                rc.AddError(new Error(2, ex));
            }

            if (rc.Failed)
            {
                ErrorWorker.LogErrors(Logger, rc);
            }

            return StatusCode(500);
        }

        [HttpGet]
        [Route("/exists/username/{username}")]
        [AllowAnonymous]
        public async Task<IActionResult> UsernameExists([FromRoute(Name = "username")] string pUsername)
        {
            IReturnCode rc = new ReturnCode();

            try
            {
                if (rc.Success)
                {
                    string query = $"SELECT * FROM c WHERE c.username = '{pUsername}'";
                    IReturnCode<IList<User>> getUserRc = await NoSqlWrapper.GetItems<User>(AppSettings.Database.Database, Database.USER_CONTAINER_NAME, query);

                    if (getUserRc.Success)
                    {
                        if (getUserRc.Data?.Count >= 1)
                        {
                            return StatusCode(409);
                        }
                        else
                        {
                            return StatusCode(200);
                        }
                    }

                    if (getUserRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getUserRc, rc);
                    }
                }
            }
            catch (Exception ex)
            {
                rc.AddError(new Error(2, ex));
            }

            if (rc.Failed)
            {
                ErrorWorker.LogErrors(Logger, rc);
            }

            return StatusCode(500);
        }

        [HttpGet]
        [Route("exists/email/{email}")]
        [AllowAnonymous]
        public async Task<IActionResult> EmailExists([FromRoute(Name = "email")] string pEmail)
        {
            IReturnCode rc = new ReturnCode();

            try
            {
                if (rc.Success)
                {
                    string query = $"SELECT * FROM c WHERE c.email = '{pEmail}'";
                    IReturnCode<IList<User>> getUserRc = await NoSqlWrapper.GetItems<User>(AppSettings.Database.Database, Database.USER_CONTAINER_NAME, query);

                    if (getUserRc.Success)
                    {
                        if (getUserRc.Data?.Count >= 1)
                        {
                            return StatusCode(409);
                        }
                        else
                        {
                            return StatusCode(200);
                        }
                    }

                    if (getUserRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getUserRc, rc);
                    }
                }
            }
            catch (Exception ex)
            {
                rc.AddError(new Error(2, ex));
            }

            if (rc.Failed)
            {
                ErrorWorker.LogErrors(Logger, rc);
            }

            return StatusCode(500);
        }
    }
}
