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
        public static string CreateToken(User user, AppSettings appSettings, int sessionTimeoutMinutes = 60)
        {
            DateTime now = DateTime.UtcNow;
            byte[] encryptionKey = Encoding.UTF8.GetBytes(appSettings.Jwt.Key);
            SecurityKey securityKey = new SymmetricSecurityKey(encryptionKey);

            IList<Claim> claims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, now.ToString("yyyy-MM-ddThh:mm:ssK")),

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
    }
}
