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
using CloudStorage.API;
using CloudStorage.Interfaces;
using Microsoft.Azure.Cosmos;
using CloudStorage.API.Services;

namespace TrackASnack_WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [AllowAnonymous]
    public class AuthenticationController : Controller
    {
        private readonly ILogger<AuthenticationController> _logger;
        private readonly IDatabaseService _database;
        private readonly IJwtService _jwtService;

        public AuthenticationController(ILogger<AuthenticationController> logger, IDatabaseService databaseService, IJwtService jwtService)
        {
            _logger = logger;
            _database = databaseService;
            _jwtService = jwtService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult?> Authenticate()
        {
            string? username = null;
            string? password = null;
            string? passwordPepper = null;

            try
            {
                string basicAuthentication = Request.Headers.Authorization.FirstOrDefault() ?? throw new Exception("Unable to Authorization header");

                if (!basicAuthentication.StartsWith("Basic", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Invalid authentication type");
                }

                basicAuthentication = basicAuthentication.Replace("Basic", "").Trim();
                string basicAuthDetails = Encoding.UTF8.GetString(Convert.FromBase64String(basicAuthentication));
                username = basicAuthDetails[..basicAuthDetails.IndexOf(':')];
                password = basicAuthDetails[(basicAuthDetails.IndexOf(':') + 1)..];

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    throw new Exception("Username or password missing");
                }

                IUser user = await _database.GetUserByUsernameAsync(username);

                byte[] passwordData = System.Text.Encoding.UTF8.GetBytes($"{password}{user.PasswordSalt}{passwordPepper}");
                byte[] passwordHashData = SHA256.HashData(passwordData);
                string passwordHash = Convert.ToBase64String(passwordHashData);

                if (!string.Equals(passwordHash, user.Password))
                {
                    throw new Exception("Passwords do not match");
                }

                RefreshToken refreshToken = _jwtService.CreateRefreshToken(user);
                Token token = _jwtService.GenerateToken(user, refreshToken);

                return new OkObjectResult(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("refresh/{token}")]
        public async Task<IActionResult?> RefreshToken([FromRoute(Name = "token")]string pRefreshTokenId)
        {
            string? userId = null;
            Token token = new Token();
            RefreshToken? refreshToken = null;
            IUser? user = null;

            try
            {
                CloudStorage.API.Models.JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(Request);
                userId = jwtPayload.Subject;

                user = await _database.GetUserByIdAsync(userId);
                refreshToken = await _database.GetRefreshTokenAsync(pRefreshTokenId);

                if (refreshToken == null)
                {
                    throw new Exception("Refresh tokens do not match");
                }

                await _database.DeleteRefreshTokenAsync(refreshToken);

                RefreshToken newRefreshToken = _jwtService.CreateRefreshToken(user);
                await _database.InsertRefreshTokenAsync(newRefreshToken);
                Token newtoken = _jwtService.GenerateToken(user, refreshToken);

                return new OkObjectResult(newtoken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
    }
}
