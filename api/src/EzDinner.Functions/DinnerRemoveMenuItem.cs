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
    public class DinnerRemoveMenuItem
    {
        private readonly ILogger<DinnerAddMenuItem> _logger;
        private readonly IDinnerService _dinnerService;
        private readonly IDinnerRepository _dinnerRepository;
        private readonly IAuthzService _authz;

        public DinnerRemoveMenuItem(ILogger<DinnerAddMenuItem> logger, IDinnerService dinnerService, IDinnerRepository dinnerRepository, IAuthzService authz)
        {
            _logger = logger;
            _dinnerService = dinnerService;
            _dinnerRepository = dinnerRepository;
            _authz = authz;
        }
        
        [Function(nameof(DinnerRemoveMenuItem))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "dinners/menuitem/remove")] HttpRequest req
            )
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            var menuItem = await req.GetBodyAs<DinnerAddRemoveMenuItemCommandModel>();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, menuItem.FamilyId, Resources.Dinner, Actions.Update)) return new UnauthorizedResult();

            _logger.LogInformation($"Adding dish: {menuItem.DishId} to date: {menuItem.Date}");

            var dinner = await _dinnerService.GetAsync(menuItem.FamilyId, menuItem.Date);
            dinner.RemoveMenuItem(new MenuItem(menuItem.DishId));
            await _dinnerRepository.SaveAsync(dinner);

            return new OkResult();
        }
    }
}

