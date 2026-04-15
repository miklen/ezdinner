using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.DomainServices.DinnerSuggestions;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EzDinner.Query.Core.SuggestionQueries
{
    public class RuleBasedDinnerWeekPlanner : IDinnerWeekPlanner
    {
        private readonly DinnerSuggestionEngineService _engine;

        public RuleBasedDinnerWeekPlanner(DinnerSuggestionEngineService engine)
        {
            _engine = engine;
        }

        public Task<IReadOnlyList<DaySuggestion>> PlanWeekAsync(
            FamilySuggestionContext context,
            LocalDate weekStart,
            IReadOnlyList<LocalDate> alreadyPlanned,
            string? userContext = null)
        {
            var alreadyPlannedSet = new HashSet<LocalDate>(alreadyPlanned);
            var results = new List<DaySuggestion>();
            var suggestedByDate = new Dictionary<LocalDate, Guid?>();
            var suggestedThisWeek = new HashSet<Guid>();

            for (var i = 0; i < 7; i++)
            {
                var date = weekStart.PlusDays(i);

                if (alreadyPlannedSet.Contains(date))
                    continue;

                var adjacentDishIds = BuildAdjacentDishIds(date, context.HistoricalDinners, suggestedByDate);
                var candidates = DishCandidateFactory.BuildCandidates(context.Dishes, context.HistoricalDinners, date);

                var effectiveExclusions = context.ExcludedDishIds.Concat(suggestedThisWeek).ToList();
                var dayEffortPreference = context.EffortPreferences?.GetValueOrDefault(date);
                var suggestionContext = new SuggestionContextValueObject(date, adjacentDishIds, effectiveExclusions, dayEffortPreference, context.WishVotesByDishId);
                var ranked = _engine.Rank(candidates, suggestionContext);

                var selected = ranked.FirstOrDefault(s => !effectiveExclusions.Contains(s.DishId))
                    ?? ranked.FirstOrDefault(s => !context.ExcludedDishIds.Contains(s.DishId))
                    ?? ranked.FirstOrDefault();

                if (selected is not null)
                    suggestedThisWeek.Add(selected.DishId);

                suggestedByDate[date] = selected?.DishId;
                results.Add(new DaySuggestion(date, selected));
            }

            return Task.FromResult<IReadOnlyList<DaySuggestion>>(results);
        }

        private static List<Guid> BuildAdjacentDishIds(
            LocalDate date,
            IReadOnlyList<Dinner> historicalDinners,
            Dictionary<LocalDate, Guid?> suggestedByDate)
        {
            var prevDate = date.PlusDays(-1);
            var ids = new List<Guid>();

            var prevDinner = historicalDinners.FirstOrDefault(d => d.Date == prevDate);
            if (prevDinner is not null)
                ids.AddRange(prevDinner.Menu.Select(m => m.DishId));

            if (suggestedByDate.TryGetValue(prevDate, out var prevSuggestion) && prevSuggestion.HasValue)
                ids.Add(prevSuggestion.Value);

            return ids;
        }
    }
}
