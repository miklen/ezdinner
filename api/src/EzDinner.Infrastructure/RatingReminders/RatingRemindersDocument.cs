using EzDinner.Core.Aggregates.RatingRemindersAggregate;
using NodaTime;
using ReminderState = EzDinner.Core.Aggregates.RatingRemindersAggregate.RatingReminders;

namespace EzDinner.Infrastructure.RatingReminders;

public sealed record RatingRemindersDocument(Guid Id, bool PushEnabled,
    IReadOnlyList<DismissedRatingReminderValueObject> Dismissals,
    IReadOnlyList<RatingReminderPushAttemptValueObject> PushAttempts, LocalDate? LatestPushAttemptDay)
{
    public static RatingRemindersDocument FromAggregate(ReminderState reminders) =>
        new(reminders.Id, reminders.PushEnabled, reminders.Dismissals, reminders.PushAttempts, reminders.LatestPushAttemptDay);

    public ReminderState ToAggregate() => ReminderState.Hydrate(Id, PushEnabled, Dismissals, PushAttempts, LatestPushAttemptDay);
}
