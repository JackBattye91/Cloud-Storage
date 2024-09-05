using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CloudStorage.API.V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HeartbeatController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("-------- I am alive --------");
        }
    }
}
