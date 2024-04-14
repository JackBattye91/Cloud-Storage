using CloudStorage.Interfaces;
using CloudStorage.Models;
using Newtonsoft.Json;

namespace CloudStorage.WebApp.Services
{
    

    public interface IBlobService
    {
        Task<IEnumerable<IBlobDetail>> GetBlobDetailsAsync();
    }

    public class BlobService : IBlobService
    {
        private readonly ILogger<BlobService> _logger;
        private readonly IHttpClientFactory _clientFactory;

        public BlobService(ILogger<BlobService> logger, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _clientFactory = clientFactory;

        }

        public async Task<IEnumerable<IBlobDetail>> GetBlobDetailsAsync()
        {
            IList<IBlobDetail> blobDetails = new List<IBlobDetail>();

            try
            {
                HttpClient client = _clientFactory.CreateClient("api");
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, "blob");
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage);

                if (responseMessage.IsSuccessStatusCode)
                {
                    string content = await responseMessage.Content.ReadAsStringAsync();
                    var blobDetailModels = JsonConvert.DeserializeObject<List<BlobDetail>>(content);

                    if (blobDetailModels != null)
                    {
                        foreach (var blobDetail in blobDetailModels)
                        {
                            blobDetails.Add(blobDetail);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }

            return blobDetails;
        }
    }
}
