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
    public class DinnerConvertDishToOptOut
    {
        private readonly ILogger<DinnerConvertDishToOptOut> _logger;
        private readonly IDinnerRepository _dinnerRepository;
        private readonly IAuthzService _authz;

        public DinnerConvertDishToOptOut(ILogger<DinnerConvertDishToOptOut> logger, IDinnerRepository dinnerRepository, IAuthzService authz)
        {
            _logger = logger;
            _dinnerRepository = dinnerRepository;
            _authz = authz;
        }

        [Function(nameof(DinnerConvertDishToOptOut))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "dinners/menuitem/convert-to-optout")] HttpRequest req
            )
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            var model = await req.GetBodyAs<DinnerConvertToOptOutCommandModel>();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, model.FamilyId, Resources.Dinner, Actions.Update)) return new UnauthorizedResult();

            _logger.LogInformation($"Converting all dinners with dish {model.DishId} to opt-out with reason: {model.Reason}");

            var optOut = new OptOut(model.Reason);
            await foreach (var dinner in _dinnerRepository.GetAsync(model.FamilyId, model.DishId))
            {
                dinner.SetOptOut(optOut);
                await _dinnerRepository.SaveAsync(dinner);
            }

            return new OkResult();
        }
    }
}
