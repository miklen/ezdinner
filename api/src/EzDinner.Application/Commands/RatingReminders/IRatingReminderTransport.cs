using EzDinner.Core.Aggregates.PushSubscriptionAggregate;

namespace EzDinner.Application.Commands.RatingReminders;

public enum RatingReminderTransportOutcome { Sent, SubscriptionExpired }
public sealed record RatingReminderPushPayload(string Type, string DishName, string DinnerDate, string Lang, string Destination);

public interface IRatingReminderTransport
{
    Task<RatingReminderTransportOutcome> SendAsync(PushSubscription subscription, RatingReminderPushPayload payload, int ttlSeconds, CancellationToken cancellationToken);
}
