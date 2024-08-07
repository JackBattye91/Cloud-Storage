using Microsoft.AspNetCore.Mvc;
using JB.Blob;
using JB.Common;

namespace CloudStorage.API.V2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BlobController : ControllerBase
    {
        private readonly ILogger<BlobController> logger;
        private readonly IWrapper _blobService;

        public BlobController(ILogger<BlobController> logger, IWrapper blobService)
        {
            this.logger = logger;
            _blobService = blobService;
        }

        [HttpGet]
        [Route("details")]
        public IActionResult GetBlobDetails()
        {
            IReturnCode rc = new ReturnCode();

            try
            {
                if (rc.Success)
                {

                }

                throw new Exception();
            }
            catch (Exception ex)
            {
                rc.AddError(new Error(ex));
                return StatusCode(500, rc);
            }
        }
    }
}
