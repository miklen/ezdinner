using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.RatingRemindersAggregate;
using NodaTime;

namespace EzDinner.Core.DomainServices.RatingReminders;

public sealed class RatingReminderSelectionService
{
    public IReadOnlyList<RatingReminderOccurrenceValueObject> Select(Guid familyId, IEnumerable<Dinner> dinners,
        IEnumerable<Dish> dishes, Aggregates.RatingRemindersAggregate.RatingReminders reminders, LocalDate today)
    {
        var eligibleDishIds = dishes.Where(dish => dish.FamilyId == familyId && !dish.Deleted && !dish.IsArchived
            && !dish.Ratings.Any(rating => rating.RaterId == reminders.Id)).Select(dish => dish.Id).ToHashSet();

        return dinners.Where(dinner => dinner.FamilyId == familyId && !dinner.IsOptedOut
                && dinner.Date >= today.PlusDays(-7) && dinner.Date < today)
            .SelectMany(dinner => dinner.Menu.Where(item => eligibleDishIds.Contains(item.DishId))
                .Select(item => new RatingReminderOccurrenceValueObject(familyId, item.DishId, dinner.Date)))
            .GroupBy(occurrence => occurrence.DishId)
            .Select(group => group.OrderByDescending(occurrence => occurrence.DinnerDate).First())
            .Where(occurrence => !reminders.IsDismissed(occurrence))
            .OrderByDescending(occurrence => occurrence.DinnerDate)
            .ThenBy(occurrence => occurrence.DishId.ToString("D"), StringComparer.Ordinal)
            .ToList().AsReadOnly();
    }
}
