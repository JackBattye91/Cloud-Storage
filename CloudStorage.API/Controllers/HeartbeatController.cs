using Microsoft.AspNetCore.Mvc;

namespace CloudStorage.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HeartbeatController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            Response.ContentType = "text/plain";
            return new OkObjectResult("Heartbeat");
        }
    }
}
