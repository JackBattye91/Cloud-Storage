namespace CloudStorage.SPA.V2.Services
{
    public class CloudHttpClient : HttpClient
    {
        private readonly AuthenticationService _authenticationService;

        public CloudHttpClient(AuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_authenticationService.IsAuthenticated())
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authenticationService.CurrentToken!.Token);
            }

            return base.Send(request, cancellationToken);
        }

        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_authenticationService.IsAuthenticated())
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authenticationService.CurrentToken!.Token);
            }

            return base.SendAsync(request, cancellationToken);
        }


    }
}
