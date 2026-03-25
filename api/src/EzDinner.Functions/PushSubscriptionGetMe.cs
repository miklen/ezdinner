using EzDinner.Query.Core.PushSubscriptionQueries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using System;
using System.Threading.Tasks;

namespace EzDinner.Functions
{
    public class PushSubscriptionGetMe
    {
        private readonly ILogger<PushSubscriptionGetMe> _logger;
        private readonly IPushSubscriptionQueryRepository _queryRepository;

        public PushSubscriptionGetMe(ILogger<PushSubscriptionGetMe> logger, IPushSubscriptionQueryRepository queryRepository)
        {
            _logger = logger;
            _queryRepository = queryRepository;
        }

        [Function(nameof(PushSubscriptionGetMe))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "push/subscriptions/me")] HttpRequest req)
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();

            var userId = req.HttpContext.User.GetNameIdentifierId();
            if (userId is null || !Guid.TryParse(userId, out var userGuid)) return new UnauthorizedResult();

            var query = new GetPushSubscriptionQuery(_queryRepository);
            var isSubscribed = await query.HasActiveSubscriptionAsync(userGuid);
            return new OkObjectResult(new { isSubscribed });
        }
    }
}
