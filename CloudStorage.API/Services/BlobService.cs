using Azure.Storage.Blobs;
using CloudStorage.API.Models;
using CloudStorage.Interfaces;
using System.Security.Cryptography;

namespace CloudStorage.API.Services
{
    public interface IBlobService
    {
        Stream GetBlobStream(IUser user, IBlobDetail blobDetail);
    }
    public class BlobService : IBlobService
    {
        private readonly ILogger<BlobService> _logger;
        private readonly AppSettings _settings;

        public BlobService(ILogger<BlobService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _settings = configuration.Get<AppSettings>() ?? throw new Exception("Unable to get AppSettings");
        }

        public Stream GetBlobStream(IUser user, IBlobDetail blobDetail)
        {
            if (blobDetail?.Private == true)
            {
                MemoryStream memoryStream = new MemoryStream();

                BlobContainerClient blobContainerClient = new BlobContainerClient(_settings.BlobStorage.ConnectionString, blobDetail!.ContainerName);
                BlobClient blobClient = blobContainerClient.GetBlobClient($"{blobDetail.BlobName}.{blobDetail.FileExtension}");

                blobClient.DownloadTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                Aes aes = Aes.Create();
                aes.Key = Convert.FromBase64String(user!.PrivateKey);
                aes.IV = Convert.FromBase64String(blobDetail.IV!);
                int length = (int)memoryStream.Length;
                byte[] buffer = new byte[length];
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    using (Stream reader = Stream.Synchronized(cryptoStream))
                    {
                        int readBytes = reader.Read(buffer, 0, length);
                    }
                }

                return new MemoryStream(buffer);
            }
            else
            {
                MemoryStream memoryStream = new MemoryStream();

                BlobContainerClient blobContainerClient = new BlobContainerClient(_settings.BlobStorage.ConnectionString, blobDetail!.ContainerName);
                BlobClient blobClient = blobContainerClient.GetBlobClient($"{blobDetail.BlobName}.{blobDetail.FileExtension}");

                blobClient.DownloadTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                return memoryStream;
            }
        }
    }
}
