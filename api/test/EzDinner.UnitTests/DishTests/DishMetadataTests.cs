using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.Shared;
using System;
using System.Collections.Generic;
using Xunit;

namespace EzDinner.UnitTests.DishTests
{
    public class DishMetadataTests
    {
        private static Dish CreateDish() =>
            new Dish(
                id: Guid.NewGuid(),
                familyId: Guid.NewGuid(),
                name: "Test Dish",
                url: null,
                tags: new List<Tag>(),
                notes: "",
                deleted: false,
                ratings: new List<Rating>());

        private static DishMetadataValueObject ConfirmedMetadata(
            EffortLevel effortLevel = EffortLevel.Quick,
            SeasonAffinity seasonAffinity = SeasonAffinity.AllYear,
            string cuisine = "Danish",
            IReadOnlyList<DishRole>? roles = null) =>
            new DishMetadataValueObject(
                roles ?? new[] { DishRole.Main },
                rolesConfirmed: true,
                effortLevel,
                effortLevelConfirmed: true,
                seasonAffinity,
                seasonAffinityConfirmed: true,
                cuisine,
                cuisineConfirmed: true);

        private static DishMetadataValueObject UnconfirmedMetadata(
            EffortLevel effortLevel = EffortLevel.Elaborate,
            SeasonAffinity seasonAffinity = SeasonAffinity.Winter,
            string cuisine = "Italian",
            IReadOnlyList<DishRole>? roles = null) =>
            new DishMetadataValueObject(
                roles ?? new[] { DishRole.Side },
                rolesConfirmed: false,
                effortLevel,
                effortLevelConfirmed: false,
                seasonAffinity,
                seasonAffinityConfirmed: false,
                cuisine,
                cuisineConfirmed: false);

        [Fact]
        public void UpdateMetadata_ConfirmedFieldsNotOverwrittenByUnconfirmedIncoming()
        {
            var dish = CreateDish();
            dish.UpdateMetadata(ConfirmedMetadata(effortLevel: EffortLevel.Quick));

            dish.UpdateMetadata(UnconfirmedMetadata(effortLevel: EffortLevel.Elaborate));

            Assert.Equal(EffortLevel.Quick, dish.Metadata.EffortLevel);
            Assert.True(dish.Metadata.EffortLevelConfirmed);
        }

        [Fact]
        public void UpdateMetadata_UnconfirmedFieldsUpdatedByIncomingUnconfirmed()
        {
            var dish = CreateDish();
            dish.UpdateMetadata(UnconfirmedMetadata(seasonAffinity: SeasonAffinity.Summer));

            dish.UpdateMetadata(UnconfirmedMetadata(seasonAffinity: SeasonAffinity.Winter));

            Assert.Equal(SeasonAffinity.Winter, dish.Metadata.SeasonAffinity);
        }

        [Fact]
        public void UpdateMetadata_ConfirmedIncomingOverwritesConfirmedCurrent()
        {
            var dish = CreateDish();
            dish.UpdateMetadata(ConfirmedMetadata(cuisine: "Danish"));

            dish.UpdateMetadata(ConfirmedMetadata(cuisine: "Italian"));

            Assert.Equal("Italian", dish.Metadata.Cuisine);
            Assert.True(dish.Metadata.CuisineConfirmed);
        }

        [Fact]
        public void UpdateMetadata_MultiRoleAssignment_AllRolesStored()
        {
            var dish = CreateDish();
            var roles = new[] { DishRole.Main, DishRole.Dessert };
            var metadata = new DishMetadataValueObject(
                roles, rolesConfirmed: true,
                null, false,
                null, false,
                null, false);

            dish.UpdateMetadata(metadata);

            Assert.Contains(DishRole.Main, dish.Metadata.Roles);
            Assert.Contains(DishRole.Dessert, dish.Metadata.Roles);
            Assert.Equal(2, dish.Metadata.Roles.Count);
        }

        [Fact]
        public void UpdateMetadata_ConfirmedRolesNotOverwrittenByUnconfirmedRoles()
        {
            var dish = CreateDish();
            var confirmed = new DishMetadataValueObject(
                new[] { DishRole.Main }, rolesConfirmed: true,
                null, false, null, false, null, false);
            var aiSuggested = new DishMetadataValueObject(
                new[] { DishRole.Side }, rolesConfirmed: false,
                null, false, null, false, null, false);

            dish.UpdateMetadata(confirmed);
            dish.UpdateMetadata(aiSuggested);

            Assert.Contains(DishRole.Main, dish.Metadata.Roles);
            Assert.DoesNotContain(DishRole.Side, dish.Metadata.Roles);
        }

        [Fact]
        public void Metadata_DefaultsToEmpty_WhenNotProvided()
        {
            var dish = CreateDish();

            Assert.Empty(dish.Metadata.Roles);
            Assert.False(dish.Metadata.RolesConfirmed);
            Assert.Null(dish.Metadata.EffortLevel);
            Assert.Null(dish.Metadata.SeasonAffinity);
            Assert.Null(dish.Metadata.Cuisine);
        }
    }
}
