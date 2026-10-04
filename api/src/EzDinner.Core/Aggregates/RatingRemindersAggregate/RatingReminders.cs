using NodaTime;

namespace EzDinner.Core.Aggregates.RatingRemindersAggregate;

public sealed class RatingReminders
{
    private readonly List<DismissedRatingReminderValueObject> _dismissals;
    private readonly List<RatingReminderPushAttemptValueObject> _pushAttempts;

    public Guid Id { get; }
    public bool PushEnabled { get; private set; }
    public LocalDate? LatestPushAttemptDay { get; private set; }
    public IReadOnlyList<DismissedRatingReminderValueObject> Dismissals => _dismissals.AsReadOnly();
    public IReadOnlyList<RatingReminderPushAttemptValueObject> PushAttempts => _pushAttempts.AsReadOnly();

    private RatingReminders(Guid id, bool pushEnabled, IEnumerable<DismissedRatingReminderValueObject> dismissals,
        IEnumerable<RatingReminderPushAttemptValueObject> pushAttempts, LocalDate? latestPushAttemptDay)
    {
        if (id == Guid.Empty) throw new ArgumentException("RATING_REMINDER_USER_REQUIRED", nameof(id));
        Id = id;
        PushEnabled = pushEnabled;
        _dismissals = dismissals.ToList();
        _pushAttempts = pushAttempts.ToList();
        LatestPushAttemptDay = _pushAttempts.Select(attempt => (LocalDate?)attempt.AttemptedOn)
            .Append(latestPushAttemptDay).Max();
    }

    public static RatingReminders CreateNew(Guid userId) => new(userId, false, [], [], null);

    public static RatingReminders Hydrate(Guid userId, bool pushEnabled,
        IEnumerable<DismissedRatingReminderValueObject> dismissals,
        IEnumerable<RatingReminderPushAttemptValueObject> pushAttempts, LocalDate? latestPushAttemptDay) =>
        new(userId, pushEnabled, dismissals, pushAttempts, latestPushAttemptDay);

    public bool SetPushEnabled(bool enabled)
    {
        if (PushEnabled == enabled) return false;
        PushEnabled = enabled;
        return true;
    }

    public bool IsDismissed(RatingReminderOccurrenceValueObject occurrence) => _dismissals.Any(dismissal => dismissal.Suppresses(occurrence));

    public bool DismissThroughOccurrence(RatingReminderOccurrenceValueObject occurrence, LocalDate today)
    {
        if (!occurrence.IsRecentPast(today)) throw new ArgumentException("RATING_REMINDER_DATE_INVALID", nameof(occurrence));
        if (IsDismissed(occurrence)) return false;
        _dismissals.RemoveAll(dismissal => dismissal.FamilyId == occurrence.FamilyId && dismissal.DishId == occurrence.DishId);
        _dismissals.Add(new(occurrence.FamilyId, occurrence.DishId, occurrence.DinnerDate));
        return true;
    }

    public bool HasAttempted(RatingReminderOccurrenceValueObject occurrence) => _pushAttempts.Any(attempt => attempt.Occurrence == occurrence);

    public bool CanAttemptPush(RatingReminderOccurrenceValueObject occurrence, LocalDate today) =>
        PushEnabled && occurrence.IsRecentPast(today) && (!LatestPushAttemptDay.HasValue || LatestPushAttemptDay.Value < today)
        && !IsDismissed(occurrence) && !HasAttempted(occurrence);

    public bool TryRecordPushAttempt(RatingReminderOccurrenceValueObject occurrence, LocalDate today)
    {
        if (!CanAttemptPush(occurrence, today)) return false;
        _pushAttempts.Add(new(occurrence, today));
        LatestPushAttemptDay = today;
        return true;
    }

    public void PruneExpiredOccurrences(LocalDate today)
    {
        var cutoff = today.PlusDays(-7);
        _dismissals.RemoveAll(dismissal => dismissal.DismissedThroughDate < cutoff);
        _pushAttempts.RemoveAll(attempt => attempt.Occurrence.DinnerDate < cutoff);
    }
}
