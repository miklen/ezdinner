using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.PushSubscriptionAggregate;
using Microsoft.Extensions.Logging;
using NodaTime;
using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using WebPush;
using DomainPushSubscription = EzDinner.Core.Aggregates.PushSubscriptionAggregate.PushSubscription;
using LibraryPushSubscription = WebPush.PushSubscription;

namespace EzDinner.Application.Commands.PushSubscriptions
{
    public class SendTonightNotificationsCommand
    {
        private readonly IPushSubscriptionRepository _pushRepository;
        private readonly IDinnerRepository _dinnerRepository;
        private readonly IDishRepository _dishRepository;
        private readonly WebPushClient _webPushClient;
        private readonly ILogger _logger;

        public SendTonightNotificationsCommand(
            IPushSubscriptionRepository pushRepository,
            IDinnerRepository dinnerRepository,
            IDishRepository dishRepository,
            WebPushClient webPushClient,
            ILogger logger)
        {
            _pushRepository = pushRepository;
            _dinnerRepository = dinnerRepository;
            _dishRepository = dishRepository;
            _webPushClient = webPushClient;
            _logger = logger;
        }

        public async Task Handle()
        {
            var tonight = TonightInCopenhagen();
            var familyIds = await _pushRepository.GetAllFamilyIdsAsync();

            foreach (var familyId in familyIds)
            {
                await SendNotificationsForFamily(familyId, tonight);
            }
        }

        private async Task SendNotificationsForFamily(Guid familyId, LocalDate tonight)
        {
            var subscriptions = (await _pushRepository.GetByFamilyIdAsync(familyId)).ToList();
            if (!subscriptions.Any()) return;

            var dinner = await _dinnerRepository.GetAsync(familyId, tonight);
            if (dinner is null || !dinner.IsPlanned) return;

            var dishes = await _dishRepository.GetDishesAsync(familyId);
            var dishMap = dishes.ToDictionary(d => d.Id, d => d.Name);
            var dishNames = dinner.Menu
                .Select(m => dishMap.TryGetValue(m.DishId, out var name) ? name : null)
                .Where(n => n is not null)
                .ToList();

            if (!dishNames.Any()) return;

            foreach (var subscription in subscriptions)
            {
                var payload = JsonSerializer.Serialize(new { title = "EzDinner", dishes = dishNames, lang = subscription.Language });
                await TrySendNotification(subscription, payload);
            }
        }

        private async Task TrySendNotification(DomainPushSubscription subscription, string payload)
        {
            try
            {
                var librarySubscription = new LibraryPushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth);
                await _webPushClient.SendNotificationAsync(librarySubscription, payload);
            }
            catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone || ex.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.LogInformation("Removing stale push subscription {SubscriptionId} (HTTP {Status})", subscription.Id, ex.StatusCode);
                await _pushRepository.DeleteAsync(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send push notification to subscription {SubscriptionId}", subscription.Id);
            }
        }

        private static LocalDate TonightInCopenhagen()
        {
            var tz = DateTimeZoneProviders.Tzdb["Europe/Copenhagen"];
            return SystemClock.Instance.GetCurrentInstant().InZone(tz).Date;
        }
    }
}
