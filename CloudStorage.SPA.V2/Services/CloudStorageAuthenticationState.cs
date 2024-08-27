using CloudStorage.SPA.V2.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloudStorage.SPA.V2.Services
{
    public class CloudStorageAuthenticationState
    {
        private readonly ILogger<CloudStorageAuthenticationState> _logger;
        private AccessToken? _accessToken;
        private readonly HttpClient _client;

        public CloudStorageAuthenticationState(ILogger<CloudStorageAuthenticationState> logger, IHttpClientFactory httpClientFactory) {
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
                    _accessToken = JsonSerializer.Deserialize<AccessToken>(content);

                    if (string.IsNullOrEmpty(_accessToken?.Token))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed");
                return false;
            }
        }

        public bool GetState() {

            try
            {
                if (string.IsNullOrEmpty(_accessToken?.Token))
                {
                    return false;
                }

                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                JwtSecurityToken token = handler.ReadJwtToken(_accessToken?.Token);

                DateTime now = DateTime.UtcNow;
                if (token.ValidFrom > now && token.ValidTo <= now)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetState failed");
                return false;
            }
        }
    }
}
