using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EzDinner.Query.Core.SuggestionQueries
{
    public class DinnerSuggestionService : IDinnerSuggestionService
    {
        private readonly DishCandidateFactory _candidateFactory;
        private readonly DinnerSuggestionEngine _engine;
        private readonly IDishRepository _dishRepository;
        private readonly IDinnerRepository _dinnerRepository;

        public DinnerSuggestionService(
            DishCandidateFactory candidateFactory,
            DinnerSuggestionEngine engine,
            IDishRepository dishRepository,
            IDinnerRepository dinnerRepository)
        {
            _candidateFactory = candidateFactory;
            _engine = engine;
            _dishRepository = dishRepository;
            _dinnerRepository = dinnerRepository;
        }

        public async Task<DishScore?> SuggestDay(Guid familyId, LocalDate date, IReadOnlyList<Guid> excludedDishIds)
        {
            var existingDinner = await _dinnerRepository.GetAsync(familyId, date);
            if (existingDinner is not null && existingDinner.IsPlanned)
                return null;

            var candidates = await _candidateFactory.BuildCandidatesAsync(familyId, date);

            var prevDayDinner = await _dinnerRepository.GetAsync(familyId, date.PlusDays(-1));
            var adjacentDishIds = prevDayDinner?.Menu.Select(m => m.DishId).ToList()
                ?? new List<Guid>();

            var context = new SuggestionContext(date, adjacentDishIds, excludedDishIds);
            var ranked = _engine.Rank(candidates, context);

            return ranked.FirstOrDefault(s => !excludedDishIds.Contains(s.DishId))
                ?? ranked.FirstOrDefault();
        }

        public async Task<IReadOnlyList<DaySuggestion>> SuggestWeek(Guid familyId, LocalDate weekStart, IReadOnlyList<Guid> excludedDishIds)
        {
            var dishes = await _dishRepository.GetDishesAsync(familyId);
            var allDinners = new List<Dinner>();
            await foreach (var dinner in _dinnerRepository.GetAsync(familyId, LocalDate.MinIsoValue, LocalDate.MaxIsoValue))
                allDinners.Add(dinner);

            var weekEnd = weekStart.PlusDays(6);
            var weekDinnersByDate = allDinners
                .Where(d => d.Date >= weekStart && d.Date <= weekEnd)
                .ToDictionary(d => d.Date);

            var results = new List<DaySuggestion>();
            var suggestedByDate = new Dictionary<LocalDate, Guid?>();
            // Tracks dishes already suggested earlier this week to avoid repeats
            var suggestedThisWeek = new HashSet<Guid>();

            for (var i = 0; i < 7; i++)
            {
                var date = weekStart.PlusDays(i);

                if (weekDinnersByDate.TryGetValue(date, out var planned) && planned.IsPlanned)
                    continue;

                var adjacentDishIds = BuildAdjacentDishIds(date, allDinners, suggestedByDate);
                var candidates = _candidateFactory.BuildCandidates(dishes, allDinners, date);

                // Combine client reroll exclusions with intra-week already-suggested dishes
                var effectiveExclusions = excludedDishIds.Concat(suggestedThisWeek).ToList();
                var context = new SuggestionContext(date, adjacentDishIds, effectiveExclusions);
                var ranked = _engine.Rank(candidates, context);

                var selected = ranked.FirstOrDefault(s => !effectiveExclusions.Contains(s.DishId))
                    ?? ranked.FirstOrDefault(s => !excludedDishIds.Contains(s.DishId))
                    ?? ranked.FirstOrDefault();

                if (selected is not null)
                    suggestedThisWeek.Add(selected.DishId);

                suggestedByDate[date] = selected?.DishId;
                results.Add(new DaySuggestion(date, selected));
            }

            return results;
        }

        private static List<Guid> BuildAdjacentDishIds(
            LocalDate date,
            List<Dinner> allDinners,
            Dictionary<LocalDate, Guid?> suggestedByDate)
        {
            var prevDate = date.PlusDays(-1);
            var ids = new List<Guid>();

            var prevDinner = allDinners.FirstOrDefault(d => d.Date == prevDate);
            if (prevDinner is not null)
                ids.AddRange(prevDinner.Menu.Select(m => m.DishId));

            if (suggestedByDate.TryGetValue(prevDate, out var prevSuggestion) && prevSuggestion.HasValue)
                ids.Add(prevSuggestion.Value);

            return ids;
        }
    }
}
