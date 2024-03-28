
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using CloudStorage.Interfaces;
using CloudStorage.Models;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Net.Http;
using System.Threading;

namespace CloudStorage.SPA.Models
{
    public class JwtDelegationHandler : DelegatingHandler
    {

        private readonly ProtectedLocalStorage ProtectedStorage;
        private readonly IHttpClientFactory ClientFactory;
        private static IToken? Token { get; set; }
        private static bool GettingRefreshToken { get; set; } = false;

        public JwtDelegationHandler(ProtectedLocalStorage pProtectedLocalStorage, IHttpClientFactory pHttpClientFactory)
        {
            ProtectedStorage = pProtectedLocalStorage;
            ClientFactory = pHttpClientFactory;
            
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
            if (request.RequestUri?.AbsolutePath.StartsWith("/Authentication") == true)
            {
                HttpResponseMessage authResponse = await base.SendAsync(request, cancellationToken);
                if (authResponse.IsSuccessStatusCode)
                {
                    string auth = await authResponse.Content.ReadAsStringAsync();
                    Token = JsonConvert.DeserializeObject<Token>(auth);
                }

                return authResponse;
            }

            if (Token!.Expires <= DateTime.UtcNow)
            {
                await GetRefreshToken(request, cancellationToken);
            }

            if (IsValidJwt(cancellationToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token!.WebToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private bool IsValidJwt(CancellationToken cancellationToken) { 
            if (Token == null)
            {
                Token = AppState.Instance?.Token;
            }

            return (Token != null);
        }

        private async Task GetRefreshToken(HttpRequestMessage pRequestMessage, CancellationToken cancellationToken)
        {
            try
            {
                if (GettingRefreshToken)
                {
                    while (GettingRefreshToken)
                    {
                        await Task.Delay(100);
                    }
                }
                else
                {
                    GettingRefreshToken = true;

                    int baseUrlEnd = pRequestMessage.RequestUri!.AbsoluteUri.IndexOf(pRequestMessage.RequestUri.AbsolutePath);
                    string baseUrl = pRequestMessage.RequestUri.AbsoluteUri.Substring(0, baseUrlEnd);
                    string authUri = $"{baseUrl}/Authentication/refresh/{Token!.RefreshToken}";

                    HttpClient client = new HttpClient();
                    HttpRequestMessage refreshRequest = new HttpRequestMessage(HttpMethod.Get, authUri);
                    refreshRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token!.WebToken);
                    HttpResponseMessage refreshResponse = await client.SendAsync(refreshRequest, cancellationToken);

                    if (refreshResponse.IsSuccessStatusCode)
                    {
                        string auth = await refreshResponse.Content.ReadAsStringAsync();
                        Token = JsonConvert.DeserializeObject<Token>(auth);
                    }

                }

            }
            catch (Exception ex)
            {
                
            }

            GettingRefreshToken = false;
        }
    }
}
