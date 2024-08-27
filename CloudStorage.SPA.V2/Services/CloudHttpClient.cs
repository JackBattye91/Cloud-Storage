namespace CloudStorage.SPA.V2.Services
{
    public class CloudHttpClient : HttpClient
    {
        private readonly CloudStorageAuthenticationState _authenticationState;

        public CloudHttpClient(CloudStorageAuthenticationState authenticationState)
        {
            _authenticationState = authenticationState;
        }

        public override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            

            return base.Send(request, cancellationToken);
        }

        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken);
        }


    }
}
