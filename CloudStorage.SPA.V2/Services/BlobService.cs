using CloudStorage.SPA.V2.Models;
using System.Text.Json;

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

        public async Task<BlobDetail?> UploadStreamAsync(BlobDetail blobDetail, Stream dataStream)
        {
            try
            {
                using (Stream uploadStream = new MemoryStream())
                using (StreamWriter writer = new StreamWriter(uploadStream))
                {
                    await WriteBlobDetails(uploadStream, blobDetail);

                    char[] buffer = new char[1024];
                    using (StreamReader dataReader = new StreamReader(dataStream))
                    {
                        int bytesRead = await dataReader.ReadAsync(buffer);
                        await writer.WriteAsync(buffer, 0, bytesRead);
                    }

                    uploadStream.Position = 0;

                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, "Blob/stream");
                    requestMessage.Content = new StreamContent(uploadStream);
                    HttpClient client = _clientFactory.CreateClient("api");
                    HttpResponseMessage responseMessage = await client.SendAsync(requestMessage);

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        string content = await responseMessage.Content.ReadAsStringAsync();
                        BlobDetail? newBlobDetail = JsonSerializer.Deserialize<BlobDetail>(content);

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
                        throw new Exception($"Server returned error code: {(int)responseMessage.StatusCode}");
                    }
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
