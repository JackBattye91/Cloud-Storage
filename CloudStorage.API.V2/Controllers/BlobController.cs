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
using System.Net.Http.Headers;
using JB.NoSqlDatabase;
using CloudStorage.API.V2.Security;
using System.Text.Json;
using System.Text;

namespace CloudStorage.API.V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class BlobController : CloudControllerBase
    {
        private readonly ILogger<BlobController> _logger;
        private readonly IBlobService _blobService;
        private readonly IBlobDetailService _blobDetailService;
        private readonly INoSqlDatabaseService _noSqlDatabase;
        private readonly IUserService _userService;
        private readonly AppSettings _appSettings;

        public BlobController(ILogger<BlobController> logger,
            IBlobService blobService,
            IBlobDetailService blobDetailService,
            INoSqlDatabaseService noSqlDatabase, 
            IUserService userService,
            IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _blobService = blobService;
            _userService = userService;
            _noSqlDatabase = noSqlDatabase;
            _appSettings = appSettings.Value;
            _blobDetailService = blobDetailService;
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

                IList<BlobDetail> blobDetailsList = await _blobDetailService.GetByUser(userId);

                List<Models.DTOs.BlobDetailDTO> blobDetails = new List<Models.DTOs.BlobDetailDTO>();
                foreach(BlobDetail detail in blobDetailsList) {
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

                BlobDetail blobDetail = await _blobDetailService.Get(id);
                Stream dataStream = await _blobService.GetBlobStreamAsync(blobDetail.ContainerName, blobDetail.BlobName);
                Response.ContentType = blobDetail.MimeType;
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

                BlobDetail blobDetail = await _blobDetailService.Get(id);
                Stream dataStream = await _blobService.GetBlobStreamAsync(Consts.Blob.THUMBNAIL_CONTAINER, blobDetail.ThumbnailName);
                Response.ContentType = blobDetail.MimeType;
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
        public async Task<IActionResult> CreateBlobStream()
        {
            try
            {
                string? userId = "cfb983ec-27ce-4c1a-9391-fcd3382c4417";// TokenUtilities.GetSubjectId(Request); 

                if (userId == null) {
                    return Unauthorized();
                }

                Stream contentStream = Request.Body;
                BlobDetail? blobDetail = await GetBlobDetails(contentStream);
                string fileExtension = "png";

                if (blobDetail == null)
                {
                    throw new Exception("Unable to get blob details from stream");
                }

                blobDetail.Id = Guid.NewGuid().ToString();
                blobDetail.ContainerName = "images";
                blobDetail.BlobName = string.Format("{0}.{1}", Guid.NewGuid(), fileExtension);
                blobDetail.ThumbnailName = string.Format("{0}.{1}", Guid.NewGuid(), fileExtension);

                blobDetail = await _blobDetailService.Create(blobDetail);

                Stream? fileStream = await GetFileStream(contentStream);

                if (fileStream == null)
                {
                    throw new Exception("Unable to get file stream");
                }

                fileStream.Seek(0, SeekOrigin.Begin);
                await _blobService.UploadBlobAsync(blobDetail.ContainerName, blobDetail.BlobName, fileStream);

                fileStream.Seek(0, SeekOrigin.Begin);
                await _blobService.UploadBlobAsync(Consts.Blob.THUMBNAIL_CONTAINER, blobDetail.ThumbnailName, fileStream);
                
                return Ok(blobDetail);
           }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get Blob Details Failed");
                return StatusCode(500);
            }
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> DeleteBlob(string id)
        {
            try
            {
                await _blobDetailService.Delete(id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get Blob Details Failed");
                return StatusCode(500);
            }
        }

        protected async Task<BlobDetail?> GetBlobDetails(Stream dataStream)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = await dataStream.ReadAsync(buffer, 0, 4);

            if (bytesRead > 0)
            {
                int detailsSize = BitConverter.ToInt32(buffer);

                if (detailsSize < 1024)
                {
                    await dataStream.ReadAsync(buffer, 0, detailsSize);
                    string details = Encoding.UTF8.GetString(buffer, 0, detailsSize);
                    BlobDetail? blobDetail = JsonSerializer.Deserialize<BlobDetail>(details);
                    return blobDetail;
                }
            }

            return null;
        }

        protected async Task<Stream?> GetFileStream(Stream dataStream)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = 0;
            Stream imageStream = new MemoryStream();
            do
            {
                bytesRead = await dataStream.ReadAsync(buffer, 0, 1024);
                imageStream.Write(buffer, 0, bytesRead);
            } while (bytesRead > 0);

            imageStream.Seek(0, SeekOrigin.Begin);
            return imageStream;
        }
    }
}
