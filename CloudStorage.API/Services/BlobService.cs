using Azure.Storage.Blobs;
using CloudStorage.API.Models;
using CloudStorage.Interfaces;
using CloudStorage.Models;
using System.Security.Cryptography;
using CloudStorage;
using Microsoft.Azure.Cosmos;

namespace CloudStorage.API.Services
{
    public interface IBlobService
    {
        Stream GetBlobStream(IUser user, IBlobDetail blobDetail);
        Task UploadThumbnailStream(IBlobDetail blobDetail, Stream stream);
        Task UploadStream(IUser user, IBlobDetail blobDetail, Stream stream, bool isPrivate);
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
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task UploadThumbnailStream(IBlobDetail blobDetail, Stream stream)
        {
            try
            {
                BlobContainerClient blobContainerClient = new BlobContainerClient(_settings.BlobStorage.ConnectionString, "thumbnails");
                byte[] data = await CloudStorage.Worker.CreateThumbnail(stream, Consts.Blob.IMAGE_SIZE, Consts.Blob.IMAGE_SIZE);
                BinaryData binaryData = new BinaryData(data);
                BlobClient blobClient = blobContainerClient.GetBlobClient($"{blobDetail!.Thumbnail}");
                await blobClient.UploadAsync(binaryData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
        public async Task UploadStream(IUser user, IBlobDetail blobDetail, Stream stream, bool isPrivate = false)
        {
            try
            {
                if (isPrivate)
                {
                    MemoryStream memoryStream = new MemoryStream();
                    BlobContainerClient blobContainerClient = new BlobContainerClient(_settings.BlobStorage.ConnectionString, blobDetail!.ContainerName);
                    BlobClient blobClient = blobContainerClient.GetBlobClient($"{blobDetail!.BlobName}.{blobDetail.FileExtension}");

                    Aes aes = Aes.Create();
                    aes.Key = Convert.FromBase64String(user!.PrivateKey);
                    aes.IV = Convert.FromBase64String(blobDetail.IV!);
                    stream!.Seek(0, SeekOrigin.Begin);
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        int bytesRead = 0;
                        byte[] buffer = new byte[512];
                        do
                        {
                            bytesRead = await stream.ReadAsync(buffer, 0, 512);
                            cryptoStream.Write(buffer, 0, bytesRead);
                        } while (bytesRead > 0);

                        memoryStream.Position = 0;
                        await blobClient.UploadAsync(memoryStream);
                    }

                    memoryStream.Dispose();
                }
                else
                {
                    BlobContainerClient blobContainerClient = new BlobContainerClient(_settings.BlobStorage.ConnectionString, blobDetail!.ContainerName);
                    BlobClient blobClient = blobContainerClient.GetBlobClient($"{blobDetail.BlobName}.{blobDetail.FileExtension}");
                    stream!.Seek(0, SeekOrigin.Begin);
                    await blobClient.UploadAsync(stream);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
    }
}
