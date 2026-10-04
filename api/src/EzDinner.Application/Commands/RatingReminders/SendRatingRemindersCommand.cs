using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.PushSubscriptionAggregate;
using EzDinner.Core.Aggregates.RatingRemindersAggregate;
using EzDinner.Core.DomainServices.RatingReminders;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;
using ReminderState = EzDinner.Core.Aggregates.RatingRemindersAggregate.RatingReminders;

namespace EzDinner.Application.Commands.RatingReminders;

public sealed class SendRatingRemindersCommand(IPushSubscriptionRepository subscriptions, IDinnerRepository dinners,
    IDishRepository dishes, IRatingRemindersRepository reminders, RatingReminderSelectionService selection,
    RatingReminderRecipientAccess access, IRatingReminderTransport transport, IClock clock, ILogger<SendRatingRemindersCommand> logger)
{
    private static readonly DateTimeZone Copenhagen = DateTimeZoneProviders.Tzdb["Europe/Copenhagen"];

    public async Task SendAsync(CancellationToken cancellationToken)
    {
        var today = clock.GetCurrentInstant().InZone(Copenhagen).Date;
        var processed = new HashSet<Guid>();
        foreach (var familyId in await subscriptions.GetAllFamilyIdsAsync().WaitAsync(cancellationToken))
        {
            try
            {
                var recipients = (await subscriptions.GetByFamilyIdAsync(familyId).WaitAsync(cancellationToken)).ToList();
                if (recipients.Count == 0) continue;
                var context = await LoadContextAsync(familyId, today, cancellationToken);
                foreach (var subscription in recipients)
                {
                    if (!processed.Add(subscription.UserId)) continue;
                    try { await SendToRecipientAsync(subscription, context, today, cancellationToken); }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                    catch (Exception exception) { logger.LogWarning("Rating reminder failed for user {UserId}: {FailureType}", subscription.UserId, exception.GetType().Name); }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception) { logger.LogWarning("Rating reminder family skipped {FamilyId}: {FailureType}", familyId, exception.GetType().Name); }
        }
    }

    private async Task<(IReadOnlyList<Dinner> Dinners, IReadOnlyList<Dish> Dishes)> LoadContextAsync(Guid familyId, LocalDate today, CancellationToken cancellationToken)
    {
        var recent = new List<Dinner>();
        await foreach (var dinner in dinners.GetAsync(familyId, today.PlusDays(-7), today.PlusDays(-1)).WithCancellation(cancellationToken)) recent.Add(dinner);
        return (recent, (await dishes.GetDishesAsync(familyId).WaitAsync(cancellationToken)).ToList());
    }

    private async Task<RatingReminderOccurrenceValueObject?> ReserveAsync(PushSubscription subscription,
        (IReadOnlyList<Dinner> Dinners, IReadOnlyList<Dish> Dishes) context, LocalDate today, CancellationToken cancellationToken)
    {
        for (var retry = 0; retry < 4; retry++)
        {
            var loaded = await reminders.GetWithRevisionAsync(subscription.UserId, cancellationToken);
            var state = loaded?.Reminders ?? ReminderState.CreateNew(subscription.UserId);
            var occurrence = selection.Select(subscription.FamilyId, context.Dinners, context.Dishes, state, today)
                .FirstOrDefault(candidate => state.CanAttemptPush(candidate, today));
            if (occurrence is null || !state.TryRecordPushAttempt(occurrence, today)) return null;
            state.PruneExpiredOccurrences(today);
            var saved = loaded.HasValue
                ? await reminders.SaveIfUnchangedAsync(state, loaded.Value.Revision, cancellationToken)
                : await reminders.CreateIfAbsentAsync(state, cancellationToken);
            if (saved) return occurrence;
            context = await LoadContextAsync(subscription.FamilyId, today, cancellationToken);
        }
        logger.LogInformation("Rating reminder skipped for user {UserId}: conditional conflict", subscription.UserId);
        return null;
    }

    private async Task SendToRecipientAsync(PushSubscription subscription,
        (IReadOnlyList<Dinner> Dinners, IReadOnlyList<Dish> Dishes) context, LocalDate today, CancellationToken cancellationToken)
    {
        var active = await subscriptions.GetByUserIdAsync(subscription.UserId).WaitAsync(cancellationToken);
        if (!MatchesSubscription(subscription, active) || !await access.CanReceiveAsync(subscription.UserId, subscription.FamilyId, cancellationToken)) return;
        var occurrence = await ReserveAsync(subscription, context, today, cancellationToken);
        if (occurrence is null) return;
        logger.LogInformation("Rating reminder attempt reserved for user {UserId}, dish {DishId}, date {Date}", subscription.UserId, occurrence.DishId, occurrence.DinnerDate);
        var finalContext = await LoadContextAsync(subscription.FamilyId, today, cancellationToken);
        var stored = await reminders.GetWithRevisionAsync(subscription.UserId, cancellationToken);
        active = await subscriptions.GetByUserIdAsync(subscription.UserId).WaitAsync(cancellationToken);
        if (!stored.HasValue || !stored.Value.Reminders.PushEnabled || !MatchesSubscription(subscription, active)
            || !await access.CanReceiveAsync(subscription.UserId, subscription.FamilyId, cancellationToken)
            || clock.GetCurrentInstant().InZone(Copenhagen).Date != today
            || !selection.Select(subscription.FamilyId, finalContext.Dinners, finalContext.Dishes, stored.Value.Reminders, today).Contains(occurrence))
        {
            logger.LogInformation("Rating reminder skipped after reservation for user {UserId}", subscription.UserId);
            return;
        }
        var expiry = occurrence.DinnerDate.PlusDays(8).AtStartOfDayInZone(Copenhagen).ToInstant();
        var ttl = (int)Math.Floor((expiry - clock.GetCurrentInstant()).TotalSeconds);
        if (ttl <= 0) return;
        var date = LocalDatePattern.Iso.Format(occurrence.DinnerDate);
        var name = finalContext.Dishes.Single(dish => dish.Id == occurrence.DishId).Name;
        var destination = $"/dishes/{occurrence.DishId}?familyId={occurrence.FamilyId}&ratingReminderDate={date}#my-rating";
        var payload = new RatingReminderPushPayload("rating_reminder", name, date, subscription.Language == "da" ? "da" : "en", destination);
        var outcome = await transport.SendAsync(subscription, payload, ttl, cancellationToken);
        if (outcome == RatingReminderTransportOutcome.SubscriptionExpired) await subscriptions.DeleteAsync(subscription).WaitAsync(cancellationToken);
        logger.LogInformation("Rating reminder outcome {Outcome} for user {UserId}", outcome, subscription.UserId);
    }

    private static bool MatchesSubscription(PushSubscription expected, PushSubscription? actual) => actual is not null
        && actual.Id == expected.Id && actual.UserId == expected.UserId && actual.FamilyId == expected.FamilyId
        && actual.Endpoint == expected.Endpoint && actual.P256dh == expected.P256dh && actual.Auth == expected.Auth;
}
