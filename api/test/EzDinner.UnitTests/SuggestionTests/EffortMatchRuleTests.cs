using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.DomainServices.DinnerSuggestions;
using NodaTime;
using System;
using Xunit;

namespace EzDinner.UnitTests.SuggestionTests
{
    public class EffortMatchRuleTests
    {
        private static readonly EffortMatchRule Rule = new();
        private static readonly LocalDate AnyDate = new LocalDate(2025, 6, 15);

        private static DishCandidateValueObject CandidateWith(EffortLevel? effortLevel) =>
            new(Guid.NewGuid(), "Dish", 5, 30, 14, 0, effortLevel: effortLevel);

        private static SuggestionContextValueObject ContextWith(EffortLevel? preference) =>
            new(AnyDate, Array.Empty<Guid>(), Array.Empty<Guid>(), effortPreference: preference);

        [Fact]
        public void Score_MatchingEffortLevel_ReturnsBoost()
        {
            var score = Rule.Score(CandidateWith(EffortLevel.Quick), ContextWith(EffortLevel.Quick));
            Assert.Equal(6.0, score);
        }

        [Fact]
        public void Score_MismatchingEffortLevel_ReturnsPenalty()
        {
            var score = Rule.Score(CandidateWith(EffortLevel.Elaborate), ContextWith(EffortLevel.Quick));
            Assert.Equal(-6.0, score);
        }

        [Fact]
        public void Score_NoPreferenceInContext_ReturnsZero()
        {
            var score = Rule.Score(CandidateWith(EffortLevel.Quick), ContextWith(null));
            Assert.Equal(0.0, score);
        }

        [Fact]
        public void Score_NoCandidateEffortLevel_ReturnsZero()
        {
            var score = Rule.Score(CandidateWith(null), ContextWith(EffortLevel.Quick));
            Assert.Equal(0.0, score);
        }

        [Theory]
        [InlineData(EffortLevel.Quick, EffortLevel.Quick)]
        [InlineData(EffortLevel.Medium, EffortLevel.Medium)]
        [InlineData(EffortLevel.Elaborate, EffortLevel.Elaborate)]
        public void Score_AllMatchingCombinations_ReturnBoost(EffortLevel candidateLevel, EffortLevel preference)
        {
            var score = Rule.Score(CandidateWith(candidateLevel), ContextWith(preference));
            Assert.Equal(6.0, score);
        }

        [Theory]
        [InlineData(EffortLevel.Quick, EffortLevel.Medium)]
        [InlineData(EffortLevel.Quick, EffortLevel.Elaborate)]
        [InlineData(EffortLevel.Medium, EffortLevel.Quick)]
        [InlineData(EffortLevel.Medium, EffortLevel.Elaborate)]
        [InlineData(EffortLevel.Elaborate, EffortLevel.Quick)]
        [InlineData(EffortLevel.Elaborate, EffortLevel.Medium)]
        public void Score_AllMismatchingCombinations_ReturnPenalty(EffortLevel candidateLevel, EffortLevel preference)
        {
            var score = Rule.Score(CandidateWith(candidateLevel), ContextWith(preference));
            Assert.Equal(-6.0, score);
        }
    }
}
