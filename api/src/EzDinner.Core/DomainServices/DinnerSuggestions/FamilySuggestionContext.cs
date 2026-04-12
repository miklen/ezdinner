using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using NodaTime;
using System;
using System.Collections.Generic;

namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public class FamilySuggestionContext
    {
        public IReadOnlyList<Dish> Dishes { get; }
        public IReadOnlyList<Dinner> HistoricalDinners { get; }
        public IReadOnlyDictionary<Guid, int> WishVotesByDishId { get; }
        public IReadOnlyList<Guid> ExcludedDishIds { get; }
        public IReadOnlyDictionary<LocalDate, EffortLevel>? EffortPreferences { get; }

        public FamilySuggestionContext(
            IReadOnlyList<Dish> dishes,
            IReadOnlyList<Dinner> historicalDinners,
            IReadOnlyDictionary<Guid, int> wishVotesByDishId,
            IReadOnlyList<Guid> excludedDishIds,
            IReadOnlyDictionary<LocalDate, EffortLevel>? effortPreferences = null)
        {
            Dishes = dishes;
            HistoricalDinners = historicalDinners;
            WishVotesByDishId = wishVotesByDishId;
            ExcludedDishIds = excludedDishIds;
            EffortPreferences = effortPreferences;
        }
    }
}
