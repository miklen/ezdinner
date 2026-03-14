using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace EzDinner.Functions
{
    public class TestGet
    {
        private readonly ILogger<TestGet> _logger;

        public TestGet(ILogger<TestGet> logger)
        {
            _logger = logger;
        }

        [Function("TestGet")]
        public IActionResult? Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "test")] HttpRequest req)
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();

            return new OkObjectResult("Test OK");
        }
    }
}
