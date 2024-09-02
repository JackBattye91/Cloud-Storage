
namespace CloudStorage.SPA.V2.Services
{
    public class CloudHttpClient : DelegatingHandler
    {
        private readonly AuthenticationService _authenticationService;


        public CloudHttpClient(AuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_authenticationService.IsAuthenticated())
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authenticationService.GetToken());
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
