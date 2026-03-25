using EzDinner.Application.Commands.PushSubscriptions;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.PushSubscriptionAggregate;
using EzDinner.Functions.Models.Command;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using System;
using System.Threading.Tasks;

namespace EzDinner.Functions
{
    public class PushSubscriptionSave
    {
        private readonly ILogger<PushSubscriptionSave> _logger;
        private readonly IPushSubscriptionRepository _pushRepository;
        private readonly IAuthzService _authz;

        public PushSubscriptionSave(ILogger<PushSubscriptionSave> logger, IPushSubscriptionRepository pushRepository, IAuthzService authz)
        {
            _logger = logger;
            _pushRepository = pushRepository;
            _authz = authz;
        }

        [Function(nameof(PushSubscriptionSave))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "push/subscriptions")] HttpRequest req)
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();

            var userId = req.HttpContext.User.GetNameIdentifierId();
            if (userId is null) return new UnauthorizedResult();

            try
            {
                var body = await req.GetBodyAs<SavePushSubscriptionCommandModel>();
                if (body is null) return new BadRequestResult();

                if (!Guid.TryParse(userId, out var userGuid)) return new BadRequestResult();
                if (!Guid.TryParse(body.FamilyId, out var familyId)) return new BadRequestObjectResult("INVALID_FAMILYID");

                if (!_authz.Authorize(userId, body.FamilyId, Resources.Family, Actions.Read))
                    return new UnauthorizedResult();

                var command = new SavePushSubscriptionCommand(_pushRepository);
                await command.Handle(userGuid, familyId, body.Endpoint, body.P256dh, body.Auth, body.Language);
                return new OkResult();
            }
            catch (ArgumentException e)
            {
                return new BadRequestObjectResult(e.Message);
            }
        }
    }
}
