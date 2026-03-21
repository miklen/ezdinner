using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.DomainServices.DinnerSuggestions;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EzDinner.Query.Core.SuggestionQueries
{
    public interface IDinnerSuggestionService
    {
        Task<DishScoreValueObject?> SuggestDay(Guid familyId, LocalDate date, IReadOnlyList<Guid> excludedDishIds, EffortLevel? effortPreference = null);
        Task<IReadOnlyList<DaySuggestion>> SuggestWeek(Guid familyId, LocalDate weekStart, IReadOnlyList<Guid> excludedDishIds, Dictionary<LocalDate, EffortLevel>? effortPreferences = null);
    }
}
