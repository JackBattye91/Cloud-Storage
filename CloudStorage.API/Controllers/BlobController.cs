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
            IList<BlobDetail> blobDetailsList = new List<BlobDetail>();
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
                    IReturnCode<IList<BlobDetail>> getBlobDetailRc = await NoSqlWrapper.GetItems<BlobDetail>(AppSettings.Database.Database, Database.PICTURES_CONTAINER_NAME, query);

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
        public async Task<Stream?> GetImage([FromHeader(Name = "Authorization")]string pBearerToken, [FromRoute(Name = "id")]string fileName)
        {
            IReturnCode rc = new ReturnCode();
            BlobDetail? blobDetail = null;
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
                    string query = $"SELECT * FROM c WHERE c.fileName = '{fileName}' AND c.userId = '{userId}' AND c.deleted = false";
                    IReturnCode<IList<BlobDetail>> getBlobDetailRc = await NoSqlWrapper.GetItems<BlobDetail>(AppSettings.Database.Database, Database.PICTURES_CONTAINER_NAME, query);

                    if (getBlobDetailRc.Success)
                    {
                        if (getBlobDetailRc.Data?.Count == 1)
                        {
                            blobDetail = getBlobDetailRc.Data[0];
                        }
                        else
                        {
                            throw new JBException("Unable to get blob details");
                        }
                    }

                    if (getBlobDetailRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getBlobDetailRc, rc);
                    }
                }

                if (rc.Success)
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

        [HttpPost]
        public async Task<IActionResult> UploadImage([FromHeader(Name = "Authorization")] string pBearerToken, [FromBody] FileUpload pFileUpload)
        {
            IReturnCode rc = new ReturnCode();
            BlobDetail? blobDetail = null;
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
                    blobDetail = new BlobDetail();
                    blobDetail.FileName = pFileUpload.FileName;
                    blobDetail.ContainerName = pFileUpload.ContainerName;
                    blobDetail.BlobName = Guid.NewGuid().ToString();
                    blobDetail.FileExtension = pFileUpload.FileExtension;
                    blobDetail.UserId = userId!;
                    blobDetail.Id = Guid.NewGuid().ToString();
                    blobDetail.Thumbnail = Worker.CreateBase64Thumbnail(pFileUpload.DataBase64, 256, 256);
                }

                if (rc.Success)
                {
                    IReturnCode<BlobDetail> createBlobDetailRc = await NoSqlWrapper.AddItem<BlobDetail>(AppSettings.Database.Database, Database.PICTURES_CONTAINER_NAME, blobDetail!);

                    if (createBlobDetailRc.Failed)
                    {
                        ErrorWorker.CopyErrors(createBlobDetailRc, rc);
                    }
                }

                if (rc.Success)
                {
                    BlobContainerClient blobContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, blobDetail!.ContainerName);
                    byte[] data = Convert.FromBase64String(pFileUpload.DataBase64);
                    BinaryData binaryData = new BinaryData(data);
                    BlobClient blobClient = blobContainerClient.GetBlobClient($"{blobDetail.BlobName}.{blobDetail.FileExtension}");
                    await blobClient.UploadAsync(binaryData);
                }

                return new OkResult();
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
        public async Task<IActionResult> DeleteImage([FromHeader(Name = "Authorization")] string pBearerToken, [FromRoute(Name = "id")] string fileName)
        {
            IReturnCode rc = new ReturnCode();
            BlobDetail? blobDetail = null;
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
                    string query = $"SELECT * FROM c WHERE c.fileName = '{fileName}' AND c.userId = '{userId}' AND c.deleted = false";
                    IReturnCode<IList<BlobDetail>> getBlobDetailRc = await NoSqlWrapper.GetItems<BlobDetail>(AppSettings.Database.Database, Database.PICTURES_CONTAINER_NAME, query);

                    if (getBlobDetailRc.Success)
                    {
                        if (getBlobDetailRc.Data?.Count == 1)
                        {
                            blobDetail = getBlobDetailRc.Data[0];
                        }
                        else
                        {
                            throw new JBException("Unable to get blob details");
                        }
                    }

                    if (getBlobDetailRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getBlobDetailRc, rc);
                    }
                }

                if (rc.Success)
                {
                    blobDetail!.Deleted = true;
                    IReturnCode<BlobDetail> updateBlobDetailRc = await NoSqlWrapper.UpdateItem<BlobDetail>(AppSettings.Database.Database, Database.PICTURES_CONTAINER_NAME, blobDetail!, blobDetail.Id, blobDetail.ContainerName);

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
        [Route("thumbnail")]
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
                    IReturnCode<IList<IBlobDetail>> getBlobDetailsRc = await NoSqlWrapper.GetItems<IBlobDetail, BlobDetail>(AppSettings.Database.Database, Database.PICTURES_CONTAINER_NAME);

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

                            BlobContainerClient blobContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, blob!.ContainerName);
                            BlobClient blobClient = blobContainerClient.GetBlobClient($"{blob.BlobName}.{blob.FileExtension}");

                            blobClient.DownloadTo(memoryStream);
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            string b64Image = Convert.ToBase64String(memoryStream.ToArray());

                            blob.Thumbnail = Worker.CreateBase64Thumbnail(b64Image, 256, 256);
                        }
                    }
                   
                }

                if (rc.Success)
                {
                    foreach(var blob in blobDetailsList)
                    {
                        IReturnCode<IBlobDetail> createBlobDetailRc = await NoSqlWrapper.UpdateItem<IBlobDetail, BlobDetail>(AppSettings.Database.Database, Database.PICTURES_CONTAINER_NAME, blob, blob.Id, blob.ContainerName);

                        if (createBlobDetailRc.Failed)
                        {
                            ErrorWorker.CopyErrors(createBlobDetailRc, rc);
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
