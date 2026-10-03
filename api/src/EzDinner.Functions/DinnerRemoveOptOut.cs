using System.Threading.Tasks;
using EzDinner.Application.Commands.Dinners;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Functions.Models.Command;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace EzDinner.Functions
{
    public class DinnerRemoveOptOut
    {
        private readonly ILogger<DinnerRemoveOptOut> _logger;
        private readonly ChangeDinnerCommand _dinnerChanges;
        private readonly IAuthzService _authz;

        public DinnerRemoveOptOut(ILogger<DinnerRemoveOptOut> logger, ChangeDinnerCommand dinnerChanges, IAuthzService authz)
        {
            _logger = logger;
            _dinnerChanges = dinnerChanges;
            _authz = authz;
        }

        [Function(nameof(DinnerRemoveOptOut))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "dinners/optout/remove")] HttpRequest req
            )
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            var model = await req.GetBodyAs<DinnerOptOutCommandModel>();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, model.FamilyId, Resources.Dinner, Actions.Update)) return new UnauthorizedResult();

            _logger.LogInformation($"Removing opt-out for date: {model.Date}");
            await _dinnerChanges.RemoveOptOutAsync(model.FamilyId, model.Date);

            return new OkResult();
        }
    }
}
