using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.DomainServices.DinnerSuggestions;
using NodaTime;
using System;
using System.Linq;
using Xunit;

namespace EzDinner.UnitTests.SuggestionTests
{
    public class DishRoleFilterTests
    {
        private static readonly Guid FamilyId = Guid.NewGuid();
        private static readonly LocalDate Target = new(2025, 6, 15);

        private static Dish DishWithRoles(params DishRole[] roles)
        {
            var dish = Dish.CreateNew(FamilyId, "Test Dish");
            var metadata = new DishMetadataValueObject(
                roles, rolesConfirmed: false,
                null, false, null, false, null, false);
            dish.UpdateMetadata(metadata);
            return dish;
        }

        private static Dish DishWithNoRoles() => Dish.CreateNew(FamilyId, "Unenriched Dish");

        [Fact]
        public void BuildCandidates_SideDishWithRolesSet_IsExcluded()
        {
            var side = DishWithRoles(DishRole.Side);

            var candidates = DishCandidateFactory.BuildCandidates(
                new[] { side },
                Array.Empty<EzDinner.Core.Aggregates.DinnerAggregate.Dinner>(),
                Target).ToList();

            Assert.Empty(candidates);
        }

        [Fact]
        public void BuildCandidates_MainDish_IsIncluded()
        {
            var main = DishWithRoles(DishRole.Main);

            var candidates = DishCandidateFactory.BuildCandidates(
                new[] { main },
                Array.Empty<EzDinner.Core.Aggregates.DinnerAggregate.Dinner>(),
                Target).ToList();

            Assert.Single(candidates);
            Assert.Equal(main.Id, candidates[0].DishId);
        }

        [Fact]
        public void BuildCandidates_DishWithNoRoles_IsIncluded()
        {
            var unenriched = DishWithNoRoles();

            var candidates = DishCandidateFactory.BuildCandidates(
                new[] { unenriched },
                Array.Empty<EzDinner.Core.Aggregates.DinnerAggregate.Dinner>(),
                Target).ToList();

            Assert.Single(candidates);
            Assert.Equal(unenriched.Id, candidates[0].DishId);
        }

        [Fact]
        public void BuildCandidates_DishWithMainAndDessertRoles_IsIncluded()
        {
            var pandekager = DishWithRoles(DishRole.Main, DishRole.Dessert);

            var candidates = DishCandidateFactory.BuildCandidates(
                new[] { pandekager },
                Array.Empty<EzDinner.Core.Aggregates.DinnerAggregate.Dinner>(),
                Target).ToList();

            Assert.Single(candidates);
            Assert.Equal(pandekager.Id, candidates[0].DishId);
        }

        [Fact]
        public void BuildCandidates_MixedDishes_OnlyMainEligibleAndUnenrichedIncluded()
        {
            var main = DishWithRoles(DishRole.Main);
            var side = DishWithRoles(DishRole.Side);
            var dessert = DishWithRoles(DishRole.Dessert);
            var unenriched = DishWithNoRoles();

            var candidates = DishCandidateFactory.BuildCandidates(
                new[] { main, side, dessert, unenriched },
                Array.Empty<EzDinner.Core.Aggregates.DinnerAggregate.Dinner>(),
                Target).ToList();

            Assert.Equal(2, candidates.Count);
            Assert.Contains(candidates, c => c.DishId == main.Id);
            Assert.Contains(candidates, c => c.DishId == unenriched.Id);
        }
    }
}
