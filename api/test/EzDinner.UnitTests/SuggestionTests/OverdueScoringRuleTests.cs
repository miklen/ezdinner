using EzDinner.Core.DomainServices.DinnerSuggestions;
using NodaTime;
using System;
using System.Collections.Generic;
using Xunit;

namespace EzDinner.UnitTests.SuggestionTests
{
    public class OverdueScoringRuleTests
    {
        private readonly OverdueScoringRule _rule = new();
        private readonly SuggestionContextValueObject _context = new(
            new LocalDate(2025, 1, 15),
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        [Fact]
        public void Score_ReturnsRatioTimesWeight()
        {
            var candidate = new DishCandidateValueObject(Guid.NewGuid(), "Pasta", 8, daysSinceLast: 14, typicalFrequencyDays: 14, leftoverFrequencyRatio: 0);

            var score = _rule.Score(candidate, _context);

            Assert.Equal(10.0, score, precision: 10);
        }

        [Fact]
        public void Score_DishUsedTwiceAsLongAgoAsFrequency_ScoresHigherThanOnSchedule()
        {
            var onSchedule = new DishCandidateValueObject(Guid.NewGuid(), "Pasta", 5, daysSinceLast: 14, typicalFrequencyDays: 14, leftoverFrequencyRatio: 0);
            var overdue = new DishCandidateValueObject(Guid.NewGuid(), "Risotto", 5, daysSinceLast: 28, typicalFrequencyDays: 14, leftoverFrequencyRatio: 0);

            Assert.True(_rule.Score(overdue, _context) > _rule.Score(onSchedule, _context));
        }

        [Fact]
        public void Score_ZeroTypicalFrequency_FallsBackToDefault()
        {
            var candidate = new DishCandidateValueObject(Guid.NewGuid(), "Soup", 5, daysSinceLast: 14, typicalFrequencyDays: 0, leftoverFrequencyRatio: 0);

            var score = _rule.Score(candidate, _context);

            Assert.Equal(10.0, score, precision: 10);
        }
    }
}
