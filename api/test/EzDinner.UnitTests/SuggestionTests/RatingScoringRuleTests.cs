using EzDinner.Core.DomainServices.DinnerSuggestions;
using NodaTime;
using System;
using Xunit;

namespace EzDinner.UnitTests.SuggestionTests
{
    public class RatingScoringRuleTests
    {
        private readonly RatingScoringRule _rule = new();
        private readonly SuggestionContextValueObject _context = new(
            new LocalDate(2025, 1, 15),
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        [Fact]
        public void Score_MaxRating_ReturnsFivePointZero()
        {
            var candidate = new DishCandidateValueObject(Guid.NewGuid(), "Pasta", rating: 10, 30, 14, 0);

            Assert.Equal(5.0, _rule.Score(candidate, _context), precision: 10);
        }

        [Fact]
        public void Score_ZeroRating_ReturnsZero()
        {
            var candidate = new DishCandidateValueObject(Guid.NewGuid(), "Pasta", rating: 0, 30, 14, 0);

            Assert.Equal(0.0, _rule.Score(candidate, _context), precision: 10);
        }

        [Fact]
        public void Score_EqualsRatingDividedByTenTimesWeight()
        {
            var candidate = new DishCandidateValueObject(Guid.NewGuid(), "Pasta", rating: 7, 30, 14, 0);

            Assert.Equal((7.0 / 10.0) * 5.0, _rule.Score(candidate, _context), precision: 10);
        }

        [Fact]
        public void Score_HigherRatedDishScoresHigher()
        {
            var lowRated = new DishCandidateValueObject(Guid.NewGuid(), "A", rating: 3, 30, 14, 0);
            var highRated = new DishCandidateValueObject(Guid.NewGuid(), "B", rating: 8, 30, 14, 0);

            Assert.True(_rule.Score(highRated, _context) > _rule.Score(lowRated, _context));
        }
    }
}
