using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.RatingRemindersAggregate;
using NodaTime;

namespace EzDinner.Application.Commands.RatingReminders;

public sealed class DismissRatingReminderCommand(RatingReminderWrites writes, IDinnerRepository dinners, IDishRepository dishes, IClock clock)
{
    public async Task DismissAsync(Guid userId, RatingReminderOccurrenceValueObject occurrence, CancellationToken cancellationToken)
    {
        var today = clock.GetCurrentInstant().InZone(DateTimeZoneProviders.Tzdb["Europe/Copenhagen"]).Date;
        if (!occurrence.IsRecentPast(today)) throw new ArgumentException("RATING_REMINDER_DATE_INVALID", nameof(occurrence));
        await writes.ApplyAsync(userId, today, async (state, token) =>
        {
            var dish = await dishes.GetDishAsync(occurrence.DishId).WaitAsync(token);
            if (dish is null || dish.FamilyId != occurrence.FamilyId) throw new ArgumentException("DISH_NOT_FOUND");
            var dinner = await dinners.GetAsync(occurrence.FamilyId, occurrence.DinnerDate).WaitAsync(token);
            if (dinner is null || dinner.IsOptedOut || !dinner.Menu.Any(item => item.DishId == occurrence.DishId)) return false;
            if (dish.Deleted || dish.IsArchived || dish.Ratings.Any(rating => rating.RaterId == userId)) return false;
            return state.DismissThroughOccurrence(occurrence, today);
        }, cancellationToken);
    }
}
