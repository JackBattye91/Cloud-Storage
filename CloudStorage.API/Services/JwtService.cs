using CloudStorage.API.Models;
using CloudStorage.Interfaces;
using CloudStorage.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CloudStorage.API.Services
{
    public interface IJwtService
    {
        RefreshToken CreateRefreshToken(IUser user);
        Token GenerateToken(IUser user, RefreshToken? refreshToken);
    }

    public class JwtService : IJwtService
    {
        private readonly ILogger<JwtService> _logger;
        private readonly AppSettings _settings;

        public JwtService(ILogger<JwtService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _settings = configuration.Get<AppSettings>() ?? throw new Exception("Unable to get AppSettings");
        }

        public RefreshToken CreateRefreshToken(IUser user)
        {
            return new RefreshToken()
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user!.Id,
                DateCreated = DateTime.UtcNow
            };
        }

        public Token GenerateToken(IUser user, RefreshToken? refreshToken = null)
        {
            string? issuer = _settings.Jwt.Issuer;
            string? securityKey = _settings.Jwt.Key;
            string? audience = _settings.Jwt.Audience;

            IDictionary<string, string> jwtBody = new Dictionary<string, string>();
            DateTime issuedAt = DateTime.UtcNow;
            DateTime expiresAt = issuedAt.AddSeconds(_settings.Jwt.ExpiresAfterSeconds);

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

            return new Token()
            {
                TokenId = Guid.NewGuid().ToString(),
                WebToken = jwtData,
                Expires = expiresAt,
                RefreshToken = refreshToken?.Id ?? null
            };
        }

        public void ValidateToken(string bearerToken)
        {
            if (!bearerToken.StartsWith("bearer", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Invalid token");
            }

            bearerToken = bearerToken.Remove(0, 6).Trim();

            TokenValidationParameters validationParameters = new TokenValidationParameters()
            {
                ValidIssuer = _settings!.Jwt.Issuer,
                ValidAudience = _settings!.Jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_settings!.Jwt.Key)),

                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = false
            };

            JwtSecurityTokenHandler jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            jwtSecurityTokenHandler.ValidateToken(bearerToken, validationParameters, out SecurityToken validatedToken);
        }
    }
}
