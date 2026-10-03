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
    public class DinnerRemoveMenuItem
    {
        private readonly ILogger<DinnerAddMenuItem> _logger;
        private readonly ChangeDinnerCommand _dinnerChanges;
        private readonly IAuthzService _authz;
        private readonly ConditionalDinnerMenuChangeHttp? _conditionalChanges;

        public DinnerRemoveMenuItem(ILogger<DinnerAddMenuItem> logger, ChangeDinnerCommand dinnerChanges, IAuthzService authz, ConditionalDinnerMenuChangeHttp? conditionalChanges = null)
        {
            _logger = logger;
            _dinnerChanges = dinnerChanges;
            _authz = authz;
            _conditionalChanges = conditionalChanges;
        }
        
        [Function(nameof(DinnerRemoveMenuItem))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "dinners/menuitem/remove")] HttpRequest req
            )
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            if (ConditionalDinnerMenuChangeHttp.IsRequested(req))
                return await (_conditionalChanges ?? throw new System.InvalidOperationException("CONDITIONAL_MENU_CHANGE_NOT_CONFIGURED")).RunAsync(req, false);
            var menuItem = await req.GetBodyAs<DinnerAddRemoveMenuItemCommandModel>();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, menuItem.FamilyId, Resources.Dinner, Actions.Update)) return new UnauthorizedResult();

            _logger.LogInformation($"Adding dish: {menuItem.DishId} to date: {menuItem.Date}");

            await _dinnerChanges.RemoveAsync(menuItem.FamilyId, menuItem.Date, menuItem.DishId);

            return new OkResult();
        }
    }
}

