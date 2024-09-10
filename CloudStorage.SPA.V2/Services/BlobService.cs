using CloudStorage.SPA.V2.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloudStorage.SPA.V2.Services
{
    public class BlobService
    {
        private readonly ILogger<BlobService> _logger;
        private readonly IHttpClientFactory _clientFactory;

        public BlobService( IHttpClientFactory clientFactory, ILogger<BlobService> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<IList<BlobDetail>> GetBlobDetails()
        {
            try
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, "api/Blob/details");
                HttpClient client = _clientFactory.CreateClient("api");
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage);

                if (responseMessage.IsSuccessStatusCode)
                {
                    string content = await responseMessage.Content.ReadAsStringAsync();
                    List<BlobDetail>? blobDetails = JsonSerializer.Deserialize<List<BlobDetail>>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    if (blobDetails != null)
                    {
                        return blobDetails;
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return new List<BlobDetail>();
        }

        public async Task<Stream> GetThumbnail(string id)
        {
            try
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $"api/Blob/thumbnail/{id}");
                HttpClient client = _clientFactory.CreateClient("api");
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage);

                if (responseMessage.IsSuccessStatusCode)
                {
                    return await responseMessage.Content.ReadAsStreamAsync();
                }
                else
                {
                    throw new Exception("");
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Stream> GetImage(string id)
        {
            try
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $"api/Blob/stream/{id}");
                HttpClient client = _clientFactory.CreateClient("api");
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage);

                if (responseMessage.IsSuccessStatusCode)
                {
                    return await responseMessage.Content.ReadAsStreamAsync();
                }
                else
                {
                    throw new Exception("");
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<BlobDetail?> UploadStreamAsync(BlobDetail blobDetail, Stream imageStream)
        {
            try
            {
                string details = JsonSerializer.Serialize(blobDetail);

                Stream dataStream = new MemoryStream();
                dataStream.Write(BitConverter.GetBytes(details.Length));
                dataStream.Write(System.Text.Encoding.UTF8.GetBytes(details));

                byte[] buffer = new byte[1024];
                int bytesRead = 0;

                if (!imageStream.CanRead)
                {
                    throw new Exception("Unable to read stream");
                }

                do
                {
                    bytesRead = await imageStream.ReadAsync(buffer, 0, 1024);
                    dataStream.Write(buffer, 0, bytesRead);
                } while (bytesRead > 0);

                dataStream.Seek(0, SeekOrigin.Begin);
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/Blob/stream");
                requestMessage.Content = new StreamContent(dataStream);
                HttpClient client = _clientFactory.CreateClient("api");
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage);

                if (responseMessage.IsSuccessStatusCode)
                {
                    string content = await responseMessage.Content.ReadAsStringAsync();
                    BlobDetail? newBlobDetail = JsonSerializer.Deserialize<BlobDetail>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    if (newBlobDetail != null)
                    {
                        return newBlobDetail;
                    }
                    else
                    {
                        throw new Exception("Unable to deserialize BlobDetail");
                    }
                }
                else
                {
                    throw new Exception($"Server returned error code: {(int)responseMessage.StatusCode} - {await requestMessage.Content.ReadAsStringAsync()}");
                }
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadStreamAsync failed");
                throw;
            }
        }
    }
}
