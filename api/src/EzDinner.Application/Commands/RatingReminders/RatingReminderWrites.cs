using EzDinner.Core.Aggregates.RatingRemindersAggregate;
using NodaTime;
using ReminderState = EzDinner.Core.Aggregates.RatingRemindersAggregate.RatingReminders;

namespace EzDinner.Application.Commands.RatingReminders;

public sealed class RatingReminderWrites(IRatingRemindersRepository repository)
{
    public async Task<ReminderState> ApplyAsync(Guid userId, LocalDate today,
        Func<ReminderState, CancellationToken, Task<bool>> change, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var stored = await repository.GetWithRevisionAsync(userId, cancellationToken);
            var state = stored?.Reminders ?? ReminderState.CreateNew(userId);
            if (!await change(state, cancellationToken)) return state;
            state.PruneExpiredOccurrences(today);
            var saved = stored.HasValue
                ? await repository.SaveIfUnchangedAsync(state, stored.Value.Revision, cancellationToken)
                : await repository.CreateIfAbsentAsync(state, cancellationToken);
            if (saved) return state;
        }
        throw new InvalidOperationException("RATING_REMINDER_CONFLICT");
    }
}
