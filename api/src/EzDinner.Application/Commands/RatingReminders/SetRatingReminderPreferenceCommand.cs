using NodaTime;

namespace EzDinner.Application.Commands.RatingReminders;

public sealed class SetRatingReminderPreferenceCommand(RatingReminderWrites writes, IClock clock)
{
    public async Task<bool> SetAsync(Guid userId, bool enabled, CancellationToken cancellationToken)
    {
        var today = clock.GetCurrentInstant().InZone(DateTimeZoneProviders.Tzdb["Europe/Copenhagen"]).Date;
        var state = await writes.ApplyAsync(userId, today, (reminders, _) => Task.FromResult(reminders.SetPushEnabled(enabled)), cancellationToken);
        return state.PushEnabled;
    }
}
