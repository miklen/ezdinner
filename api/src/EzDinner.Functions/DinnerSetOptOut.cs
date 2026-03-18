using System.Threading.Tasks;
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
    public class DinnerSetOptOut
    {
        private readonly ILogger<DinnerSetOptOut> _logger;
        private readonly IDinnerService _dinnerService;
        private readonly IDinnerRepository _dinnerRepository;
        private readonly IAuthzService _authz;

        public DinnerSetOptOut(ILogger<DinnerSetOptOut> logger, IDinnerService dinnerService, IDinnerRepository dinnerRepository, IAuthzService authz)
        {
            _logger = logger;
            _dinnerService = dinnerService;
            _dinnerRepository = dinnerRepository;
            _authz = authz;
        }

        [Function(nameof(DinnerSetOptOut))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "dinners/optout")] HttpRequest req
            )
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            var model = await req.GetBodyAs<DinnerOptOutCommandModel>();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, model.FamilyId, Resources.Dinner, Actions.Update)) return new UnauthorizedResult();

            _logger.LogInformation($"Setting opt-out for date: {model.Date}, reason: {model.Reason}");
            var dinner = await _dinnerService.GetAsync(model.FamilyId, model.Date);
            dinner.SetOptOut(model.Reason);
            await _dinnerRepository.SaveAsync(dinner);

            return new OkResult();
        }
    }
}
