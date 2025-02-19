using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using CloudStorage.API.Models;
using CloudStorage.Models;
using Microsoft.Extensions.Options;
using JB.NoSqlDatabase;
using JB.Common;
using JB.Common.Errors;
using CloudStorage.API.Consts;
using Microsoft.Extensions.Logging;
using CloudStorage.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;
using System;
using CloudStorage.API.Services;

namespace CloudStorage.API.Controllers
{

    [ApiController]
    [Route("[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class BlobController : Controller
    {
        private ILogger<BlobController> _logger;
        private IDatabaseService _database;
        private IBlobService _blob;


        public BlobController(ILogger<BlobController> logger, IDatabaseService databaseService, IBlobService blobService)
        {
            _logger = logger;
            _database = databaseService;
            _blob = blobService;
        }

        [HttpGet]
        public async Task<IActionResult> GetBlobDetails([FromHeader(Name = "deleted")]bool pShowDeleted = false)
        {
            try
            {
                JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(Request);
                string userId = jwtPayload.Subject;

                IEnumerable<IBlobDetail>  blobDetailsList = await _database.GetBlobDetailsByUserIdAsync(userId);
                return new OkObjectResult(blobDetailsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<Stream?> GetImage([FromRoute(Name = "id")]string pBlobDetailId)
        {
            try
            {
                JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(Request);
                string userId = jwtPayload.Subject;

                IUser user = await _database.GetUserByIdAsync(userId);
                IBlobDetail blobDetail = await _database.GetBlobDetailsByIdAsync(pBlobDetailId, userId);

                Stream getBlobStream = _blob.GetBlobStream(user, blobDetail);

                Response.ContentType = Worker.GetContentType(blobDetail.FileExtension);
                return getBlobStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage([FromHeader(Name = "Authorization")] string pBearerToken)
        {
            IBlobDetail? blobDetail = null;
            FileUpload? fileUpload = null;
            int detailsSize = 0;
            Stream? fileDataStream = null;

            try
            {
                JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(Request);
                string userId = jwtPayload.Subject;
                IUser user = await _database.GetUserByIdAsync(userId);

                if (rc.Success)
                {
                    byte[] buffer = new byte[512];
                    int bytesRead = 0;

                    // Read details size
                    bytesRead = await Request.Body.ReadAsync(buffer, 0, 4);
                    detailsSize = BitConverter.ToInt32(buffer, 0);

                    // read Upload Details
                    await Request.Body.ReadAsync(buffer, 0, detailsSize);
                    string detailsContent = Encoding.UTF8.GetString(buffer,0, detailsSize);
                    fileUpload = JsonConvert.DeserializeObject<FileUpload>(detailsContent);
                }

                blobDetail = new BlobDetail
                {
                    Id = Guid.NewGuid().ToString(),
                    FileName = fileUpload!.FileName,
                    ContainerName = fileUpload.IsPrivate ? "private" : fileUpload.ContainerName,
                    BlobName = $"{Guid.NewGuid()}_{DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss")}",
                    FileExtension = fileUpload.FileExtension.Trim('.'),
                    UserId = userId!,
                    Thumbnail = fileUpload.CreateThumbnail ? $"{Guid.NewGuid()}.{fileUpload.FileExtension.Trim('.')}" : null,
                    Private = fileUpload.IsPrivate,
                    IV = fileUpload.IsPrivate ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)) : null,
                    Created = DateTime.UtcNow
                };

                try
                {
                    fileDataStream = await CloudStorage.Worker.CopyTo(Request.Body);
                    fileDataStream.Seek(0, SeekOrigin.Begin);

                    if (fileUpload!.CreateThumbnail)
                    {
                        BlobContainerClient blobContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, "thumbnails");
                        byte[] data = await CloudStorage.Worker.CreateThumbnail(fileDataStream, Consts.Blob.IMAGE_SIZE, Consts.Blob.IMAGE_SIZE);
                        BinaryData binaryData = new BinaryData(data);
                        BlobClient blobClient = blobContainerClient.GetBlobClient($"{blobDetail!.Thumbnail}");
                        await blobClient.UploadAsync(binaryData);
                    }                        
                }
                catch(Exception ex)
                {
                    throw;
                }

                if (fileUpload!.IsPrivate)
                {
                    MemoryStream memoryStream = new MemoryStream();
                    BlobContainerClient blobContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, blobDetail!.ContainerName);
                    BlobClient blobClient = blobContainerClient.GetBlobClient($"{blobDetail!.BlobName}.{blobDetail.FileExtension}");

                    Aes aes = Aes.Create();
                    aes.Key = Convert.FromBase64String(user!.PrivateKey);
                    aes.IV = Convert.FromBase64String(blobDetail.IV!);
                    fileDataStream!.Seek(0, SeekOrigin.Begin);
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        int bytesRead = 0;
                        byte[] buffer = new byte[512];
                        do
                        {
                            bytesRead = await fileDataStream.ReadAsync(buffer, 0, 512);
                            cryptoStream.Write(buffer, 0, bytesRead);
                        } while (bytesRead > 0);

                        memoryStream.Position = 0;
                        await blobClient.UploadAsync(memoryStream);
                    }

                    memoryStream.Dispose();
                }
                else
                {
                    BlobContainerClient blobContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, blobDetail!.ContainerName);
                    BlobClient blobClient = blobContainerClient.GetBlobClient($"{blobDetail.BlobName}.{blobDetail.FileExtension}");
                    fileDataStream!.Seek(0, SeekOrigin.Begin);
                    await blobClient.UploadAsync(fileDataStream);
                }

                await _database.CreateBlobDetailsByIdAsync(blobDetail);
            }
            catch (Exception ex) {
                fileDataStream?.Dispose();
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> DeleteImage([FromHeader(Name = "Authorization")] string pBearerToken, [FromRoute(Name = "id")] string pBlobDetailId)
        {
            IReturnCode rc = new ReturnCode();
            IBlobDetail? blobDetail = null;
            string? userId = null;

            try
            {
                if (rc.Success)
                {
                    JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(pBearerToken);
                    userId = jwtPayload.Subject;
                }

                if (rc.Success)
                {
                    IReturnCode<IBlobDetail> getBlobDetailRc = await NoSqlWrapper.GetItem<IBlobDetail, BlobDetail>(Consts.Database.DATABASE, Database.PICTURES_CONTAINER_NAME, pBlobDetailId);

                    if (getBlobDetailRc.Success)
                    {
                        blobDetail = getBlobDetailRc.Data;
                    }

                    if (getBlobDetailRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getBlobDetailRc, rc);
                    }
                }

                if (rc.Success)
                {
                    blobDetail!.Deleted = true;
                    IReturnCode<IBlobDetail> updateBlobDetailRc = await NoSqlWrapper.UpdateItem<IBlobDetail, BlobDetail>(Consts.Database.DATABASE, Database.PICTURES_CONTAINER_NAME, blobDetail!, blobDetail.Id, blobDetail.ContainerName);

                    if (updateBlobDetailRc.Failed)
                    {
                        ErrorWorker.CopyErrors(updateBlobDetailRc, rc);
                    }
                }

                return new OkResult();
            }
            catch (Exception ex)
            {
                rc.AddError(new Error(5, ex));
            }

            if (rc.Failed)
            {
                ErrorWorker.LogErrors(Logger, rc);
            }

            return StatusCode(500);
        }

        [HttpGet]
        [Route("thumbnail/{id}")]
        public async Task<Stream?> GetThumbnail([FromHeader(Name = "Authorization")] string pBearerToken, [FromRoute(Name = "id")] string pBlobDetailId)
        {
            IReturnCode rc = new ReturnCode();
            IBlobDetail? blobDetail = null;
            string? userId = null;

            try
            {
                if (rc.Success)
                {
                    JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(pBearerToken);
                    userId = jwtPayload.Subject;
                }

                if (rc.Success)
                {
                    string query = $"SELECT * FROM c WHERE c.id = '{pBlobDetailId}' AND c.userId = '{userId}' AND c.deleted = false";
                    IReturnCode<IList<IBlobDetail>> getBlobDetailRc = await NoSqlWrapper.GetItems<IBlobDetail, BlobDetail>(Consts.Database.DATABASE, Database.PICTURES_CONTAINER_NAME, query);

                    if (getBlobDetailRc.Success)
                    {
                        if (getBlobDetailRc.Data?.Count > 0)
                        {
                            blobDetail = getBlobDetailRc.Data[0];
                        }
                        else
                        {
                            rc.AddError(new NetworkError(45, System.Net.HttpStatusCode.NotFound));
                        }
                    }

                    if (getBlobDetailRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getBlobDetailRc, rc);
                    }
                }

                if (rc.Success)
                {
                    if (!string.IsNullOrEmpty(blobDetail!.Thumbnail))
                    {
                        MemoryStream memoryStream = new MemoryStream();

                        BlobContainerClient blobContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, "thumbnails");
                        BlobClient blobClient = blobContainerClient.GetBlobClient(blobDetail!.Thumbnail);

                        blobClient.DownloadTo(memoryStream);
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        Response.ContentType = Worker.GetContentType(blobDetail.FileExtension);
                        this.Response.ContentType = "image/jpeg";
                        return memoryStream;
                    }
                    else
                    {
                        Response.StatusCode = 404;
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                rc.AddError(new Error(3, ex));
            }

            if (rc.Failed)
            {
                ErrorWorker.LogErrors(Logger, rc);
            }

            Response.StatusCode = 500;
            return null;
        }

        [HttpGet]
        [Route("genThumbnails")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateThumbnails([FromHeader(Name = "ApiKey")] string pApiKey)
        {
            IReturnCode rc = new ReturnCode();
            IList<IBlobDetail> blobDetailsList = new List<IBlobDetail>();

            try
            {
                if (rc.Success)
                {
                    if (!string.Equals(pApiKey, AppSettings.BlobStorage.ThumbnailApiKey, StringComparison.OrdinalIgnoreCase))
                    {
                        return new UnauthorizedResult();
                    }
                }

                if (rc.Success)
                {
                    IReturnCode<IList<IBlobDetail>> getBlobDetailsRc = await NoSqlWrapper.GetItems<IBlobDetail, BlobDetail>(Consts.Database.DATABASE, Database.PICTURES_CONTAINER_NAME);

                    if (getBlobDetailsRc.Success)
                    {
                        blobDetailsList = getBlobDetailsRc.Data!;
                    }

                    if (getBlobDetailsRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getBlobDetailsRc, rc);
                    }
                }

                if (rc.Success)
                {
                    foreach(var blob in blobDetailsList)
                    {
                        if (string.IsNullOrEmpty(blob.Thumbnail))
                        {
                            MemoryStream memoryStream = new MemoryStream();

                            BlobContainerClient imageBlobContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, blob!.ContainerName);
                            BlobClient imageBlobClient = imageBlobContainerClient.GetBlobClient($"{blob.BlobName}.{blob.FileExtension}");

                            imageBlobClient.DownloadTo(memoryStream);
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            string b64Image = Convert.ToBase64String(memoryStream.ToArray());

                            BlobContainerClient thumbnailBlobContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, "thumbnails");
                            byte[] thumbnailData = CloudStorage.Worker.CreateThumbnail(b64Image, Consts.Blob.IMAGE_SIZE, Consts.Blob.IMAGE_SIZE);
                            BinaryData thumbnailBinaryData = new BinaryData(thumbnailData);
                            blob.Thumbnail = $"{Guid.NewGuid()}.jpeg";
                            BlobClient thumbnailBlobClient = thumbnailBlobContainerClient.GetBlobClient(blob.Thumbnail);
                            await thumbnailBlobClient.UploadAsync(thumbnailBinaryData);
                        }
                    }
                }

                if (rc.Success)
                {
                    foreach(IBlobDetail blobDetail in blobDetailsList)
                    {
                        IReturnCode<IBlobDetail> getBlobDetailsRc = await NoSqlWrapper.UpdateItem<IBlobDetail, BlobDetail>(Consts.Database.DATABASE, Database.PICTURES_CONTAINER_NAME, blobDetail, blobDetail.Id, blobDetail.ContainerName);

                        if (getBlobDetailsRc.Failed)
                        {
                            ErrorWorker.CopyErrors(getBlobDetailsRc, rc);
                        }
                    }
                }

                return new OkResult();
            }
            catch (Exception ex)
            {
                rc.AddError(new Error(4, ex));
            }

            if (rc.Failed)
            {
                ErrorWorker.LogErrors(Logger, rc);
            }

            return StatusCode(500);
        }
    }
}
