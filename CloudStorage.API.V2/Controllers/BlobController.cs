using Microsoft.AspNetCore.Mvc;

namespace CloudStorage.API.V2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BlobController : ControllerBase
    {
        [HttpGet]
        [Route("details")]
        public IActionResult GetBlobDetails()
        {
            return Ok();
        }
    }
}
