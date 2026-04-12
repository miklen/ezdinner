using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.WishlistAggregate;
using EzDinner.Core.DomainServices.DinnerSuggestions;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EzDinner.Query.Core.SuggestionQueries
{
    public class DinnerSuggestionService : IDinnerSuggestionService
    {
        private readonly DinnerSuggestionEngineService _engine;
        private readonly IDishRepository _dishRepository;
        private readonly IDinnerRepository _dinnerRepository;
        private readonly IWishlistRepository _wishlistRepository;
        private readonly IDinnerWeekPlanner _weekPlanner;
        private readonly SuggestionContextAssembler _contextAssembler;

        public DinnerSuggestionService(
            DinnerSuggestionEngineService engine,
            IDishRepository dishRepository,
            IDinnerRepository dinnerRepository,
            IWishlistRepository wishlistRepository,
            IDinnerWeekPlanner weekPlanner,
            SuggestionContextAssembler contextAssembler)
        {
            _engine = engine;
            _dishRepository = dishRepository;
            _dinnerRepository = dinnerRepository;
            _wishlistRepository = wishlistRepository;
            _weekPlanner = weekPlanner;
            _contextAssembler = contextAssembler;
        }

        public async Task<DishScoreValueObject?> SuggestDay(Guid familyId, LocalDate date, IReadOnlyList<Guid> excludedDishIds, EffortLevel? effortPreference = null)
        {
            var existingDinner = await _dinnerRepository.GetAsync(familyId, date);
            if (existingDinner is not null && existingDinner.IsPlanned)
                return null;

            var dishes = (await _dishRepository.GetDishesAsync(familyId)).Where(d => !d.IsArchived).ToList();
            var allDinners = new List<Dinner>();
            await foreach (var dinner in _dinnerRepository.GetAsync(familyId, LocalDate.MinIsoValue, LocalDate.MaxIsoValue))
                allDinners.Add(dinner);

            var candidates = DishCandidateFactory.BuildCandidates(dishes, allDinners, date);

            var adjacentDishIds = allDinners
                .Where(d => d.Date == date.PlusDays(-1))
                .SelectMany(d => d.Menu.Select(m => m.DishId))
                .ToList();

            var wishedDishIds = await BuildWishedDishIds(familyId);
            var context = new SuggestionContextValueObject(date, adjacentDishIds, excludedDishIds, effortPreference, wishedDishIds);
            var ranked = _engine.Rank(candidates, context);

            return ranked.FirstOrDefault(s => !excludedDishIds.Contains(s.DishId))
                ?? ranked.FirstOrDefault();
        }

        public async Task<IReadOnlyList<DaySuggestion>> SuggestWeek(Guid familyId, LocalDate weekStart, IReadOnlyList<Guid> excludedDishIds, Dictionary<LocalDate, EffortLevel>? effortPreferences = null)
        {
            var context = await _contextAssembler.AssembleAsync(familyId, excludedDishIds, effortPreferences);

            var weekEnd = weekStart.PlusDays(6);
            var alreadyPlanned = context.HistoricalDinners
                .Where(d => d.Date >= weekStart && d.Date <= weekEnd && d.IsPlanned)
                .Select(d => d.Date)
                .ToList();

            return await _weekPlanner.PlanWeekAsync(context, weekStart, alreadyPlanned);
        }

        private async Task<IReadOnlyDictionary<Guid, int>> BuildWishedDishIds(Guid familyId)
        {
            var now = SystemClock.Instance.GetCurrentInstant();
            var wishItems = await _wishlistRepository.GetActiveAsync(familyId);
            return wishItems
                .Where(w => !w.IsExpired(now))
                .ToDictionary(w => w.DishId, w => w.Votes.Count);
        }
    }
}
