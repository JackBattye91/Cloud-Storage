using Microsoft.AspNetCore.Mvc;
using JB.Blob;
using JB.Common;
using Microsoft.IdentityModel.JsonWebTokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using CloudStorage.API.V2.Services;
using CloudStorage.API.V2.Models;
using Microsoft.Extensions.Options;
using CloudStorage.API.V2.Models.DTOs;
using System.Data;
using JB.NoSqlDatabase;
using CloudStorage.API.V2.Security;

namespace CloudStorage.API.V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class BlobController : CloudControllerBase
    {
        private readonly ILogger<BlobController> _logger;
        private readonly IBlobService _blobService;
        private readonly INoSqlDatabaseService _noSqlDatabase;
        private readonly IUserService _userService;
        private readonly AppSettings _appSettings;

        public BlobController(ILogger<BlobController> logger,
            IBlobService blobService,
            INoSqlDatabaseService noSqlDatabase, 
            IUserService userService,
            IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _blobService = blobService;
            _userService = userService;
            _noSqlDatabase = noSqlDatabase;
            _appSettings = appSettings.Value;
        }

        [HttpGet]
        [Route("details")]
        public async Task<IActionResult> GetBlobDetails()
        {
            try
            {
                string? userId = TokenUtilities.GetSubjectId(Request);

                if (userId == null) {
                    return Unauthorized();
                }

                string query = $"SELECT * FROM c WHERE c.UserId = '{userId}'";
                IReturnCode<IList<Models.BlobDetail>> getBlobDetailsRc = await _noSqlDatabase.GetItems<Models.BlobDetail>(_appSettings.Database.Database, Consts.Database.BlobDetailContainer, query);
                
                if (getBlobDetailsRc.Failed) {
                    Exception? exception = getBlobDetailsRc.Errors.FirstOrDefault()?.Exception;

                    if (exception == null) {
                        throw new Exception("Unable to get Blob details");
                    }
                    else {
                        throw exception;
                    }
                }

                List<Models.DTOs.BlobDetailDTO> blobDetails = new List<Models.DTOs.BlobDetailDTO>();
                foreach(BlobDetail detail in getBlobDetailsRc.Data!) {
                    BlobDetailDTO? detail1Dto = Converter.Convert<Models.BlobDetail, Models.DTOs.BlobDetailDTO>(detail);
                    
                    if (detail1Dto != null){
                        blobDetails.Add(detail1Dto);
                    }
                }

                return Ok(blobDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get Blob Details Failed");
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("stream/{id}")]
        public async Task<IActionResult> GetBlobStream([FromRoute] string id)
        {
            try
            {
                string? userId = TokenUtilities.GetSubjectId(Request);

                if (userId == null) {
                    return Unauthorized();
                }

                User user = await _userService.GetByIdAsync(userId);

                string query = $"SELECT * FROM c WHERE c.id = '{id}' AND c.UserId = '{userId}'";
                IReturnCode<IList<Models.BlobDetail>> getBlobDetailsRc = await _noSqlDatabase.GetItems<Models.BlobDetail>(_appSettings.Database.Database, Consts.Database.BlobDetailContainer, query);
                
                if (getBlobDetailsRc.Failed) {
                    Exception? exception = getBlobDetailsRc.Errors.FirstOrDefault()?.Exception;

                    if (exception == null) {
                        throw new Exception("Unable to get Blob details");
                    }
                    else {
                        throw exception;
                    }
                }
                
                BlobDetail? blobDetail = getBlobDetailsRc.Data?.Count > 0 ? getBlobDetailsRc.Data[0] : null;
                
                if (blobDetail == null) {
                    throw new Exception("Blob not found");
                }

                Stream dataStream = await _blobService.GetBlobStreamAsync(blobDetail.ContainerName, blobDetail.BlobName);
                Response.ContentType = blobDetail.Extension;
                return Ok(dataStream);
           }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get Blob Details Failed");
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("thumbnail/{id}")]
        public async Task<IActionResult> GetBlobThumbnailStream([FromRoute] string id)
        {
            try
            {
                string? userId = TokenUtilities.GetSubjectId(Request);

                if (userId == null)
                {
                    return Unauthorized();
                }

                User user = await _userService.GetByIdAsync(userId);

                string query = $"SELECT * FROM c WHERE c.id = '{id}' AND c.UserId = '{userId}'";
                IReturnCode<IList<Models.BlobDetail>> getBlobDetailsRc = await _noSqlDatabase.GetItems<Models.BlobDetail>(_appSettings.Database.Database, Consts.Database.BlobDetailContainer, query);

                if (getBlobDetailsRc.Failed)
                {
                    Exception? exception = getBlobDetailsRc.Errors.FirstOrDefault()?.Exception;

                    if (exception == null)
                    {
                        throw new Exception("Unable to get Blob details");
                    }
                    else
                    {
                        throw exception;
                    }
                }

                BlobDetail? blobDetail = getBlobDetailsRc.Data?.Count > 0 ? getBlobDetailsRc.Data[0] : null;
                if (blobDetail == null)
                {
                    throw new Exception("Blob not found");
                }

                Stream dataStream = await _blobService.GetBlobStreamAsync(Consts.Blob.THUMBNAIL_CONTAINER, blobDetail.ThumbnailName);
                Response.ContentType = blobDetail.Extension;
                return Ok(dataStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get Blob Details Failed");
                return StatusCode(500);
            }
        }

        [HttpPost]
        [Route("stream")]
        public async Task<IActionResult> CreateBlobStream([FromBody]Stream dataStream)
        {
            try
            {
                string? userId = TokenUtilities.GetSubjectId(Request); 

                if (userId == null) {
                    return Unauthorized();
                }

                User user = await _userService.GetByIdAsync(userId);
                BlobDetail blobDetail = await ReadBlobDetail(dataStream);
                blobDetail.ContainerName = userId;

                IReturnCode<BlobDetail> getBlobDetailsRc = await _noSqlDatabase.AddItem(_appSettings.Database.Database, Consts.Database.BlobDetailContainer, blobDetail);
                
                if (getBlobDetailsRc.Failed) {
                    Exception? exception = getBlobDetailsRc.Errors.FirstOrDefault()?.Exception;

                    if (exception == null) {
                        throw new Exception("Unable to get Blob details");
                    }
                    else {
                        throw exception;
                    }
                }

                blobDetail = getBlobDetailsRc.Data!;
                await _blobService.UploadBlobAsync(blobDetail.ContainerName, blobDetail.BlobName, dataStream);

                dataStream.Position = 0;
                await _blobService.UploadBlobAsync(Consts.Blob.THUMBNAIL_CONTAINER, blobDetail.ThumbnailName, dataStream);

                return Ok();
           }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get Blob Details Failed");
                return StatusCode(500);
            }
        }

        protected async Task<BlobDetail> ReadBlobDetail(Stream stream)
        {
            BlobDetail blobDetail = new BlobDetail()
            {
                Id = Guid.NewGuid().ToString(),
                BlobName = Guid.NewGuid().ToString(),
                ThumbnailName = Guid.NewGuid().ToString(),
            };
            using (StreamReader reader = new StreamReader(stream))
            {
                int detailsNameSize = reader.Read();

                if (detailsNameSize > 0)
                {
                    char[] buffer = new char[detailsNameSize];
                    await reader.ReadBlockAsync(buffer, CancellationToken.None);
                    blobDetail.Name = new string(buffer);
                }

                int detailDescriptionSize = reader.Read();

                if (detailDescriptionSize > 0)
                {
                    char[] buffer = new char[detailDescriptionSize];
                    await reader.ReadBlockAsync(buffer, CancellationToken.None);
                    blobDetail.Description = new string(buffer);
                }

                int detailExtensionSize = reader.Read();

                if (detailExtensionSize > 0)
                {
                    char[] buffer = new char[detailExtensionSize];
                    await reader.ReadBlockAsync(buffer, CancellationToken.None);
                    blobDetail.Extension = new string(buffer);
                }
            }
            return blobDetail;
        }
    }
}
