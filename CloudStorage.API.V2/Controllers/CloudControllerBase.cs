using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CloudStorage.API.V2.Controllers
{
    public class CloudControllerBase : ControllerBase
    {
        protected string? GetUserIdAsync()
        {
            string? authHeader = Response.Headers.Authorization.FirstOrDefault();

            if (authHeader == null)
            {
                throw new Exception("Unauthorised");
            }

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken token = tokenHandler.ReadJwtToken(authHeader);

            Claim? subjectClaim = token.Claims.Where(x => x.Subject != null).FirstOrDefault();
            string? userId = subjectClaim?.Value;

            return userId;
        }
    }
}
