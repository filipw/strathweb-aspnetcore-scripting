using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.Scripting
{
    [Route("api/[controller]")]
    public class TestController : Controller
    {
        [HttpGet]
        public string Get()
        {
            return "hello!";
        }
    }
}