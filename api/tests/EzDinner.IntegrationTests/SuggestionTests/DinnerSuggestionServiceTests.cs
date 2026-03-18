using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.DomainServices.DinnerSuggestions;
using EzDinner.Query.Core.SuggestionQueries;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EzDinner.IntegrationTests.SuggestionTests
{
    public class DinnerSuggestionServiceTests : IClassFixture<StartupFixture>
    {
        private readonly IDishRepository _dishRepository;
        private readonly IDinnerRepository _dinnerRepository;
        private readonly DinnerSuggestionService _suggestionService;

        public DinnerSuggestionServiceTests(StartupFixture startup)
        {
            _dishRepository = (IDishRepository)startup.Provider.GetService(typeof(IDishRepository))!;
            _dinnerRepository = (IDinnerRepository)startup.Provider.GetService(typeof(IDinnerRepository))!;

            var rules = new IScoringRule[]
            {
                new OverdueScoringRule(),
                new RatingScoringRule(),
                new RecencyPenaltyRule(),
                new LeftoverPatternRule(),
            };

            var engine = new DinnerSuggestionEngineService(rules);
            _suggestionService = new DinnerSuggestionService(engine, _dishRepository, _dinnerRepository);
        }

        /// <summary>
        /// 6.1 With known dish history, verify top-ranked dish is correctly selected.
        /// Seeds two dishes where dishA is overdue (last used 30 days ago, high rating)
        /// and dishB was used recently (4 days ago). DishA should win.
        /// </summary>
        [Fact]
        public async Task SuggestDay_WithKnownHistory_ReturnsTopRankedDish()
        {
            var familyId = Guid.NewGuid();
            var today = new LocalDate(2025, 6, 15);

            var dishA = Dish.CreateNew(familyId, "Overdue High-Rated Dish");
            dishA.SetRating(Guid.NewGuid(), 9);
            var dishB = Dish.CreateNew(familyId, "Recently Used Dish");
            dishB.SetRating(Guid.NewGuid(), 5);

            await _dishRepository.SaveAsync(dishA);
            await _dishRepository.SaveAsync(dishB);

            // dishA was last used 30 days ago
            var dinnerA = Dinner.CreateNew(familyId, today.PlusDays(-30));
            dinnerA.AddMenuItem(new MenuItem(dishA.Id));
            await _dinnerRepository.SaveAsync(dinnerA);

            // dishB was used 4 days ago (recency penalty applies)
            var dinnerB = Dinner.CreateNew(familyId, today.PlusDays(-4));
            dinnerB.AddMenuItem(new MenuItem(dishB.Id));
            await _dinnerRepository.SaveAsync(dinnerB);

            try
            {
                var suggestion = await _suggestionService.SuggestDay(familyId, today, Array.Empty<Guid>());

                Assert.NotNull(suggestion);
                Assert.Equal(dishA.Id, suggestion!.DishId);
            }
            finally
            {
                await _dinnerRepository.DeleteAsync(dinnerA);
                await _dinnerRepository.DeleteAsync(dinnerB);
                await CleanupDish(dishA);
                await CleanupDish(dishB);
            }
        }

        /// <summary>
        /// 6.2 With exclusion list, verify excluded dishes are skipped and second-ranked dish is returned.
        /// </summary>
        [Fact]
        public async Task SuggestDay_WithExclusionList_SkipsExcludedDishAndReturnsBestRemaining()
        {
            var familyId = Guid.NewGuid();
            var today = new LocalDate(2025, 6, 15);

            var dishA = Dish.CreateNew(familyId, "Best Dish");
            dishA.SetRating(Guid.NewGuid(), 9);
            var dishB = Dish.CreateNew(familyId, "Second Best Dish");
            dishB.SetRating(Guid.NewGuid(), 7);

            await _dishRepository.SaveAsync(dishA);
            await _dishRepository.SaveAsync(dishB);

            // Both overdue — dishA rated higher so normally wins
            var dinnerA = Dinner.CreateNew(familyId, today.PlusDays(-30));
            dinnerA.AddMenuItem(new MenuItem(dishA.Id));
            await _dinnerRepository.SaveAsync(dinnerA);

            var dinnerB = Dinner.CreateNew(familyId, today.PlusDays(-28));
            dinnerB.AddMenuItem(new MenuItem(dishB.Id));
            await _dinnerRepository.SaveAsync(dinnerB);

            try
            {
                var excluded = new List<Guid> { dishA.Id };
                var suggestion = await _suggestionService.SuggestDay(familyId, today, excluded);

                Assert.NotNull(suggestion);
                Assert.Equal(dishB.Id, suggestion!.DishId);
            }
            finally
            {
                await _dinnerRepository.DeleteAsync(dinnerA);
                await _dinnerRepository.DeleteAsync(dinnerB);
                await CleanupDish(dishA);
                await CleanupDish(dishB);
            }
        }

        /// <summary>
        /// 6.3 SuggestWeek — already-planned days are absent from response;
        /// unplanned days receive a suggestion.
        /// </summary>
        [Fact]
        public async Task SuggestWeek_PlannedDaysAbsent_UnplannedDaysReceiveSuggestion()
        {
            var familyId = Guid.NewGuid();
            var weekStart = new LocalDate(2025, 6, 9); // Monday

            var dish = Dish.CreateNew(familyId, "A Dish");
            dish.SetRating(Guid.NewGuid(), 7);
            await _dishRepository.SaveAsync(dish);

            // Plan Monday (day 0) already
            var mondayDinner = Dinner.CreateNew(familyId, weekStart);
            mondayDinner.AddMenuItem(new MenuItem(dish.Id));
            await _dinnerRepository.SaveAsync(mondayDinner);

            try
            {
                var suggestions = await _suggestionService.SuggestWeek(familyId, weekStart, Array.Empty<Guid>());

                // Monday should not be in the result (already planned)
                Assert.DoesNotContain(suggestions, s => s.Date == weekStart);

                // At least one other day should have a suggestion
                Assert.Contains(suggestions, s => s.Suggestion is not null);

                // All returned days are within the week
                foreach (var s in suggestions)
                {
                    Assert.True(s.Date >= weekStart && s.Date <= weekStart.PlusDays(6));
                }
            }
            finally
            {
                await _dinnerRepository.DeleteAsync(mondayDinner);
                await CleanupDish(dish);
            }
        }

        private async Task CleanupDish(Dish dish)
        {
            dish.Delete();
            await _dishRepository.SaveAsync(dish);
        }
    }
}
