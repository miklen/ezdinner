using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;

namespace EzDinner.Functions
{
    public class PushVapidPublicKey
    {
        private readonly string _vapidPublicKey;

        public PushVapidPublicKey(IConfiguration configuration)
        {
            _vapidPublicKey = configuration.GetValue<string>("WebPush:VapidPublicKey")!;
        }

        [Function(nameof(PushVapidPublicKey))]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "push/vapid-public-key")] HttpRequest req)
        {
            return new OkObjectResult(_vapidPublicKey);
        }
    }
}
