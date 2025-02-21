using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using CloudStorage.API.Models;
using CloudStorage.Models;
using Microsoft.Extensions.Options;
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
        public async Task<IActionResult> UploadImage()
        {
            IBlobDetail? blobDetail = null;
            FileUpload? fileUpload = null;
            int detailsSize = 0;
            Stream? fileDataStream = null;
            byte[] buffer = new byte[512];
            int bytesRead = 0;

            try
            {
                JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(Request);
                string userId = jwtPayload.Subject;
                IUser user = await _database.GetUserByIdAsync(userId);

                // Read details size
                bytesRead = await Request.Body.ReadAsync(buffer, 0, 4);
                detailsSize = BitConverter.ToInt32(buffer, 0);

                // Read Upload Details
                await Request.Body.ReadAsync(buffer, 0, detailsSize);
                string detailsContent = Encoding.UTF8.GetString(buffer,0, detailsSize);
                fileUpload = JsonConvert.DeserializeObject<FileUpload>(detailsContent);

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

                fileDataStream = await CloudStorage.Worker.CopyTo(Request.Body);
                fileDataStream.Seek(0, SeekOrigin.Begin);

                if (fileUpload.CreateThumbnail)
                {
                    await _blob.UploadThumbnailStream(blobDetail, fileDataStream);
                }

                await _blob.UploadStream(user, blobDetail, fileDataStream, fileUpload.IsPrivate);
                await _database.CreateBlobDetailsByIdAsync(blobDetail);

                return new OkResult();
            }
            catch (Exception ex) {
                fileDataStream?.Dispose();
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> DeleteImage([FromRoute(Name = "id")] string pBlobDetailId)
        {
            
            string? userId = null;

            try
            {
                JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(Request);
                userId = jwtPayload.Subject;

                IBlobDetail blobDetail = await _database.GetBlobDetailsByIdAsync(userId, pBlobDetailId);
                blobDetail.Deleted = true;
                await _database.UpdateBlobDetail(blobDetail);
                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpGet]
        [Route("thumbnail/{id}")]
        public async Task<Stream?> GetThumbnail([FromRoute(Name = "id")] string blobDetailId)
        {
            try
            {
                JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(Request);
                string userId = jwtPayload.Subject;
                IBlobDetail blobDetail = await _database.GetBlobDetailsByIdAsync(blobDetailId, userId);

                if (!string.IsNullOrEmpty(blobDetail!.Thumbnail))
                {
                    Stream thumbnailStream = _blob.GetBlobThumbnail(blobDetail);

                    Response.ContentType = Worker.GetContentType(blobDetail.FileExtension);
                    this.Response.ContentType = "image/jpeg";
                    return thumbnailStream;
                }
                else
                {
                    Response.StatusCode = 404;
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        /*
        [HttpGet]
        [Route("genThumbnails")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateThumbnails([FromHeader(Name = "ApiKey")] string pApiKey)
        {
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
        */
    }
}
