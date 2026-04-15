using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.DomainServices.DinnerSuggestions;
using NodaTime;
using NodaTime.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EzDinner.Query.Core.SuggestionQueries
{
    public class AiDinnerWeekPlanner : IDinnerWeekPlanner
    {
        private readonly ILlmWeekPlanClient _client;

        public AiDinnerWeekPlanner(ILlmWeekPlanClient client)
        {
            _client = client;
        }

        public async Task<IReadOnlyList<DaySuggestion>> PlanWeekAsync(
            FamilySuggestionContext context,
            LocalDate weekStart,
            IReadOnlyList<LocalDate> alreadyPlanned,
            string? userContext = null)
        {
            var alreadyPlannedSet = new HashSet<LocalDate>(alreadyPlanned);

            var dishes = context.ExcludedDishIds.Count > 0
                ? context.Dishes.Where(d => !context.ExcludedDishIds.Contains(d.Id)).ToList()
                : context.Dishes.ToList();

            if (!dishes.Any())
                return Array.Empty<DaySuggestion>();

            var unplannedDates = Enumerable.Range(0, 7)
                .Select(i => weekStart.PlusDays(i))
                .Where(d => !alreadyPlannedSet.Contains(d))
                .ToList();

            if (!unplannedDates.Any())
                return Array.Empty<DaySuggestion>();

            var catalog = BuildCatalog(dishes, context);
            var aiResults = await _client.PlanWeekAsync(catalog, weekStart, unplannedDates, userContext, CancellationToken.None);

            var validDishIds = new HashSet<Guid>(dishes.Select(d => d.Id));
            var unplannedDateStrings = new HashSet<string>(unplannedDates.Select(d => LocalDatePattern.Iso.Format(d)));

            var resultByDate = aiResults
                .Where(r => validDishIds.Contains(r.DishId) && unplannedDateStrings.Contains(r.Date))
                .GroupBy(r => r.Date)
                .ToDictionary(g => g.Key, g => g.First());

            var suggestions = new List<DaySuggestion>();
            foreach (var date in unplannedDates)
            {
                var dateStr = LocalDatePattern.Iso.Format(date);
                DishScoreValueObject? score = null;

                if (resultByDate.TryGetValue(dateStr, out var aiResult))
                {
                    score = new DishScoreValueObject(
                        aiResult.DishId,
                        aiResult.DishName,
                        totalScore: 0.0,
                        rating: 0.0,
                        daysSinceLast: 0,
                        reasons: string.IsNullOrEmpty(aiResult.Reason) ? null : new[] { aiResult.Reason });
                }

                suggestions.Add(new DaySuggestion(date, score));
            }

            return suggestions;
        }

        private static string BuildCatalog(
            IReadOnlyList<Dish> dishes,
            FamilySuggestionContext context)
        {
            var today = LocalDate.FromDateTime(System.DateTime.UtcNow);

            var lastUsedByDish = new Dictionary<Guid, LocalDate>();
            foreach (var dinner in context.HistoricalDinners)
            {
                foreach (var item in dinner.Menu)
                {
                    if (!lastUsedByDish.TryGetValue(item.DishId, out var existing) || dinner.Date > existing)
                        lastUsedByDish[item.DishId] = dinner.Date;
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("id | name | effort | weeks_since_last | wish_votes");
            sb.AppendLine("---|------|--------|-----------------|----------");

            foreach (var dish in dishes)
            {
                var effort = dish.Metadata.EffortLevel?.ToString() ?? "unknown";
                var weeksSince = "never";
                if (lastUsedByDish.TryGetValue(dish.Id, out var lastUsed))
                {
                    var daysDiff = Period.Between(lastUsed, today, PeriodUnits.Days).Days;
                    weeksSince = $"{Math.Round(daysDiff / 7.0, 1)}";
                }
                var votes = context.WishVotesByDishId.TryGetValue(dish.Id, out var v) ? v : 0;
                sb.AppendLine($"{dish.Id} | {dish.Name} | {effort} | {weeksSince} | {votes}");
            }

            return sb.ToString();
        }
    }
}
