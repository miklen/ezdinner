using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.DomainServices.DinnerSuggestions;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace EzDinner.UnitTests.SuggestionTests
{
    public class DishCandidateFactoryTests
    {
        private static readonly Guid FamilyId = Guid.NewGuid();
        private static readonly LocalDate Target = new(2025, 6, 15);

        [Fact]
        public void BuildCandidates_ExcludesDeletedDishes()
        {
            var active = Dish.CreateNew(FamilyId, "Active");
            var deleted = Dish.CreateNew(FamilyId, "Deleted");
            deleted.Delete();

            var candidates = DishCandidateFactory.BuildCandidates(
                new[] { active, deleted },
                Array.Empty<Dinner>(),
                Target).ToList();

            Assert.Single(candidates);
            Assert.Equal(active.Id, candidates[0].DishId);
        }

        [Fact]
        public void BuildCandidates_NeverUsedDish_DefaultsDaysSinceLastTo365()
        {
            var dish = Dish.CreateNew(FamilyId, "New Dish");

            var candidates = DishCandidateFactory.BuildCandidates(
                new[] { dish },
                Array.Empty<Dinner>(),
                Target).ToList();

            Assert.Equal(365, candidates[0].DaysSinceLast);
        }

        [Fact]
        public void BuildCandidates_ComputesDaysSinceLastFromMostRecentUsage()
        {
            var dish = Dish.CreateNew(FamilyId, "Pasta");
            var lastUsed = Target.PlusDays(-10);
            var dinner = Dinner.CreateNew(FamilyId, lastUsed);
            dinner.AddMenuItem(new MenuItem(dish.Id));

            var candidates = DishCandidateFactory.BuildCandidates(
                new[] { dish },
                new[] { dinner },
                Target).ToList();

            Assert.Equal(10, candidates[0].DaysSinceLast);
        }

        [Fact]
        public void BuildCandidates_UsedOnce_DefaultsTypicalFrequencyDaysToFourteen()
        {
            var dish = Dish.CreateNew(FamilyId, "Pasta");
            var dinner = Dinner.CreateNew(FamilyId, Target.PlusDays(-20));
            dinner.AddMenuItem(new MenuItem(dish.Id));

            var candidates = DishCandidateFactory.BuildCandidates(
                new[] { dish },
                new[] { dinner },
                Target).ToList();

            Assert.Equal(14.0, candidates[0].TypicalFrequencyDays, precision: 10);
        }

        [Fact]
        public void BuildCandidates_NeverUsedDish_LeftoverFrequencyRatioIsZero()
        {
            var dish = Dish.CreateNew(FamilyId, "Pasta");

            var candidates = DishCandidateFactory.BuildCandidates(
                new[] { dish },
                Array.Empty<Dinner>(),
                Target).ToList();

            Assert.Equal(0.0, candidates[0].LeftoverFrequencyRatio, precision: 10);
        }

        [Fact]
        public void BuildCandidates_ConsecutiveDayPairsPresent_ComputesLeftoverFrequencyRatioCorrectly()
        {
            var dish = Dish.CreateNew(FamilyId, "Stew");
            var baseDate = new LocalDate(2025, 5, 1);

            var dinners = new List<Dinner>();
            foreach (var offset in new[] { 1, 2, 5, 6, 10, 11, 15, 16, 20 })
            {
                var dinner = Dinner.CreateNew(FamilyId, baseDate.PlusDays(offset));
                dinner.AddMenuItem(new MenuItem(dish.Id));
                dinners.Add(dinner);
            }

            var candidates = DishCandidateFactory.BuildCandidates(
                new[] { dish },
                dinners,
                baseDate.PlusDays(21)).ToList();

            Assert.True(candidates[0].LeftoverFrequencyRatio >= 0.30,
                $"Expected ratio ≥ 0.30 but was {candidates[0].LeftoverFrequencyRatio:F4}");
        }
    }
}
