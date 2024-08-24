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

namespace CloudStorage.API.V2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Password")]
    public class BlobController : ControllerBase
    {
        private readonly ILogger<BlobController> _logger;
        private readonly JB.Blob.IWrapper _blobService;
        private readonly JB.NoSqlDatabase.IWrapper _noSqlDatabase;
        private readonly UserService _userService;
        private readonly AppSettings _appSettings;

        public BlobController(ILogger<BlobController> logger, 
            JB.Blob.IWrapper blobService, 
            JB.NoSqlDatabase.IWrapper noSqlDatabase, 
            UserService userService,
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
                string? authHeader = Request.Headers.Authorization.FirstOrDefault();

                if (authHeader == null) {
                    return Unauthorized();
                }
                
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                JwtSecurityToken token = tokenHandler.ReadJwtToken(authHeader);

                Claim? subjectClaim = token.Claims.Where(x => x.Subject != null).FirstOrDefault();
                string? userId = subjectClaim?.Value;

                if (userId == null) {
                    return Unauthorized();
                }

                User user = await _userService.GetByIdAsync(userId);

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
                string? authHeader = Request.Headers.Authorization.FirstOrDefault();

                if (authHeader == null) {
                    return Unauthorized();
                }
                
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                JwtSecurityToken token = tokenHandler.ReadJwtToken(authHeader);

                Claim? subjectClaim = token.Claims.Where(x => x.Subject != null).FirstOrDefault();
                string? userId = subjectClaim?.Value;

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

                IReturnCode<Stream> dataStreamRc = await _blobService.GetBlobAsStreamAsync(blobDetail.ContainerName, blobDetail.BlobName);
                if (dataStreamRc.Failed) {

                    throw new Exception("Unable to get blob stream");
                }

                Stream dataStream = dataStreamRc.Data!;
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
                string? authHeader = Request.Headers.Authorization.FirstOrDefault();

                if (authHeader == null) {
                    return Unauthorized();
                }
                
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                JwtSecurityToken token = tokenHandler.ReadJwtToken(authHeader);

                Claim? subjectClaim = token.Claims.Where(x => x.Subject != null).FirstOrDefault();
                string? userId = subjectClaim?.Value;

                if (userId == null) {
                    return Unauthorized();
                }

                User user = await _userService.GetByIdAsync(userId);

                BlobDetail blobDetail = new BlobDetail() {
                    Id = Guid.NewGuid().ToString(),
                    ContainerName = user.Id,
                    BlobName = Guid.NewGuid().ToString()
                };
                using (StreamReader reader = new StreamReader(dataStream)) {
                    int detailsNameSize = reader.Read();

                    if (detailsNameSize > 0) {
                        char[] buffer = new char[detailsNameSize];
                        await reader.ReadBlockAsync(buffer, CancellationToken.None);
                        blobDetail.Name = new string(buffer);
                    }
                    
                    int detailDescriptionSize = reader.Read();

                    if (detailDescriptionSize > 0) {
                        char[] buffer = new char[detailDescriptionSize];
                        await reader.ReadBlockAsync(buffer, CancellationToken.None);
                        blobDetail.Description = new string(buffer);
                    }
                    
                    int detailExtensionSize = reader.Read();

                    if (detailExtensionSize > 0) {
                        char[] buffer = new char[detailExtensionSize];
                        await reader.ReadBlockAsync(buffer, CancellationToken.None);
                        blobDetail.Extension = new string(buffer);
                    }
                }

                IReturnCode<Models.BlobDetail> getBlobDetailsRc = await _noSqlDatabase.AddItem<Models.BlobDetail>(_appSettings.Database.Database, Consts.Database.BlobDetailContainer, blobDetail);
                
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

                IReturnCode<string> dataStreamRc = await _blobService.UploadBlobAsync(dataStream, blobDetail.ContainerName, blobDetail.BlobName);
                if (dataStreamRc.Failed) {

                    throw new Exception("Unable to upload blob stream");
                }

                return Ok();
           }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get Blob Details Failed");
                return StatusCode(500);
            }
        }
    }
}
