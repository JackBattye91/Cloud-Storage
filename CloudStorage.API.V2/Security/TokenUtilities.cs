using CloudStorage.API.V2.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CloudStorage.API.V2.Security
{
    public class TokenUtilities
    {
        public static TokenValidationParameters ValidationParameters(AppSettings appSettings)
        {
            return new TokenValidationParameters
            {
                ValidIssuer = appSettings!.Jwt.Issuer,
                ValidAudience = appSettings!.Jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings!.Jwt.Key)),
                LifetimeValidator = new LifetimeValidator(CustomLifetimeValidator.Create),

                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,

                RequireSignedTokens = true
            };
        }

        public static TokenValidationParameters ValidationParametersWithoutDate(AppSettings appSettings)
        {
            return new TokenValidationParameters
            {
                ValidIssuer = appSettings!.Jwt.Issuer,
                ValidAudience = appSettings!.Jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings!.Jwt.Key)),

                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,

                RequireSignedTokens = true
            };
        }

        public static string CreateToken(User user, AppSettings appSettings, int sessionTimeoutMinutes = 60)
        {
            DateTime now = DateTime.UtcNow;
            byte[] encryptionKey = Encoding.UTF8.GetBytes(appSettings.Jwt.Key);
            SecurityKey securityKey = new SymmetricSecurityKey(encryptionKey);

            IList<Claim> claims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, now.ToString("yyyy-MM-ddThh:mm:ssK")),

                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.GivenName, user.Forenames),
                new Claim(JwtRegisteredClaimNames.FamilyName, user.Surname),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
            };

            JwtSecurityToken jwtToken = new JwtSecurityToken(
                issuer: appSettings.Jwt.Issuer,
                audience: appSettings.Jwt.Audience,
                expires: now.AddMinutes(sessionTimeoutMinutes),
                notBefore: now,
                claims: claims,
                signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
            );

            string encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            return encodedJwt;
        }

        public static string GetSubjectEmail(HttpRequest pRequest)
        {
            string? bearerToken = pRequest.Headers.Authorization.FirstOrDefault();
            if (bearerToken == null)
            {
                throw new UnauthorizedAccessException();
            }

            JwtSecurityTokenHandler hander = new JwtSecurityTokenHandler();

            JwtSecurityToken securityToken = hander.ReadJwtToken(bearerToken);
            string? email = securityToken.Claims.Where(x => x.Type == JwtRegisteredClaimNames.Email).FirstOrDefault()?.Value;

            return email ?? string.Empty;
        }

        public static async Task<bool> ValidateToken(HttpRequest pRequest, AppSettings appSettings)
        {
            string? bearerToken = pRequest.Headers.Authorization.FirstOrDefault();

            if (bearerToken == null)
            {
                return false;
            }

            JwtSecurityTokenHandler hander = new JwtSecurityTokenHandler();
            var validation = await hander.ValidateTokenAsync(bearerToken, ValidationParameters(appSettings));
            return validation.IsValid;
        }
        public static async Task<bool> ValidateTokenWithoutDate(HttpRequest pRequest, AppSettings appSettings)
        {
            string? bearerToken = pRequest.Headers.Authorization.First();

            if (string.IsNullOrEmpty(bearerToken))
            {
                return false;
            }

            bearerToken = bearerToken.Substring(8, bearerToken.Length - (8 + 3));

            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            JwtSecurityToken token = handler.ReadJwtToken(bearerToken);

            var validation = await handler.ValidateTokenAsync(token, ValidationParametersWithoutDate(appSettings));

            return validation.IsValid;
        }
    }
}
