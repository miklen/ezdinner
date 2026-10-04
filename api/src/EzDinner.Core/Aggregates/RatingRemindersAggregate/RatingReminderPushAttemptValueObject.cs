using NodaTime;

namespace EzDinner.Core.Aggregates.RatingRemindersAggregate;

public sealed record RatingReminderPushAttemptValueObject
{
    public RatingReminderOccurrenceValueObject Occurrence { get; }
    public LocalDate AttemptedOn { get; }

    public RatingReminderPushAttemptValueObject(RatingReminderOccurrenceValueObject occurrence, LocalDate attemptedOn)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        if (!occurrence.IsRecentPast(attemptedOn)) throw new ArgumentException("RATING_REMINDER_DATE_INVALID", nameof(occurrence));
        Occurrence = occurrence;
        AttemptedOn = attemptedOn;
    }
}
