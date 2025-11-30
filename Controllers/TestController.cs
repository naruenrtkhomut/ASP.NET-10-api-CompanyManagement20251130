using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/Test")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        [Route("")]
        public IActionResult Get()
        {
            //return Ok(new { data = Program.configuration });
            return Ok(new { });
        }
    }
}
