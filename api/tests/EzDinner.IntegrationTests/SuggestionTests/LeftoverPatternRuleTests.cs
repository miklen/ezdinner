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
    public class LeftoverPatternRuleTests : IClassFixture<StartupFixture>
    {
        private readonly IDishRepository _dishRepository;
        private readonly IDinnerRepository _dinnerRepository;

        public LeftoverPatternRuleTests(StartupFixture startup)
        {
            _dishRepository = (IDishRepository)startup.Provider.GetService(typeof(IDishRepository))!;
            _dinnerRepository = (IDinnerRepository)startup.Provider.GetService(typeof(IDinnerRepository))!;
        }

        /// <summary>
        /// 6.4 Seeds history where a dish appears on consecutive days ≥ 30% of the time,
        /// verifies the LeftoverPatternRule applies a bonus when the preceding day matches.
        /// </summary>
        [Fact]
        public async Task LeftoverPatternRule_ConsecutiveDayHistory_AppliesBonusWhenPrecedingDayMatches()
        {
            var familyId = Guid.NewGuid();
            var baseDate = new LocalDate(2025, 5, 1);
            var dish = Dish.CreateNew(familyId, "Leftover Stew");
            await _dishRepository.SaveAsync(dish);

            // Seed 4 consecutive-day pairs out of 10 appearances → 40% ratio (≥ 30% threshold)
            // Dates: 1,2 (pair), 5,6 (pair), 10,11 (pair), 15,16 (pair), 20
            var dinnerDates = new[] { 1, 2, 5, 6, 10, 11, 15, 16, 20 };
            var dinners = new List<Dinner>();
            foreach (var offset in dinnerDates)
            {
                var dinner = Dinner.CreateNew(familyId, baseDate.PlusDays(offset));
                dinner.AddMenuItem(new MenuItem(dish.Id));
                await _dinnerRepository.SaveAsync(dinner);
                dinners.Add(dinner);
            }

            try
            {
                var allDinners = new List<Dinner>();
                await foreach (var d in _dinnerRepository.GetAsync(familyId, LocalDate.MinIsoValue, LocalDate.MaxIsoValue))
                    allDinners.Add(d);

                var targetDate = baseDate.PlusDays(21);
                var allDishes = await _dishRepository.GetDishesAsync(familyId);
                var candidates = DishCandidateFactory.BuildCandidates(allDishes, allDinners, targetDate).ToList();

                var candidate = candidates.Single(c => c.DishId == dish.Id);

                // Verify leftover frequency ratio meets threshold
                Assert.True(candidate.LeftoverFrequencyRatio >= 0.30,
                    $"Expected ratio ≥ 0.30 but got {candidate.LeftoverFrequencyRatio:F2}");

                // Rule applies bonus when dish is in adjacentDishIds (preceding day used this dish)
                var rule = new LeftoverPatternRule();
                var contextWithAdjacent = new SuggestionContextValueObject(
                    targetDate,
                    adjacentDishIds: new List<Guid> { dish.Id },
                    excludedDishIds: Array.Empty<Guid>());
                var contextWithoutAdjacent = new SuggestionContextValueObject(
                    targetDate,
                    adjacentDishIds: Array.Empty<Guid>(),
                    excludedDishIds: Array.Empty<Guid>());

                var scoreWithBonus = rule.Score(candidate, contextWithAdjacent);
                var scoreWithoutBonus = rule.Score(candidate, contextWithoutAdjacent);

                Assert.True(scoreWithBonus > 0, "Expected a positive bonus when preceding day matches");
                Assert.Equal(0.0, scoreWithoutBonus);
            }
            finally
            {
                foreach (var dinner in dinners)
                    await _dinnerRepository.DeleteAsync(dinner);
                dish.Delete();
                await _dishRepository.SaveAsync(dish);
            }
        }
    }
}
