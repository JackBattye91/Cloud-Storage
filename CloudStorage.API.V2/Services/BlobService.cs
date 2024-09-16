using System.IO;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CloudStorage.API.V2.Models;
using Microsoft.Extensions.Options;

namespace CloudStorage.API.V2.Services
{
    
    public interface IBlobService
    {
        Task<Stream> GetBlobStreamAsync(string container, string name);
        Task<byte[]> GetBlobDataAsync(string container, string name);
        Task UploadBlobAsync(string container, string name, Stream stream);
        Task UploadBlobAsync(string container, string name, byte[] data);
        Task DeleteBlobAsync(string container, string name);
    }

    public class BlobService : IBlobService
    {
        private readonly ILogger<BlobService> _logger;
        private readonly AppSettings _appSettings;
        private readonly BlobServiceClient _blobService;

        public BlobService(ILogger<BlobService> logger, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
            _blobService = new BlobServiceClient(_appSettings.Blob.ConnectionString);
        }

        public async Task<Stream> GetBlobStreamAsync(string container, string name)
        {
            var containerClient = _blobService.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(name);
            var stream = await blobClient.DownloadStreamingAsync();

            return stream.Value.Content;
        }
        public async Task<byte[]> GetBlobDataAsync(string container, string name)
        {
            var containerClient = _blobService.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(name);
            var blobDownload = await blobClient.DownloadContentAsync();

            var getStreamRc = blobDownload.GetRawResponse().Content;

            if (getStreamRc == null)
            {
                throw new Exception("GetBlobDataAsync Failed");
            }

            return getStreamRc.ToArray();
        }
        public async Task UploadBlobAsync(string container, string name, Stream stream)
        {
            var containerClient = _blobService.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(name);
            await blobClient.UploadAsync(stream);
        }
        public async Task UploadBlobAsync(string container, string name, byte[] data) {
            var containerClient = _blobService.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(name);

            BinaryData binaryData = new BinaryData(data);
            await blobClient.UploadAsync(binaryData);
        }

        public async Task DeleteBlobAsync(string container, string name)
        {
            var containerClient = _blobService.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(name);
            var deleteResponse = await blobClient.DeleteAsync();
        }
    }
}