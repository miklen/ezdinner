using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.DomainServices.DinnerSuggestions;
using NodaTime;
using System;
using Xunit;

namespace EzDinner.UnitTests.SuggestionTests
{
    public class SeasonalAffinityRuleTests
    {
        private static readonly SeasonalAffinityRule Rule = new();

        private static DishCandidateValueObject CandidateWith(SeasonAffinity? seasonAffinity) =>
            new(Guid.NewGuid(), "Dish", 5, 30, 14, 0, seasonAffinity: seasonAffinity);

        private static SuggestionContextValueObject ContextForMonth(int month) =>
            new(new LocalDate(2025, month, 15), Array.Empty<Guid>(), Array.Empty<Guid>());

        [Fact]
        public void Score_SummerDishInSummer_ReturnsBoost()
        {
            var score = Rule.Score(CandidateWith(SeasonAffinity.Summer), ContextForMonth(7));
            Assert.Equal(8.0, score);
        }

        [Fact]
        public void Score_WinterDishInSummer_ReturnsPenalty()
        {
            var score = Rule.Score(CandidateWith(SeasonAffinity.Winter), ContextForMonth(7));
            Assert.Equal(-8.0, score);
        }

        [Fact]
        public void Score_AllYearDish_ReturnsZero()
        {
            var score = Rule.Score(CandidateWith(SeasonAffinity.AllYear), ContextForMonth(7));
            Assert.Equal(0.0, score);
        }

        [Fact]
        public void Score_NoAffinitySet_ReturnsZero()
        {
            var score = Rule.Score(CandidateWith(null), ContextForMonth(7));
            Assert.Equal(0.0, score);
        }

        [Theory]
        [InlineData(12, SeasonAffinity.Winter)]
        [InlineData(1, SeasonAffinity.Winter)]
        [InlineData(2, SeasonAffinity.Winter)]
        [InlineData(3, SeasonAffinity.Spring)]
        [InlineData(4, SeasonAffinity.Spring)]
        [InlineData(5, SeasonAffinity.Spring)]
        [InlineData(6, SeasonAffinity.Summer)]
        [InlineData(7, SeasonAffinity.Summer)]
        [InlineData(8, SeasonAffinity.Summer)]
        [InlineData(9, SeasonAffinity.Autumn)]
        [InlineData(10, SeasonAffinity.Autumn)]
        [InlineData(11, SeasonAffinity.Autumn)]
        public void Score_SeasonBoundaries_MatchingDishGetsBoost(int month, SeasonAffinity expectedSeason)
        {
            var score = Rule.Score(CandidateWith(expectedSeason), ContextForMonth(month));
            Assert.Equal(8.0, score);
        }

        [Theory]
        [InlineData(12, SeasonAffinity.Winter)]
        [InlineData(3, SeasonAffinity.Spring)]
        [InlineData(6, SeasonAffinity.Summer)]
        [InlineData(9, SeasonAffinity.Autumn)]
        public void Score_SeasonBoundaries_NonMatchingDishGetsPenalty(int month, SeasonAffinity matchingSeason)
        {
            var mismatch = matchingSeason == SeasonAffinity.Summer ? SeasonAffinity.Winter : SeasonAffinity.Summer;
            var score = Rule.Score(CandidateWith(mismatch), ContextForMonth(month));
            Assert.Equal(-8.0, score);
        }
    }
}
