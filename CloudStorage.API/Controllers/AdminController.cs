using Azure.Storage.Blobs;
using CloudStorage.API.Consts;
using CloudStorage.API.Models;
using CloudStorage.Interfaces;
using CloudStorage.Models;
using JB.Common;
using JB.NoSqlDatabase;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IO;

namespace CloudStorage.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AdminController : Controller
    {
        private ILogger<AdminController> Logger;
        private AppSettings AppSettings { get; set; }
        private IWrapper NoSqlWrapper { get; set; }

        public AdminController(ILogger<AdminController> pLogger, IOptions<AppSettings> pAppSettings)
        {
            Logger = pLogger;
            AppSettings = pAppSettings.Value;
            NoSqlWrapper = Factory.CreateNoSqlDatabaseWrapper(AppSettings.Database.ConnectionString);
        }

        #region REFRESH TOKENS
        [HttpGet]
        [Route("refreshToken/clear/all")]
        public async Task<IActionResult> ClearRefreshTokens([FromHeader(Name = "Authorization")] string pBearerToken)
        {
            IReturnCode rc = new ReturnCode();
            IList<RefreshToken>? refreshTokens = null;

            try
            {
                if (rc.Success)
                {
                    JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(pBearerToken);
                    if (!string.Equals(jwtPayload.Permissions, CloudStorage.Consts.Permission.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return new UnauthorizedResult();
                    }
                }

                if (rc.Success)
                {
                    IReturnCode<IList<RefreshToken>> getRefreshTokenRc = await NoSqlWrapper.GetItems<RefreshToken>(Consts.Database.DATABASE, Database.REFRESH_TOKEN_CONTAINER_NAME);

                    if (getRefreshTokenRc.Success)
                    {
                        refreshTokens = getRefreshTokenRc.Data;
                    }

                    if (getRefreshTokenRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getRefreshTokenRc, rc);
                    }
                }

                if (rc.Success)
                {
                    foreach(RefreshToken token in refreshTokens!)
                    {
                        IReturnCode deleteRefreshTokenRc = await NoSqlWrapper.DeleteItem<RefreshToken>(Consts.Database.DATABASE, Database.REFRESH_TOKEN_CONTAINER_NAME, token.Id, token.UserId);

                        if (deleteRefreshTokenRc.Failed)
                        {
                            ErrorWorker.CopyErrors(deleteRefreshTokenRc, rc);
                        }
                    }
                }

                return new OkResult();
            }
            catch (Exception ex) {
                Logger.LogError(ex.Message);
            }

            return StatusCode(500); 
        }

        [HttpGet]
        [Route("refreshToken/clear/user/{id}")]
        public async Task<IActionResult> ClearRefreshTokens([FromHeader(Name = "Authorization")] string pBearerToken, [FromRoute(Name = "id")]string pUserId)
        {
            IReturnCode rc = new ReturnCode();
            IList<RefreshToken>? refreshTokens = null;

            try
            {
                if (rc.Success)
                {
                    JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(pBearerToken);
                    if (!string.Equals(jwtPayload.Permissions, CloudStorage.Consts.Permission.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return new UnauthorizedResult();
                    }
                }

                if (rc.Success)
                {
                    string query = $"SELECT * FROM c WHERE c.userId = '{pUserId}'";
                    IReturnCode<IList<RefreshToken>> getRefreshTokenRc = await NoSqlWrapper.GetItems<RefreshToken>(Consts.Database.DATABASE, Database.REFRESH_TOKEN_CONTAINER_NAME, query);

                    if (getRefreshTokenRc.Success)
                    {
                        refreshTokens = getRefreshTokenRc.Data;
                    }

                    if (getRefreshTokenRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getRefreshTokenRc, rc);
                    }
                }

                if (rc.Success)
                {
                    foreach (RefreshToken token in refreshTokens!)
                    {
                        IReturnCode deleteRefreshTokenRc = await NoSqlWrapper.DeleteItem<RefreshToken>(Consts.Database.DATABASE, Database.REFRESH_TOKEN_CONTAINER_NAME, token.Id, token.UserId);

                        if (deleteRefreshTokenRc.Failed)
                        {
                            ErrorWorker.CopyErrors(deleteRefreshTokenRc, rc);
                        }
                    }
                }

                return new OkResult();
            }
            catch (Exception ex) 
            {
                Logger.LogError(ex.Message);
            }

            return StatusCode(500);
        }

        [HttpGet]
        [Route("refreshToken/clear/expired")]
        [AllowAnonymous]
        public async Task<IActionResult> ClearExpiredRefreshTokens()
        {
            IReturnCode rc = new ReturnCode();
            IList<RefreshToken>? refreshTokens = null;

            try
            {
                if (rc.Success)
                {
                    string query = $"SELECT * FROM c WHERE c.created <= '{DateTime.UtcNow.AddSeconds(-AppSettings.Jwt.ExpiresAfterSeconds).ToString("yyyy-MM-ddThh:mm:ss:fffffffZ")}'";
                    IReturnCode<IList<RefreshToken>> getRefreshTokenRc = await NoSqlWrapper.GetItems<RefreshToken>(Consts.Database.DATABASE, Database.REFRESH_TOKEN_CONTAINER_NAME, query);

                    if (getRefreshTokenRc.Success)
                    {
                        refreshTokens = getRefreshTokenRc.Data;
                    }

                    if (getRefreshTokenRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getRefreshTokenRc, rc);
                    }
                }

                if (rc.Success)
                {
                    foreach (RefreshToken token in refreshTokens!)
                    {
                        IReturnCode deleteRefreshTokenRc = await NoSqlWrapper.DeleteItem<RefreshToken>(Consts.Database.DATABASE, Database.REFRESH_TOKEN_CONTAINER_NAME, token.Id, token.UserId);

                        if (deleteRefreshTokenRc.Failed)
                        {
                            ErrorWorker.CopyErrors(deleteRefreshTokenRc, rc);
                        }
                    }
                }

                if (rc.Success)
                {
                    return new OkObjectResult($"Deleted {refreshTokens!.Count} refresh tokens");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }

            return StatusCode(500);
        }
        #endregion

        #region PICTURES
        [HttpGet]
        [Route("pictures/clear/all")]
        public async Task<IActionResult> ClearDeletedPictures([FromHeader(Name = "Authorization")] string pBearerToken)
        {
            IReturnCode rc = new ReturnCode();
            IList<IBlobDetail>? blobDetails = null;

            try
            {
                if (rc.Success)
                {
                    JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(pBearerToken);
                    if (!string.Equals(jwtPayload.Permissions, CloudStorage.Consts.Permission.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return new UnauthorizedResult();
                    }
                }

                if (rc.Success)
                {
                    string query = $"SELECT * FROM c WHERE c.deleted = true";
                    IReturnCode<IList<IBlobDetail>> getRefreshTokenRc = await NoSqlWrapper.GetItems<IBlobDetail, BlobDetail>(Consts.Database.DATABASE, Database.PICTURES_CONTAINER_NAME, query);

                    if (getRefreshTokenRc.Success)
                    {
                        blobDetails = getRefreshTokenRc.Data;
                    }

                    if (getRefreshTokenRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getRefreshTokenRc, rc);
                    }
                }

                if (rc.Success)
                {
                    foreach (IBlobDetail blob in blobDetails!)
                    {
                        string containerName = blob!.Private ? "private" : blob.ContainerName;
                        BlobContainerClient pictureContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, containerName);
                        BlobClient pictureClient = pictureContainerClient.GetBlobClient($"{blob!.BlobName}.{blob.FileExtension}");
                        await pictureClient.DeleteAsync();

                        if (!string.IsNullOrEmpty(blob.Thumbnail))
                        {
                            BlobContainerClient thumbnailContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, "thumbnails");
                            BlobClient thumbnailClient = thumbnailContainerClient.GetBlobClient($"{blob.Thumbnail}");
                            await thumbnailClient.DeleteAsync();
                        }
                    }
                }

                if (rc.Success)
                {
                    foreach (IBlobDetail blob in blobDetails!)
                    {
                        IReturnCode deleteBlobDetailRc = await NoSqlWrapper.DeleteItem<IBlobDetail>(Consts.Database.DATABASE, Database.PICTURES_CONTAINER_NAME, blob.Id, blob.ContainerName);

                        if (deleteBlobDetailRc.Failed)
                        {
                            ErrorWorker.CopyErrors(deleteBlobDetailRc, rc);
                        }
                    }
                }

                return new OkResult();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }

            return StatusCode(500);
        }

        [HttpGet]
        [Route("pictures/clear/user/{id}")]
        public async Task<IActionResult> ClearDeletedPictures([FromHeader(Name = "Authorization")] string pBearerToken, [FromRoute(Name = "id")] string pUserId)
        {
            IReturnCode rc = new ReturnCode();
            IList<IBlobDetail>? blobDetails = null;

            try
            {
                if (rc.Success)
                {
                    JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(pBearerToken);
                    if (!string.Equals(jwtPayload.Permissions, CloudStorage.Consts.Permission.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return new UnauthorizedResult();
                    }
                }

                if (rc.Success)
                {
                    string query = $"SELECT * FROM c WHERE c.deleted = true AND c.userId = '{pUserId}'";
                    IReturnCode<IList<IBlobDetail>> getRefreshTokenRc = await NoSqlWrapper.GetItems<IBlobDetail, BlobDetail>(Consts.Database.DATABASE, Database.PICTURES_CONTAINER_NAME, query);

                    if (getRefreshTokenRc.Success)
                    {
                        blobDetails = getRefreshTokenRc.Data;
                    }

                    if (getRefreshTokenRc.Failed)
                    {
                        ErrorWorker.CopyErrors(getRefreshTokenRc, rc);
                    }
                }

                if (rc.Success)
                {
                    foreach (IBlobDetail blob in blobDetails!)
                    {
                        string containerName = blob!.Private ? "private" : blob.ContainerName;
                        BlobContainerClient pictureContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, containerName);
                        BlobClient pictureClient = pictureContainerClient.GetBlobClient($"{blob!.BlobName}.{blob.FileExtension}");
                        await pictureClient.DeleteAsync();

                        if (!string.IsNullOrEmpty(blob.Thumbnail))
                        {
                            BlobContainerClient thumbnailContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, "thumbnails");
                            BlobClient thumbnailClient = thumbnailContainerClient.GetBlobClient($"{blob.Thumbnail}");
                            await thumbnailClient.DeleteAsync();
                        }
                    }
                }

                if (rc.Success)
                {
                    foreach (IBlobDetail blob in blobDetails!)
                    {
                        IReturnCode deleteBlobDetailRc = await NoSqlWrapper.DeleteItem<IBlobDetail>(Consts.Database.DATABASE, Database.PICTURES_CONTAINER_NAME, blob.Id, blob.ContainerName);

                        if (deleteBlobDetailRc.Failed)
                        {
                            ErrorWorker.CopyErrors(deleteBlobDetailRc, rc);
                        }
                    }
                }

                return new OkResult();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }

            return StatusCode(500);
        }

        [HttpGet]
        [Route("pictures/clear/picture/{id}")]
        public async Task<IActionResult> ClearDeletedPicture([FromHeader(Name = "Authorization")] string pBearerToken, [FromRoute(Name = "id")] string pBlobDetailId)
        {
            IReturnCode rc = new ReturnCode();
            IBlobDetail? blobDetail = null;

            try
            {
                if (rc.Success)
                {
                    JwtPayload jwtPayload = Worker.GetJwtPayloadFromBearerToken(pBearerToken);
                    if (!string.Equals(jwtPayload.Permissions, CloudStorage.Consts.Permission.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return new UnauthorizedResult();
                    }
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
                    string containerName = blobDetail!.Private ? "private" : blobDetail.ContainerName;
                    BlobContainerClient pictureContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, containerName);
                    BlobClient pictureClient = pictureContainerClient.GetBlobClient($"{blobDetail!.BlobName}.{blobDetail.FileExtension}");
                    await pictureClient.DeleteAsync();

                    if (!string.IsNullOrEmpty(blobDetail.Thumbnail))
                    {
                        BlobContainerClient thumbnailContainerClient = new BlobContainerClient(AppSettings.BlobStorage.ConnectionString, "thumbnails");
                        BlobClient thumbnailClient = thumbnailContainerClient.GetBlobClient($"{blobDetail.Thumbnail}");
                        await thumbnailClient.DeleteAsync();
                    }
                }

                if (rc.Success)
                {
                    IReturnCode deleteBlobDetailRc = await NoSqlWrapper.DeleteItem<IBlobDetail>(Consts.Database.DATABASE, Database.PICTURES_CONTAINER_NAME, blobDetail!.Id, blobDetail.ContainerName);

                    if (deleteBlobDetailRc.Failed)
                    {
                        ErrorWorker.CopyErrors(deleteBlobDetailRc, rc);
                    }
                }

                return new OkResult();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }

            return StatusCode(500);
        }
        #endregion
    }
}
