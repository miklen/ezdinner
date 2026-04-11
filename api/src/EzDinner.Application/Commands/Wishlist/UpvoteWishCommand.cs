using EzDinner.Core.Aggregates.PushSubscriptionAggregate;
using EzDinner.Core.Aggregates.WishlistAggregate;
using Microsoft.Extensions.Logging;
using NodaTime;
using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using WebPush;
using LibraryPushSubscription = WebPush.PushSubscription;

namespace EzDinner.Application.Commands.Wishlist
{
    public enum UpvoteWishResult
    {
        Upvoted,
        NotFound,
        AlreadyVoted,
    }

    public class UpvoteWishCommand
    {
        private readonly IWishlistRepository _wishlistRepo;
        private readonly IPushSubscriptionRepository _pushRepo;
        private readonly WebPushClient _webPushClient;
        private readonly IClock _clock;
        private readonly ILogger _logger;

        public UpvoteWishCommand(
            IWishlistRepository wishlistRepo,
            IPushSubscriptionRepository pushRepo,
            WebPushClient webPushClient,
            IClock clock,
            ILogger logger)
        {
            _wishlistRepo = wishlistRepo;
            _pushRepo = pushRepo;
            _webPushClient = webPushClient;
            _clock = clock;
            _logger = logger;
        }

        public async Task<UpvoteWishResult> HandleAsync(Guid familyId, Guid wishId, Guid userId)
        {
            var wishes = await _wishlistRepo.GetActiveAsync(familyId);
            var wish = wishes.FirstOrDefault(w => w.Id == wishId);
            if (wish is null) return UpvoteWishResult.NotFound;

            try
            {
                wish.Upvote(userId, _clock);
            }
            catch (InvalidOperationException ex) when (ex.Message == "ALREADY_VOTED")
            {
                return UpvoteWishResult.AlreadyVoted;
            }

            await _wishlistRepo.UpdateAsync(wish);

            // Notify original requester if upvoter is not the requester
            if (userId != wish.AddedBy)
            {
                await TrySendWishUpvotedNotificationAsync(wish, userId);
            }

            return UpvoteWishResult.Upvoted;
        }

        private async Task TrySendWishUpvotedNotificationAsync(WishlistItem wish, Guid voterUserId)
        {
            try
            {
                var subscription = await _pushRepo.GetByUserIdAsync(wish.AddedBy);
                if (subscription is null) return;

                var payload = JsonSerializer.Serialize(new
                {
                    title = "EzDinner",
                    type = "wish_upvoted",
                    dishName = wish.DishName,
                    lang = subscription.Language,
                });
                await TrySendAsync(subscription, payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send WishUpvoted notification for wish {WishId}", wish.Id);
            }
        }

        private async Task TrySendAsync(EzDinner.Core.Aggregates.PushSubscriptionAggregate.PushSubscription subscription, string payload)
        {
            try
            {
                var librarySubscription = new LibraryPushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth);
                await _webPushClient.SendNotificationAsync(librarySubscription, payload);
            }
            catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone || ex.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.LogInformation("Stale push subscription {SubscriptionId} (HTTP {Status})", subscription.Id, ex.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Push notification failed for subscription {SubscriptionId}", subscription.Id);
            }
        }
    }
}
