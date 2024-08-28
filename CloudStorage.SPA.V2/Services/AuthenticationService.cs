using CloudStorage.SPA.V2.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloudStorage.SPA.V2.Services
{
    public class AuthenticationService
    {
        private readonly ILogger<AuthenticationService> _logger;
        public AccessToken? CurrentToken { get; protected set; }
        private readonly HttpClient _client;

        public AuthenticationService(ILogger<AuthenticationService> logger, IHttpClientFactory httpClientFactory) {
            _logger = logger;
            _client = httpClientFactory.CreateClient();
        }

        public async Task<bool> Login(string username, string password)
        {
            try
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, "");
                HttpResponseMessage responseMessage = await _client.SendAsync(requestMessage);

                if (responseMessage.IsSuccessStatusCode)
                {
                    string content = await responseMessage.Content.ReadAsStringAsync();
                    AccessToken? newToken = JsonSerializer.Deserialize<AccessToken>(content);

                    if (IsValid(newToken))
                    {
                        CurrentToken = newToken;
                        return true;
                    }
                    else
                    {
                        throw new Exception("Invalid token received from authentication server");
                    }
                }
                else
                {
                    throw new Exception("Invalid response from authentication server");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed");
                return false;
            }
        }

        public void Logout()
        {
            CurrentToken = null; 
        }

        public bool IsAuthenticated() {

            try
            {
                return IsValid(CurrentToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetState failed");
                return false;
            }
        }

        private static bool IsValid(AccessToken? accessToken)
        {
            if (string.IsNullOrEmpty(accessToken?.Token))
            {
                return false;
            }

            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            JwtSecurityToken token = handler.ReadJwtToken(accessToken?.Token);

            DateTime now = DateTime.UtcNow;
            if (token.ValidFrom > now && token.ValidTo <= now)
            {
                return false;
            }

            return true;
        }
    }
}
