using EzDinner.Core.DomainServices.DinnerSuggestions;
using NodaTime;
using System;
using System.Collections.Generic;
using Xunit;

namespace EzDinner.UnitTests.SuggestionTests
{
    public class DinnerSuggestionEngineTests
    {
        private readonly SuggestionContextValueObject _context = new(
            new LocalDate(2025, 1, 15),
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        private static DinnerSuggestionEngineService EngineWith(params IScoringRule[] rules) =>
            new(rules);

        [Fact]
        public void Rank_ReturnsCandidatesSortedByDescendingTotalScore()
        {
            var engine = EngineWith(new RatingScoringRule());
            var lowRated = new DishCandidateValueObject(Guid.NewGuid(), "Low", rating: 3, 30, 14, 0);
            var highRated = new DishCandidateValueObject(Guid.NewGuid(), "High", rating: 9, 30, 14, 0);

            var ranked = engine.Rank(new[] { lowRated, highRated }, _context);

            Assert.Equal(highRated.DishId, ranked[0].DishId);
            Assert.Equal(lowRated.DishId, ranked[1].DishId);
        }

        [Fact]
        public void Rank_SameInputsAlwaysProduceSameOrder()
        {
            var engine = EngineWith(new OverdueScoringRule(), new RatingScoringRule());
            var a = new DishCandidateValueObject(Guid.NewGuid(), "A", rating: 8, 20, 14, 0);
            var b = new DishCandidateValueObject(Guid.NewGuid(), "B", rating: 5, 10, 14, 0);
            var candidates = new[] { a, b };

            var first = engine.Rank(candidates, _context);
            var second = engine.Rank(candidates, _context);

            Assert.Equal(first[0].DishId, second[0].DishId);
            Assert.Equal(first[1].DishId, second[1].DishId);
        }

        [Fact]
        public void Rank_AggregatesScoresFromAllRules()
        {
            var engine = EngineWith(new RatingScoringRule(), new OverdueScoringRule());
            var candidate = new DishCandidateValueObject(Guid.NewGuid(), "Pasta", rating: 10, daysSinceLast: 14, typicalFrequencyDays: 14, leftoverFrequencyRatio: 0);

            var ranked = engine.Rank(new[] { candidate }, _context);

            var expectedScore = 5.0 + 10.0;
            Assert.Equal(expectedScore, ranked[0].TotalScore, precision: 10);
        }

        [Fact]
        public void Rank_EmptyCandidates_ReturnsEmptyList()
        {
            var engine = EngineWith(new RatingScoringRule());

            var ranked = engine.Rank(Array.Empty<DishCandidateValueObject>(), _context);

            Assert.Empty(ranked);
        }
    }
}
