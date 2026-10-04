using EzDinner.Application.Commands.RatingReminders;
using System.Net;
using System.Text.Json;
using WebPush;
using Subscription = EzDinner.Core.Aggregates.PushSubscriptionAggregate.PushSubscription;

namespace EzDinner.Infrastructure.RatingReminders;

public sealed class WebPushRatingReminderTransport(WebPushClient client) : IRatingReminderTransport
{
    public async Task<RatingReminderTransportOutcome> SendAsync(Subscription subscription, RatingReminderPushPayload payload, int ttlSeconds, CancellationToken cancellationToken)
    {
        try
        {
            await client.SendNotificationAsync(new WebPush.PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth),
                JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                new Dictionary<string, object> { ["TTL"] = ttlSeconds }, cancellationToken);
            return RatingReminderTransportOutcome.Sent;
        }
        catch (WebPushException exception) when (exception.StatusCode is HttpStatusCode.Gone or HttpStatusCode.BadRequest)
        {
            return RatingReminderTransportOutcome.SubscriptionExpired;
        }
    }
}
