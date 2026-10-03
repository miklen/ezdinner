using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using NodaTime;

namespace EzDinner.Core.DomainServices.DishRecommendations;

public static class DishRecommendationCandidateFactory
{
    public static IReadOnlyList<DishRecommendationCandidateValueObject> Create(Guid familyId,
        IEnumerable<Dish> dishes, IEnumerable<Dinner> dinners, IReadOnlyDictionary<Guid, int> wishes,
        LocalDate today, LocalDate selectedMonday)
    {
        var familyDinners = dinners.Where(dinner => dinner.FamilyId == familyId).ToArray();
        return dishes.Where(dish => dish.FamilyId == familyId && !dish.Deleted && !dish.IsArchived)
            .Select(dish => CreateCandidate(dish, familyDinners, wishes, today, selectedMonday)).ToArray();
    }

    private static DishRecommendationCandidateValueObject CreateCandidate(Dish dish, IReadOnlyList<Dinner> dinners,
        IReadOnlyDictionary<Guid, int> wishes, LocalDate today, LocalDate monday)
    {
        var dates = dinners.Where(dinner => dinner.Menu.Any(item => item.DishId == dish.Id)).Select(dinner => dinner.Date).ToArray();
        var isWished = wishes.TryGetValue(dish.Id, out var votes);
        return new(dish.Id, dish.Name, dish.Ratings.Any() ? dish.Rating : null, dish.Metadata.Roles,
            isWished, votes, new(dates, today), dates.Where(date => date >= monday.PlusDays(-2) && date <= monday.PlusDays(6)));
    }
}
