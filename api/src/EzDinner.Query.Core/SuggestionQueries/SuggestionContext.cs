using NodaTime;
using System;
using System.Collections.Generic;

namespace EzDinner.Query.Core.SuggestionQueries
{
    public class SuggestionContext
    {
        public LocalDate TargetDate { get; }
        public IReadOnlyList<Guid> AdjacentDishIds { get; }
        public IReadOnlyList<Guid> ExcludedDishIds { get; }

        public SuggestionContext(
            LocalDate targetDate,
            IReadOnlyList<Guid> adjacentDishIds,
            IReadOnlyList<Guid> excludedDishIds)
        {
            TargetDate = targetDate;
            AdjacentDishIds = adjacentDishIds;
            ExcludedDishIds = excludedDishIds;
        }
    }
}
