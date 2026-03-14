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
    public class DinnerReplaceMenuItem
    {
        private readonly ILogger<DinnerAddMenuItem> _logger;
        private readonly IDinnerService _dinnerService;
        private readonly IDinnerRepository _dinnerRepository;
        private readonly IAuthzService _authz;

        public DinnerReplaceMenuItem(ILogger<DinnerAddMenuItem> logger, IDinnerService dinnerService, IDinnerRepository dinnerRepository, IAuthzService authz)
        {
            _logger = logger;
            _dinnerService = dinnerService;
            _dinnerRepository = dinnerRepository;
            _authz = authz;
        }
        
        [Function(nameof(DinnerReplaceMenuItem))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "dinners/menuitem/replace")] HttpRequest req
            )
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            var replaceModel = await req.GetBodyAs<DinnerReplaceMenuItemCommandModel>();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, replaceModel.FamilyId, Resources.Dinner, Actions.Update)) return new UnauthorizedResult();

            _logger.LogInformation($"Replacing dishId: {replaceModel.DishId} with: {replaceModel.DishId}");

            await foreach (var dinner in _dinnerRepository.GetAsync(replaceModel.FamilyId, replaceModel.DishId)) {
                dinner.ReplaceMenuItem(new MenuItem(replaceModel.DishId), new MenuItem(replaceModel.NewDishId));
                await _dinnerRepository.SaveAsync(dinner);
            }
            
            return new OkResult();
        }
    }
}

