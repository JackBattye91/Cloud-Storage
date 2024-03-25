using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using JB.Common.Errors;
using JB.Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using CloudStorage.API.Models;
using CloudStorage.Models;
using CloudStorage.API.Consts;
using JB.NoSqlDatabase;
using CloudStorage.API;
using CloudStorage.Interfaces;

namespace TrackASnack_WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [AllowAnonymous]
    public class AuthenticationController : Controller
    {
        ILogger<AuthenticationController> logger;
        IWrapper NoSqlWrapper;
        AppSettings appSettings;

        public AuthenticationController(ILogger<AuthenticationController> pLogger, IOptions<AppSettings> pAppSettings)
        {
            appSettings = pAppSettings.Value;
            logger = pLogger;
            NoSqlWrapper = Factory.CreateNoSqlDatabaseWrapper(appSettings.Database.ConnectionString);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult?> Authenticate([FromHeader(Name = "Authorization")] string pBasicAuthentication)
        {
            IReturnCode rc = new ReturnCode();
            string? username = null;
            string? password = null;
            string? database = null;
            string? passwordPepper = null;
            Token token = new Token();
            User? user = null;
            RefreshToken? refreshToken = null;

            try
            {
                // pre-checks
                if (rc.Success)
                {
                    database = CloudStorage.API.Consts.Database.DATABASE;
                    passwordPepper = "JackBattyePepperString";

                    if (string.IsNullOrEmpty(database) || string.IsNullOrEmpty(passwordPepper))
                    {
                        rc.AddError(new Error(4, new JBException("Invalid authentication type")));
                    }

                    if (!pBasicAuthentication.StartsWith("Basic", StringComparison.OrdinalIgnoreCase))
                    {
                        rc.AddError(new Error(2, new JBException("Invalid authentication type")));
                    }
                }

                if (rc.Success)
                {
                    pBasicAuthentication = pBasicAuthentication.Replace("Basic", "").Trim();
                    string basicAuthDetails = Encoding.UTF8.GetString(Convert.FromBase64String(pBasicAuthentication));
                    username = basicAuthDetails[..basicAuthDetails.IndexOf(':')];
                    password = basicAuthDetails[(basicAuthDetails.IndexOf(':') + 1)..];

                    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                    {
                        rc.AddError(new Error(4, new JBException("Username or password missing")));
                    }
                }

                if (rc.Success)
                {
                    string sqlQuery = $"SELECT * FROM c WHERE c.username = '{username}'";
                    IReturnCode<IList<User>> getUsersRc = await NoSqlWrapper.GetItems<User>(database!, Database.USER_CONTAINER_NAME, sqlQuery);

                    if (getUsersRc.Success)
                    {
                        IList<User> usersList = getUsersRc.Data!;

                        if (usersList.Count == 1)
                        {
                            user = usersList[0];
                        }
                        else if (usersList.Count > 1)
                        {
                            rc.AddError(new Error(4, new JBException("Too many users returned")));
                        }
                        else
                        {
                            rc.AddError(new Error(4, new JBException("No user found")));
                        }
                    }

                    if (getUsersRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getUsersRc, rc);
                    }
                }

                if (rc.Success)
                {
                    byte[] passwordData = System.Text.Encoding.UTF8.GetBytes($"{password}{user?.PasswordSalt}{passwordPepper}");
                    byte[] passwordHashData = SHA256.HashData(passwordData);
                    string passwordHash = Convert.ToBase64String(passwordHashData);

                    if (!string.Equals(passwordHash, user.Password))
                    {
                        rc.AddError(new Error(6, new JBException("Passwords do not match")));
                    }
                }

                if (rc.Success)
                {
                    refreshToken = new RefreshToken()
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = user!.Id,
                        Token = Guid.NewGuid().ToString(),
                        DateCreated = DateTime.UtcNow
                    };
                }

                if (rc.Success)
                {
                    string? issuer = appSettings.Jwt.Issuer;
                    string? securityKey = appSettings.Jwt.Key;
                    string? audience = appSettings.Jwt.Audience;

                    IDictionary<string, string> jwtBody = new Dictionary<string, string>();
                    DateTime issuedAt = DateTime.UtcNow;
                    DateTime expiresAt = issuedAt.AddSeconds(3600);

                    SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));
                    SigningCredentials signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

                    IList<Claim> claimsList = new List<Claim>() {
                        new Claim("sub", user!.Id),
                        new Claim("per", user.Permissions.ToString())
                    };
                    ClaimsIdentity claimsIdentity = new ClaimsIdentity(claimsList);

                    JwtSecurityTokenHandler jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
                    JwtSecurityToken securityToken = jwtSecurityTokenHandler.CreateJwtSecurityToken(issuer, audience, claimsIdentity, issuedAt, expiresAt, issuedAt, signingCredentials);
                    string jwtData = jwtSecurityTokenHandler.WriteToken(securityToken);

                    token = new Token()
                    {
                        TokenId = Guid.NewGuid().ToString(),
                        WebToken = jwtData,
                        Expires = expiresAt,
                        RefreshToken = refreshToken!.Token
                    };
                }

                if (rc.Success)
                {
                    IReturnCode getUserRc = await NoSqlWrapper.AddItem(CloudStorage.API.Consts.Database.DATABASE, Database.REFRESH_TOKEN_CONTAINER_NAME, refreshToken!);

                    if (getUserRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getUserRc, rc);
                    }
                }

                if (rc.Success)
                {
                    return new OkObjectResult(token);
                }
            }
            catch (Exception ex)
            {
                rc.AddError(new Error(1, ex));
            }

            if (rc.Failed)
            {
                ErrorWorker.LogErrors(logger, rc);
            }

            return new UnauthorizedResult();
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("refresh/{token}")]
        public async Task<IActionResult?> RefreshToken([FromHeader(Name = "Authorization")] string pBearerToken, [FromRoute(Name = "token")]string pRefreshToken)
        {
            IReturnCode rc = new ReturnCode();
            string? userId = null;
            Token token = new Token();
            IList<RefreshToken>? refreshTokensList = null;
            RefreshToken? refreshToken = null;
            IUser? user = null;

            try
            {
                if (rc.Success)
                {
                    CloudStorage.API.Models.JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(pBearerToken);
                    userId = jwtPayload.Subject;
                }
                if (rc.Success)
                {
                    if (!pBearerToken.StartsWith("bearer", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new JBException("Invalid token");
                    }

                    pBearerToken = pBearerToken.Remove(0, 6).Trim();

                    TokenValidationParameters validationParameters = new TokenValidationParameters()
                    {
                        ValidIssuer = appSettings!.Jwt.Issuer,
                        ValidAudience = appSettings!.Jwt.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings!.Jwt.Key)),
                        
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = false
                    };

                    JwtSecurityTokenHandler jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
                    jwtSecurityTokenHandler.ValidateToken(pBearerToken, validationParameters, out SecurityToken validatedToken);
                }

                if (rc.Success)
                {
                    IReturnCode<IUser> getUserRc = await NoSqlWrapper.GetItem<IUser, User>(CloudStorage.API.Consts.Database.DATABASE, Database.USER_CONTAINER_NAME, userId!);

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
                    string query = $"SELECT * FROM c WHERE c.userId = '{userId}'";
                    IReturnCode<IList<RefreshToken>> getUserRc = await NoSqlWrapper.GetItems<RefreshToken>(CloudStorage.API.Consts.Database.DATABASE, Database.REFRESH_TOKEN_CONTAINER_NAME, query);

                    if (getUserRc.Success)
                    {
                        if (getUserRc.Data?.Count > 0)
                        {
                            refreshTokensList = getUserRc.Data;
                        }
                        else
                        {
                            throw new JBException("No refresh tokens found");
                        }
                    }

                    if (getUserRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getUserRc, rc);
                    }
                }

                if (rc.Success)
                {
                    refreshToken = refreshTokensList!.Where(x => string.Equals(x.Token, pRefreshToken, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                    if (refreshToken == null)
                    {
                        throw new JBException("Refresh tokens do not match");
                    }
                }

                if (rc.Success)
                {
                    IReturnCode deleteTokenRc = await NoSqlWrapper.DeleteItem<RefreshToken>(CloudStorage.API.Consts.Database.DATABASE, Database.REFRESH_TOKEN_CONTAINER_NAME, refreshToken!.Id, refreshToken.UserId);

                    if (deleteTokenRc.Success) {
                        refreshToken = new RefreshToken()
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserId = userId!,
                            Token = Guid.NewGuid().ToString(),
                            DateCreated = DateTime.UtcNow,
                        };
                    }

                    if (deleteTokenRc.Failed)
                    {
                        ErrorWorker.CopyErrors(deleteTokenRc, rc);
                    }
                }

                if (rc.Success)
                {
                    string? issuer = appSettings.Jwt.Issuer;
                    string? securityKey = appSettings.Jwt.Key;
                    string? audience = appSettings.Jwt.Audience;

                    IDictionary<string, string> jwtBody = new Dictionary<string, string>();
                    DateTime issuedAt = DateTime.UtcNow;
                    DateTime expiresAt = issuedAt.AddSeconds(3600);

                    SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));
                    SigningCredentials signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

                    IList<Claim> claimsList = new List<Claim>() {
                        new Claim("sub", user!.Id),
                        new Claim("per", user.Permissions.ToString())
                    };
                    ClaimsIdentity claimsIdentity = new ClaimsIdentity(claimsList);

                    JwtSecurityTokenHandler jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
                    JwtSecurityToken securityToken = jwtSecurityTokenHandler.CreateJwtSecurityToken(issuer, audience, claimsIdentity, issuedAt, expiresAt, issuedAt, signingCredentials);
                    string jwtData = jwtSecurityTokenHandler.WriteToken(securityToken);

                    token = new Token()
                    {
                        TokenId = Guid.NewGuid().ToString(),
                        WebToken = jwtData,
                        Expires = expiresAt,
                        RefreshToken = refreshToken!.Id
                    };
                }

                if (rc.Success)
                {
                    IReturnCode getUserRc = await NoSqlWrapper.AddItem<RefreshToken>(CloudStorage.API.Consts.Database.DATABASE, Database.REFRESH_TOKEN_CONTAINER_NAME, refreshToken!);

                    if (getUserRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getUserRc, rc);
                    }
                }
            }
            catch (Exception ex)
            {
                rc.AddError(new Error(3, ex));
            }

            if (rc.Failed)
            {
                ErrorWorker.LogErrors(logger, rc);
                return StatusCode(500);
            }

            return new OkObjectResult(token);
        }
    }
}
