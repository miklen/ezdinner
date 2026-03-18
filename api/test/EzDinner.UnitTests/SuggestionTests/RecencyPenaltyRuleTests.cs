using EzDinner.Core.DomainServices.DinnerSuggestions;
using NodaTime;
using System;
using Xunit;

namespace EzDinner.UnitTests.SuggestionTests
{
    public class RecencyPenaltyRuleTests
    {
        private readonly RecencyPenaltyRule _rule = new();
        private readonly SuggestionContextValueObject _context = new(
            new LocalDate(2025, 1, 15),
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public void Score_UsedWithinThreeDays_ReturnsZeroScorePenalty(int daysSinceLast)
        {
            var candidate = new DishCandidateValueObject(Guid.NewGuid(), "Pasta", 8, daysSinceLast, 14, 0);

            Assert.Equal(-1000.0, _rule.Score(candidate, _context));
        }

        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(7)]
        public void Score_UsedWithinSevenDays_ReturnsRecentUsePenalty(int daysSinceLast)
        {
            var candidate = new DishCandidateValueObject(Guid.NewGuid(), "Pasta", 8, daysSinceLast, 14, 0);

            Assert.Equal(-20.0, _rule.Score(candidate, _context));
        }

        [Theory]
        [InlineData(8)]
        [InlineData(14)]
        [InlineData(365)]
        public void Score_UsedMoreThanSevenDaysAgo_ReturnsZero(int daysSinceLast)
        {
            var candidate = new DishCandidateValueObject(Guid.NewGuid(), "Pasta", 8, daysSinceLast, 14, 0);

            Assert.Equal(0.0, _rule.Score(candidate, _context));
        }
    }
}
