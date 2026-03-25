using EzDinner.Application.Commands.PushSubscriptions;
using EzDinner.Core.Aggregates.PushSubscriptionAggregate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using System;
using System.Threading.Tasks;

namespace EzDinner.Functions
{
    public class PushSubscriptionDelete
    {
        private readonly ILogger<PushSubscriptionDelete> _logger;
        private readonly IPushSubscriptionRepository _pushRepository;

        public PushSubscriptionDelete(ILogger<PushSubscriptionDelete> logger, IPushSubscriptionRepository pushRepository)
        {
            _logger = logger;
            _pushRepository = pushRepository;
        }

        [Function(nameof(PushSubscriptionDelete))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "push/subscriptions")] HttpRequest req)
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();

            var userId = req.HttpContext.User.GetNameIdentifierId();
            if (userId is null || !Guid.TryParse(userId, out var userGuid)) return new UnauthorizedResult();

            try
            {
                var command = new DeletePushSubscriptionCommand(_pushRepository);
                await command.Handle(userGuid);
                return new NoContentResult();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to delete push subscription for user {UserId}", userGuid);
                return new StatusCodeResult(500);
            }
        }
    }
}
