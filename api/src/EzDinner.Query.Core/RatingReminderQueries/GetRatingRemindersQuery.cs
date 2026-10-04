using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.RatingRemindersAggregate;
using EzDinner.Core.DomainServices.RatingReminders;
using NodaTime;
using ReminderState = EzDinner.Core.Aggregates.RatingRemindersAggregate.RatingReminders;

namespace EzDinner.Query.Core.RatingReminderQueries;

public sealed class GetRatingRemindersQuery(IDinnerRepository dinners, IDishRepository dishes,
    IRatingRemindersRepository reminders, RatingReminderSelectionService selection, IClock clock)
{
    public async Task<RatingReminderQueueResult> GetAsync(Guid userId, Guid familyId, CancellationToken cancellationToken)
    {
        var today = clock.GetCurrentInstant().InZone(DateTimeZoneProviders.Tzdb["Europe/Copenhagen"]).Date;
        var recent = new List<Dinner>();
        await foreach (var dinner in dinners.GetAsync(familyId, today.PlusDays(-7), today.PlusDays(-1)).WithCancellation(cancellationToken)) recent.Add(dinner);
        var catalog = (await dishes.GetDishesAsync(familyId).WaitAsync(cancellationToken)).ToList();
        var stored = await reminders.GetWithRevisionAsync(userId, cancellationToken);
        var state = stored?.Reminders ?? ReminderState.CreateNew(userId);
        var names = catalog.ToDictionary(dish => dish.Id, dish => dish.Name);
        var results = selection.Select(familyId, recent, catalog, state, today)
            .Select(occurrence => new RatingReminderResult(occurrence.DishId, names[occurrence.DishId], occurrence.DinnerDate)).ToList().AsReadOnly();
        return new(today, results);
    }
}
