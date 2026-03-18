using EzDinner.Core.DomainServices.DinnerSuggestions;
using NodaTime;
using System;
using System.Collections.Generic;
using Xunit;

namespace EzDinner.UnitTests.SuggestionTests
{
    public class LeftoverPatternRuleTests
    {
        private readonly LeftoverPatternRule _rule = new();
        private readonly LocalDate _date = new(2025, 1, 15);

        [Fact]
        public void Score_HighRatioAndDishIsAdjacent_ReturnsFifteenBonus()
        {
            var dishId = Guid.NewGuid();
            var candidate = new DishCandidateValueObject(dishId, "Stew", 8, 1, 14, leftoverFrequencyRatio: 0.4);
            var context = new SuggestionContextValueObject(_date, new List<Guid> { dishId }, Array.Empty<Guid>());

            Assert.Equal(15.0, _rule.Score(candidate, context));
        }

        [Fact]
        public void Score_RatioBelowThreshold_ReturnsZero()
        {
            var dishId = Guid.NewGuid();
            var candidate = new DishCandidateValueObject(dishId, "Stew", 8, 1, 14, leftoverFrequencyRatio: 0.29);
            var context = new SuggestionContextValueObject(_date, new List<Guid> { dishId }, Array.Empty<Guid>());

            Assert.Equal(0.0, _rule.Score(candidate, context));
        }

        [Fact]
        public void Score_HighRatioButDishNotAdjacent_ReturnsZero()
        {
            var candidate = new DishCandidateValueObject(Guid.NewGuid(), "Stew", 8, 1, 14, leftoverFrequencyRatio: 0.5);
            var context = new SuggestionContextValueObject(_date, Array.Empty<Guid>(), Array.Empty<Guid>());

            Assert.Equal(0.0, _rule.Score(candidate, context));
        }

        [Fact]
        public void Score_ExactlyAtThreshold_ReturnsFifteenBonus()
        {
            var dishId = Guid.NewGuid();
            var candidate = new DishCandidateValueObject(dishId, "Stew", 8, 1, 14, leftoverFrequencyRatio: 0.30);
            var context = new SuggestionContextValueObject(_date, new List<Guid> { dishId }, Array.Empty<Guid>());

            Assert.Equal(15.0, _rule.Score(candidate, context));
        }
    }
}
