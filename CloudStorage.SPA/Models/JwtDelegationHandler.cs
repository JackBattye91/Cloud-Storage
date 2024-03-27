
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using CloudStorage.Interfaces;
using CloudStorage.Models;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace CloudStorage.SPA.Models
{
    public class JwtDelegationHandler : DelegatingHandler
    {
        private readonly ProtectedLocalStorage ProtectedStorage;
        private static IToken? Token { get; set; }
        private AppSettings AppSettings { get; set; }

        public JwtDelegationHandler(IOptions<AppSettings> pAppSettings, ProtectedLocalStorage pProtectedLocalStorage)
        {
            AppSettings = pAppSettings.Value;
            ProtectedStorage = pProtectedLocalStorage;
            
            if (AppState.Instance != null)
            {
                Token = AppState.Instance.Token;
            }
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.Send(request, cancellationToken);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsolutePath == "/Authentication")
            {
                HttpResponseMessage authResponse = await base.SendAsync(request, cancellationToken);

                if (authResponse.IsSuccessStatusCode)
                {
                    string auth = await authResponse.Content.ReadAsStringAsync();
                    Token = JsonConvert.DeserializeObject<Token>(auth);
                }

                return authResponse; 
            }

            if (await IsValidJwt())
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token!.WebToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private async Task<bool> IsValidJwt() { 
            if (Token == null)
            {
                Token = AppState.Instance?.Token;
            }
            await UpdateJwt();

            return (Token != null);
        }

        private async Task UpdateJwt()
        {
            if (Token?.Expires <= DateTime.UtcNow)
            {
                string authUri = Path.Combine(AppSettings.ApiBaseUrl, $"Authentication/refresh/{Token.RefreshToken}");

                HttpClient httpClient = new HttpClient();
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, authUri);
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token.WebToken);

                HttpResponseMessage refreshResponse = await httpClient.SendAsync(requestMessage);

                if (refreshResponse.IsSuccessStatusCode)
                {
                    string auth = await refreshResponse.Content.ReadAsStringAsync();
                    Token = JsonConvert.DeserializeObject<Token>(auth);
                }
            }
        }
    }
}
