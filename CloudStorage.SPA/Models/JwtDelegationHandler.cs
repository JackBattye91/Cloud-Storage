
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using CloudStorage.Models;

namespace CloudStorage.SPA.Models
{
    public class JwtDelegationHandler : DelegatingHandler
    {
        private static Token? Token { get; set; }
        private AppSettings AppSettings { get; set; }

        public JwtDelegationHandler(IOptions<AppSettings> pAppSettings)
        {
            AppSettings = pAppSettings.Value;
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

            if (IsValidJwt())
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token!.WebToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private bool IsValidJwt() { 
            if (Token == null)
            {
                return false;
            }

            return true;
        }

        private async Task UpdateJwt()
        {
            if (Token == null)
            {
                throw new Exception();
            }

            if (Token.Expires > DateTime.UtcNow)
            {
                string authUri = Path.Combine(AppSettings.ApiBaseUrl, "Authentication");

                HttpClient httpClient = new HttpClient();
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, authUri);
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token.WebToken);
            }
        }
    }
}
