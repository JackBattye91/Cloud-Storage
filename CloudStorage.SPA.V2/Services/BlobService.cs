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

        protected async Task WriteBlobDetails(Stream stream, BlobDetail blobDetail)
        {
            int nameLength = blobDetail.Name.Length;
            int descriptionLength = blobDetail.Description.Length;

            byte[] nameBuffer = System.Text.Encoding.UTF8.GetBytes(blobDetail.Name);
            byte[] descriptionBuffer = System.Text.Encoding.UTF8.GetBytes(blobDetail.Description);

            stream.WriteByte((byte)nameLength);
            await stream.WriteAsync(nameBuffer);
            stream.WriteByte((byte)descriptionLength);
            await stream.WriteAsync(descriptionBuffer);
        }
    }
}
