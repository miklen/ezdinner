using System.Threading.Tasks;
using EzDinner.Application.Commands.Dinners;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.PushSubscriptionAggregate;
using EzDinner.Core.Aggregates.WishlistAggregate;
using EzDinner.Functions.Models.Command;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using NodaTime;
using System;
using WebPush;

namespace EzDinner.Functions
{
    public class DinnerAddMenuItem
    {
        private readonly ILogger<DinnerAddMenuItem> _logger;
        private readonly AddDishToDinnerCommand _addDish;
        private readonly IAuthzService _authz;
        private readonly ConditionalDinnerMenuChangeHttp? _conditionalChanges;

        public DinnerAddMenuItem(
            ILogger<DinnerAddMenuItem> logger,
            AddDishToDinnerCommand addDish,
            IAuthzService authz,
            ConditionalDinnerMenuChangeHttp? conditionalChanges = null)
        {
            _logger = logger;
            _addDish = addDish;
            _authz = authz;
            _conditionalChanges = conditionalChanges;
        }

        [Function(nameof(DinnerAddMenuItem))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "dinners/menuitem")] HttpRequest req
            )
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            if (ConditionalDinnerMenuChangeHttp.IsRequested(req))
                return await (_conditionalChanges ?? throw new InvalidOperationException("CONDITIONAL_MENU_CHANGE_NOT_CONFIGURED")).RunAsync(req, true);
            var menuItem = await req.GetBodyAs<DinnerAddRemoveMenuItemCommandModel>();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, menuItem.FamilyId, Resources.Dinner, Actions.Update)) return new UnauthorizedResult();

            _logger.LogInformation($"Adding dish: {menuItem.DishId} to date: {menuItem.Date}");

            var plannerId = Guid.Parse(req.HttpContext.User.GetNameIdentifierId()!);
            await _addDish.HandleAsync(menuItem.FamilyId, menuItem.Date, menuItem.DishId, plannerId);

            return new OkResult();
        }
    }
}
