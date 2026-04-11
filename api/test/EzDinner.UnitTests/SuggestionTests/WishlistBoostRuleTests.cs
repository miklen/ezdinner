using EzDinner.Core.DomainServices.DinnerSuggestions;
using NodaTime;
using System;
using System.Collections.Generic;
using Xunit;

namespace EzDinner.UnitTests.SuggestionTests
{
    public class WishlistBoostRuleTests
    {
        private readonly WishlistBoostRule _rule = new();
        private readonly LocalDate _date = new(2025, 1, 15);

        private SuggestionContextValueObject ContextWith(Dictionary<Guid, int> wishedDishIds) =>
            new(_date, Array.Empty<Guid>(), Array.Empty<Guid>(), wishedDishIds: wishedDishIds);

        private SuggestionContextValueObject EmptyContext() =>
            new(_date, Array.Empty<Guid>(), Array.Empty<Guid>());

        private static DishCandidateValueObject Candidate(Guid dishId) =>
            new(dishId, "Test Dish", rating: 5, daysSinceLast: 14, typicalFrequencyDays: 14, leftoverFrequencyRatio: 0);

        [Fact]
        public void Score_WishedDish_ReturnsVoteCountTimesBaseBoost()
        {
            var dishId = Guid.NewGuid();
            var context = ContextWith(new Dictionary<Guid, int> { [dishId] = 3 });
            var candidate = Candidate(dishId);

            Assert.Equal(3 * 0.3, _rule.Score(candidate, context), precision: 10);
        }

        [Fact]
        public void Score_UnwishedDish_ReturnsZero()
        {
            var context = ContextWith(new Dictionary<Guid, int> { [Guid.NewGuid()] = 2 });
            var candidate = Candidate(Guid.NewGuid());

            Assert.Equal(0, _rule.Score(candidate, context), precision: 10);
        }

        [Fact]
        public void Score_ExpiredWishNotInContext_ReturnsZero()
        {
            // Expired wishes are filtered out before building context — empty dict simulates this
            var dishId = Guid.NewGuid();
            var context = EmptyContext();
            var candidate = Candidate(dishId);

            Assert.Equal(0, _rule.Score(candidate, context), precision: 10);
        }

        [Fact]
        public void Explain_WishedDish_ReturnsReasonWithVoteCount()
        {
            var dishId = Guid.NewGuid();
            var context = ContextWith(new Dictionary<Guid, int> { [dishId] = 2 });
            var candidate = Candidate(dishId);

            var reason = _rule.Explain(candidate, context);

            Assert.Equal("Wished for by the family (2 votes)", reason);
        }

        [Fact]
        public void Explain_UnwishedDish_ReturnsNull()
        {
            var context = EmptyContext();
            var candidate = Candidate(Guid.NewGuid());

            Assert.Null(_rule.Explain(candidate, context));
        }
    }
}
