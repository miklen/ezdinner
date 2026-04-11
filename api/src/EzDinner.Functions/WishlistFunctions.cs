using EzDinner.Application.Commands.Wishlist;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.FamilyAggregate;
using EzDinner.Core.Aggregates.PushSubscriptionAggregate;
using EzDinner.Core.Aggregates.WishlistAggregate;
using EzDinner.Functions.Models.Command;
using EzDinner.Query.Core.FamilyQueries;
using EzDinner.Query.Core.WishlistQueries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using NodaTime;
using System;
using System.Threading.Tasks;
using WebPush;

namespace EzDinner.Functions
{
    public class WishlistFunctions
    {
        private readonly ILogger<WishlistFunctions> _logger;
        private readonly IWishlistRepository _wishlistRepo;
        private readonly IWishStatsRepository _wishStatsRepo;
        private readonly IWishlistQueryRepository _wishlistQueryRepo;
        private readonly IFamilyRepository _familyRepo;
        private readonly IFamilyQueryService _familyQueryService;
        private readonly IPushSubscriptionRepository _pushRepo;
        private readonly WebPushClient _webPushClient;
        private readonly IAuthzService _authz;

        public WishlistFunctions(
            ILogger<WishlistFunctions> logger,
            IWishlistRepository wishlistRepo,
            IWishStatsRepository wishStatsRepo,
            IWishlistQueryRepository wishlistQueryRepo,
            IFamilyRepository familyRepo,
            IFamilyQueryService familyQueryService,
            IPushSubscriptionRepository pushRepo,
            WebPushClient webPushClient,
            IAuthzService authz)
        {
            _logger = logger;
            _wishlistRepo = wishlistRepo;
            _wishStatsRepo = wishStatsRepo;
            _wishlistQueryRepo = wishlistQueryRepo;
            _familyRepo = familyRepo;
            _familyQueryService = familyQueryService;
            _pushRepo = pushRepo;
            _webPushClient = webPushClient;
            _authz = authz;
        }

        [Function("WishlistGet")]
        public async Task<IActionResult> GetWishlist(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "families/{familyId}/wishlist")] HttpRequest req,
            string familyId)
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            if (!Guid.TryParse(familyId, out var parsedFamilyId)) return new BadRequestObjectResult("Invalid family ID.");
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, familyId, Resources.Wishlist, Actions.Read)) return new UnauthorizedResult();

            var userId = Guid.Parse(req.HttpContext.User.GetNameIdentifierId()!);
            var query = new GetWishlistQuery(_wishlistQueryRepo, _familyQueryService);
            var result = await query.ExecuteAsync(parsedFamilyId, userId);
            return new OkObjectResult(result);
        }

        [Function("WishlistAdd")]
        public async Task<IActionResult> AddWish(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "families/{familyId}/wishlist")] HttpRequest req,
            string familyId)
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            if (!Guid.TryParse(familyId, out var parsedFamilyId)) return new BadRequestObjectResult("Invalid family ID.");
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, familyId, Resources.Wishlist, Actions.Create)) return new UnauthorizedResult();

            var userId = Guid.Parse(req.HttpContext.User.GetNameIdentifierId()!);
            var model = await req.GetBodyAs<AddWishCommandModel>();
            if (model.DishId == Guid.Empty) return new BadRequestObjectResult("MISSING_DISH_ID");
            if (string.IsNullOrWhiteSpace(model.DishName)) return new BadRequestObjectResult("MISSING_DISH_NAME");

            var command = new AddWishCommand(_wishlistRepo, _wishStatsRepo, SystemClock.Instance);
            var result = await command.HandleAsync(parsedFamilyId, model.DishId, model.DishName, userId);

            if (result.AlreadyExists)
                return new ConflictObjectResult(new { wishId = result.WishId });

            return new CreatedResult($"/api/families/{familyId}/wishlist", new { wishId = result.WishId });
        }

        [Function("WishlistUpvote")]
        public async Task<IActionResult> UpvoteWish(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "families/{familyId}/wishlist/{wishId}/upvote")] HttpRequest req,
            string familyId,
            string wishId)
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            if (!Guid.TryParse(familyId, out var parsedFamilyId)) return new BadRequestObjectResult("Invalid family ID.");
            if (!Guid.TryParse(wishId, out var parsedWishId)) return new BadRequestObjectResult("Invalid wish ID.");
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, familyId, Resources.Wishlist, Actions.Update)) return new UnauthorizedResult();

            var userId = Guid.Parse(req.HttpContext.User.GetNameIdentifierId()!);
            var command = new UpvoteWishCommand(_wishlistRepo, _pushRepo, _webPushClient, SystemClock.Instance, _logger);
            var result = await command.HandleAsync(parsedFamilyId, parsedWishId, userId);

            return result switch
            {
                UpvoteWishResult.Upvoted => new OkResult(),
                UpvoteWishResult.AlreadyVoted => new ConflictObjectResult("ALREADY_VOTED"),
                UpvoteWishResult.NotFound => new NotFoundResult(),
                _ => new StatusCodeResult(500),
            };
        }

        [Function("WishlistRemove")]
        public async Task<IActionResult> RemoveWish(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "families/{familyId}/wishlist/{wishId}")] HttpRequest req,
            string familyId,
            string wishId)
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            if (!Guid.TryParse(familyId, out var parsedFamilyId)) return new BadRequestObjectResult("Invalid family ID.");
            if (!Guid.TryParse(wishId, out var parsedWishId)) return new BadRequestObjectResult("Invalid wish ID.");
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, familyId, Resources.Wishlist, Actions.Delete)) return new UnauthorizedResult();

            var userId = Guid.Parse(req.HttpContext.User.GetNameIdentifierId()!);
            var command = new RemoveWishCommand(_wishlistRepo, _familyRepo);
            var result = await command.HandleAsync(parsedFamilyId, parsedWishId, userId);

            return result switch
            {
                RemoveWishResult.Removed => new NoContentResult(),
                RemoveWishResult.Forbidden => new ObjectResult(null) { StatusCode = 403 },
                RemoveWishResult.NotFound => new NotFoundResult(),
                _ => new StatusCodeResult(500),
            };
        }
    }
}
