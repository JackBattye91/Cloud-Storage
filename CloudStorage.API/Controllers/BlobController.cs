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

namespace CloudStorage.API.Controllers
{

    [ApiController]
    [Route("[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class BlobController : Controller
    {
        private ILogger<BlobController> Logger;
        private AppSettings AppSettings { get; set; }
        private IWrapper NoSqlWrapper { get; set; }

        public BlobController(ILogger<BlobController> pLogger, IOptions<AppSettings> pAppSettings)
        {
            Logger = pLogger;
            AppSettings = pAppSettings.Value;
            NoSqlWrapper = Factory.CreateNoSqlDatabaseWrapper(AppSettings.Database.ConnectionString);
        }

        [HttpGet]
        public async Task<IActionResult> GetBlobDetails([FromHeader(Name = "Authorization")] string pBearerToken, [FromHeader(Name = "deleted")]bool pShowDeleted = false)
        {
            IReturnCode rc = new ReturnCode();
            IList<IBlobDetail> blobDetailsList = new List<IBlobDetail>();
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
                    string showDeletedBlobs = pShowDeleted ? "" : "AND c.deleted = false";
                    string query = $"SELECT * FROM c WHERE c.userId = '{userId}' {showDeletedBlobs}";
                    IReturnCode<IList<IBlobDetail>> getBlobDetailRc = await NoSqlWrapper.GetItems<IBlobDetail, BlobDetail>(Consts.Database.DATABASE, Database.PICTURES_CONTAINER_NAME, query);

                    if (getBlobDetailRc.Success)
                    {
                        blobDetailsList = getBlobDetailRc.Data!;
                    }

                    if (getBlobDetailRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getBlobDetailRc, rc);
                    }
                }

                if (rc.Success)
                {
                    return new OkObjectResult(blobDetailsList);
                }
            }
            catch (Exception ex)
            {
                rc.AddError(new Error(2, ex));
            }

            if (rc.Failed)
            {
                ErrorWorker.LogErrors(Logger, rc);
            }

            return StatusCode(500);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<Stream?> GetImage([FromHeader(Name = "Authorization")]string pBearerToken, [FromRoute(Name = "id")]string pBlobDetailId)
        {
            IReturnCode rc = new ReturnCode();
            IBlobDetail? blobDetail = null;
            IUser? user = null;
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
                    IReturnCode<IUser> getUserRc = await NoSqlWrapper.GetItem<IUser, User>(Consts.Database.DATABASE, Database.USER_CONTAINER_NAME, userId!);

                    if (getUserRc.Success)
                    {
                        user = getUserRc.Data;
                    }

                    if (getUserRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getUserRc, rc);
                    }
                }

                if (rc.Success)
                {
                    if (blobDetail?.Private == true)
                    {
                        MemoryStream memoryStream = new MemoryStream();

                        BlobContainerClient blobContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, blobDetail!.ContainerName);
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

                        Response.ContentType = Worker.GetContentType(blobDetail.FileExtension);
                        return new MemoryStream(buffer);
                    }
                    else
                    {
                        MemoryStream memoryStream = new MemoryStream();

                        BlobContainerClient blobContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, blobDetail!.ContainerName);
                        BlobClient blobClient = blobContainerClient.GetBlobClient($"{blobDetail.BlobName}.{blobDetail.FileExtension}");

                        blobClient.DownloadTo(memoryStream);
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        Response.ContentType = Worker.GetContentType(blobDetail.FileExtension);
                        return memoryStream;
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

            Response.StatusCode = (int)ErrorWorker.GetStatusCode(rc);
            return null;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage([FromHeader(Name = "Authorization")] string pBearerToken, [FromBody] FileUpload pFileUpload)
        {
            IReturnCode rc = new ReturnCode();
            IBlobDetail? blobDetail = null;
            IUser? user = null;
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
                    IReturnCode<IUser> getUserRc = await NoSqlWrapper.GetItem<IUser, User>(Consts.Database.DATABASE, Database.USER_CONTAINER_NAME, userId!);

                    if (getUserRc.Success)
                    {
                        user = getUserRc.Data;
                    }

                    if (getUserRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getUserRc, rc);
                    }
                }

                if (rc.Success)
                {
                    blobDetail = new BlobDetail
                    {
                        FileName = pFileUpload.FileName,
                        ContainerName = pFileUpload.IsPrivate ? "private" : pFileUpload.ContainerName,
                        BlobName = Guid.NewGuid().ToString(),
                        FileExtension = pFileUpload.FileExtension,
                        UserId = userId!,
                        Id = Guid.NewGuid().ToString(),
                        Thumbnail = pFileUpload.IsPrivate ? null : Guid.NewGuid().ToString(),
                        Private = pFileUpload.IsPrivate,
                        IV = pFileUpload.IsPrivate ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)) : null,
                        Created = DateTime.UtcNow
                    };
                }

                if (rc.Success)
                {
                    try
                    {
                        if (!pFileUpload.IsPrivate)
                        {
                            BlobContainerClient blobContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, "thumbnail");
                            byte[] data = Convert.FromBase64String(Worker.CreateBase64Thumbnail(pFileUpload.DataBase64, Consts.Blob.IMAGE_SIZE, Consts.Blob.IMAGE_SIZE));
                            BinaryData binaryData = new BinaryData(data);
                            BlobClient blobClient = blobContainerClient.GetBlobClient($"{blobDetail!.Thumbnail}.{blobDetail.FileExtension}");
                            await blobClient.UploadAsync(binaryData);
                        }                        
                    }
                    catch(Exception ex)
                    {
                        rc.AddError(new Error(789, ex, JB.Common.Consts.ErrorType.WARNING));
                    }
                }

                if (rc.Success)
                {
                    if (pFileUpload.IsPrivate)
                    {
                        MemoryStream memoryStream = new MemoryStream();
                        BlobContainerClient blobContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, blobDetail!.ContainerName);
                        BlobClient blobClient = blobContainerClient.GetBlobClient($"{blobDetail!.BlobName}.{blobDetail.FileExtension}");

                        Aes aes = Aes.Create();
                        aes.Key = Convert.FromBase64String(user!.PrivateKey);
                        aes.IV = Convert.FromBase64String(blobDetail.IV!);
                        byte[] data = Convert.FromBase64String(pFileUpload.DataBase64);
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(data);
                            memoryStream.Position = 0;
                            await blobClient.UploadAsync(memoryStream);
                        }
                    }
                    else
                    {
                        BlobContainerClient blobContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, blobDetail!.ContainerName);
                        byte[] data = Convert.FromBase64String(pFileUpload.DataBase64);
                        BinaryData binaryData = new BinaryData(data);
                        BlobClient blobClient = blobContainerClient.GetBlobClient($"{blobDetail.BlobName}.{blobDetail.FileExtension}");
                        await blobClient.UploadAsync(binaryData);
                    }
                    
                }

                if (rc.Success)
                {
                    IReturnCode<IBlobDetail> createBlobDetailRc = await NoSqlWrapper.AddItem<IBlobDetail, BlobDetail>(Consts.Database.DATABASE, Database.PICTURES_CONTAINER_NAME, blobDetail!);

                    if (createBlobDetailRc.Failed)
                    {
                        ErrorWorker.CopyErrors(createBlobDetailRc, rc);
                    }
                }

                return new CreatedResult();
            }
            catch (Exception ex) {
                rc.AddError(new Error(4, ex));
            }

            if (rc.Failed)
            {
                ErrorWorker.LogErrors(Logger, rc);
            }

            return StatusCode(500);
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
                    if (!blobDetail!.Private)
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
                            byte[] thumbnailData = Worker.CreateThumbnail(b64Image, Consts.Blob.IMAGE_SIZE, Consts.Blob.IMAGE_SIZE);
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
