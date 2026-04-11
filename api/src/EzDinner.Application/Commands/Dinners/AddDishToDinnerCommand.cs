using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.PushSubscriptionAggregate;
using EzDinner.Core.Aggregates.WishlistAggregate;
using Microsoft.Extensions.Logging;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using WebPush;
using LibraryPushSubscription = WebPush.PushSubscription;

namespace EzDinner.Application.Commands.Dinners
{
    public class AddDishToDinnerCommand
    {
        private readonly IDinnerService _dinnerService;
        private readonly IDinnerRepository _dinnerRepository;
        private readonly IWishlistRepository _wishlistRepo;
        private readonly IWishStatsRepository _wishStatsRepo;
        private readonly IPushSubscriptionRepository _pushRepo;
        private readonly WebPushClient _webPushClient;
        private readonly ILogger _logger;

        public AddDishToDinnerCommand(
            IDinnerService dinnerService,
            IDinnerRepository dinnerRepository,
            IWishlistRepository wishlistRepo,
            IWishStatsRepository wishStatsRepo,
            IPushSubscriptionRepository pushRepo,
            WebPushClient webPushClient,
            ILogger logger)
        {
            _dinnerService = dinnerService;
            _dinnerRepository = dinnerRepository;
            _wishlistRepo = wishlistRepo;
            _wishStatsRepo = wishStatsRepo;
            _pushRepo = pushRepo;
            _webPushClient = webPushClient;
            _logger = logger;
        }

        public async Task HandleAsync(Guid familyId, LocalDate date, Guid dishId, Guid plannerId)
        {
            var dinner = await _dinnerService.GetAsync(familyId, date);
            dinner.AddMenuItem(new MenuItem(dishId));
            await _dinnerRepository.SaveAsync(dinner);

            // Best-effort wish grant — does not fail the dinner assignment if this throws
            try
            {
                await TryGrantWishAsync(familyId, dishId, date, plannerId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Wish grant side-effect failed for dish {DishId} in family {FamilyId}", dishId, familyId);
            }
        }

        private async Task TryGrantWishAsync(Guid familyId, Guid dishId, LocalDate date, Guid plannerId)
        {
            var wish = await _wishlistRepo.GetByDishAsync(familyId, dishId);
            if (wish is null) return;

            var now = SystemClock.Instance.GetCurrentInstant();
            if (wish.IsExpired(now)) return;

            await _wishlistRepo.DeleteAsync(wish);
            await _wishStatsRepo.IncrementWishesGrantedAsync(familyId, wish.AddedBy);
            await SendWishGrantedNotificationsAsync(wish, date, plannerId);
        }

        private async Task SendWishGrantedNotificationsAsync(WishlistItem wish, LocalDate date, Guid plannerId)
        {
            // Deduplicate recipients: requester + all voters, excluding the planner (they know — they did it)
            var recipientIds = new HashSet<Guid>(wish.Votes.Select(v => v.UserId));
            recipientIds.Add(wish.AddedBy);
            recipientIds.Remove(plannerId);

            foreach (var userId in recipientIds)
            {
                try
                {
                    var subscription = await _pushRepo.GetByUserIdAsync(userId);
                    if (subscription is null) continue;

                    // Each recipient gets their own payload so lang matches their stored preference
                    var payload = JsonSerializer.Serialize(new
                    {
                        title = "EzDinner",
                        type = "wish_granted",
                        dishName = wish.DishName,
                        date = date.ToString("yyyy-MM-dd", null),
                        lang = subscription.Language,
                    });

                    var librarySubscription = new LibraryPushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth);
                    await _webPushClient.SendNotificationAsync(librarySubscription, payload);
                }
                catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone || ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    _logger.LogInformation("Stale push subscription for user {UserId} during wish grant (HTTP {Status})", userId, ex.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send WishGranted notification to user {UserId}", userId);
                }
            }
        }
    }
}

