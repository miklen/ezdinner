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
        private readonly IDinnerService _dinnerService;
        private readonly IDinnerRepository _dinnerRepository;
        private readonly IWishlistRepository _wishlistRepository;
        private readonly IWishStatsRepository _wishStatsRepository;
        private readonly IPushSubscriptionRepository _pushRepository;
        private readonly WebPushClient _webPushClient;
        private readonly IAuthzService _authz;

        public DinnerAddMenuItem(
            ILogger<DinnerAddMenuItem> logger,
            IDinnerService dinnerService,
            IDinnerRepository dinnerRepository,
            IWishlistRepository wishlistRepository,
            IWishStatsRepository wishStatsRepository,
            IPushSubscriptionRepository pushRepository,
            WebPushClient webPushClient,
            IAuthzService authz)
        {
            _logger = logger;
            _dinnerService = dinnerService;
            _dinnerRepository = dinnerRepository;
            _wishlistRepository = wishlistRepository;
            _wishStatsRepository = wishStatsRepository;
            _pushRepository = pushRepository;
            _webPushClient = webPushClient;
            _authz = authz;
        }

        [Function(nameof(DinnerAddMenuItem))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "dinners/menuitem")] HttpRequest req
            )
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            var menuItem = await req.GetBodyAs<DinnerAddRemoveMenuItemCommandModel>();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, menuItem.FamilyId, Resources.Dinner, Actions.Update)) return new UnauthorizedResult();

            _logger.LogInformation($"Adding dish: {menuItem.DishId} to date: {menuItem.Date}");

            var command = new AddDishToDinnerCommand(
                _dinnerService,
                _dinnerRepository,
                _wishlistRepository,
                _wishStatsRepository,
                _pushRepository,
                _webPushClient,
                _logger);

            var plannerId = Guid.Parse(req.HttpContext.User.GetNameIdentifierId()!);
            await command.HandleAsync(menuItem.FamilyId, menuItem.Date, menuItem.DishId, plannerId);

            return new OkResult();
        }
    }
}
