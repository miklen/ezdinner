namespace EzDinner.Core.Aggregates.RatingRemindersAggregate;

public interface IRatingRemindersRepository
{
    Task<(RatingReminders Reminders, string Revision)?> GetWithRevisionAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> CreateIfAbsentAsync(RatingReminders reminders, CancellationToken cancellationToken);
    Task<bool> SaveIfUnchangedAsync(RatingReminders reminders, string revision, CancellationToken cancellationToken);
}
