using System.IO;

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
        private readonly JB.Blob.IWrapper _blobWrapper;

        public BlobService(ILogger<BlobService> logger, JB.Blob.IWrapper blobWrapper)
        {
            _logger = logger;
            _blobWrapper = blobWrapper;
        }

        public async Task<Stream> GetBlobStreamAsync(string container, string name)
        {
            var getStreamRc = await _blobWrapper.GetBlobAsStreamAsync(container, name);

            if (getStreamRc.Success)
            {
                return getStreamRc.Data!;
            }

            throw new Exception("Unable to get blob as stream");
        }
        public async Task<byte[]> GetBlobDataAsync(string container, string name)
        {
            var getStreamRc = await _blobWrapper.GetBlobAsArrayAsync(container, name);

            if (getStreamRc.Success)
            {
                return getStreamRc.Data!;
            }

            throw new Exception("Unable to get blob data");
        }
        public async Task UploadBlobAsync(string container, string name, Stream stream)
        {
            var getStreamRc = await _blobWrapper.UploadBlobAsync(stream, container, name);

            if (getStreamRc.Success)
            {
                return;
            }

            throw new Exception("Unable to get blob data");
        }
        public async Task UploadBlobAsync(string container, string name, byte[] data) {
            var getStreamRc = await _blobWrapper.UploadBlobAsync(data, container, name);

            if (getStreamRc.Success)
            {
                return;
            }

            throw new Exception("Unable to get blob data");
        }

        public async Task DeleteBlobAsync(string container, string name)
        {
            
        }
    }
}