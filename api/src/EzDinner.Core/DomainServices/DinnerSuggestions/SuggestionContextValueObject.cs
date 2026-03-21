using EzDinner.Core.Aggregates.DishAggregate;
using NodaTime;
using System;
using System.Collections.Generic;

namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public class SuggestionContextValueObject
    {
        public LocalDate TargetDate { get; }
        public IReadOnlyList<Guid> AdjacentDishIds { get; }
        public IReadOnlyList<Guid> ExcludedDishIds { get; }
        public EffortLevel? EffortPreference { get; }

        public SuggestionContextValueObject(
            LocalDate targetDate,
            IReadOnlyList<Guid> adjacentDishIds,
            IReadOnlyList<Guid> excludedDishIds,
            EffortLevel? effortPreference = null)
        {
            TargetDate = targetDate;
            AdjacentDishIds = adjacentDishIds;
            ExcludedDishIds = excludedDishIds;
            EffortPreference = effortPreference;
        }
    }
}
