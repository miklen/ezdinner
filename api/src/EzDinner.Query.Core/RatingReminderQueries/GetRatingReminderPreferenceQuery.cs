using EzDinner.Core.Aggregates.RatingRemindersAggregate;

namespace EzDinner.Query.Core.RatingReminderQueries;

public sealed class GetRatingReminderPreferenceQuery(IRatingRemindersRepository reminders)
{
    public async Task<bool> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        (await reminders.GetWithRevisionAsync(userId, cancellationToken))?.Reminders.PushEnabled ?? false;
}
