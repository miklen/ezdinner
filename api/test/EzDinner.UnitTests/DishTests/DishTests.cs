using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.Shared;
using System;
using System.Collections.Generic;
using Xunit;

namespace EzDinner.UnitTests.DishTests
{
    public class DishTests
    {
        private static Dish CreateDish(bool isArchived = false)
        {
            return new Dish(
                id: Guid.NewGuid(),
                familyId: Guid.NewGuid(),
                name: "Test Dish",
                url: null,
                tags: new List<Tag>(),
                notes: "",
                deleted: false,
                ratings: new List<Rating>(),
                isArchived: isArchived);
        }

        [Fact]
        public void Archive_ActiveDish_SetsIsArchivedTrue()
        {
            var dish = CreateDish(isArchived: false);
            dish.Archive();
            Assert.True(dish.IsArchived);
        }

        [Fact]
        public void Archive_AlreadyArchivedDish_ThrowsInvalidOperationException()
        {
            var dish = CreateDish(isArchived: true);
            Assert.Throws<InvalidOperationException>(() => dish.Archive());
        }

        [Fact]
        public void Reactivate_ArchivedDish_SetsIsArchivedFalse()
        {
            var dish = CreateDish(isArchived: true);
            dish.Reactivate();
            Assert.False(dish.IsArchived);
        }

        [Fact]
        public void Reactivate_ActiveDish_ThrowsInvalidOperationException()
        {
            var dish = CreateDish(isArchived: false);
            Assert.Throws<InvalidOperationException>(() => dish.Reactivate());
        }

        [Fact]
        public void IsArchived_DefaultsToFalse_WhenConstructedWithoutParameter()
        {
            var dish = new Dish(
                id: Guid.NewGuid(),
                familyId: Guid.NewGuid(),
                name: "Test",
                url: null,
                tags: new List<Tag>(),
                notes: "",
                deleted: false,
                ratings: new List<Rating>());
            Assert.False(dish.IsArchived);
        }
    }
}
