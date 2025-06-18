using Microsoft.AspNetCore.Mvc;

namespace Blog.Controllers
{
    [ApiController]
    [Route("")]
    public class HomeController : ControllerBase
    {
        [HttpGet("")]
        public IActionResult Get(IConfiguration configuration)
        {
            var env = configuration.GetValue<string>("Env");
            return Ok(new
            {
                environment = env
            });
        }
    }
}
