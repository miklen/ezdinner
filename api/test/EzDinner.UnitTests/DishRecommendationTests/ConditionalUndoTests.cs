using EzDinner.Application.Commands.Dinners;
using EzDinner.Core.Aggregates.DinnerAggregate;
using Moq;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using Xunit;

namespace EzDinner.UnitTests.DishRecommendationTests;

public class ConditionalUndoTests
{
    [Fact]
    public void UndoCannotRemoveAReassignedDish()
    {
        var dish = Guid.NewGuid();
        var dinner = Saved();
        var before = DinnerStateValueObject.From(dinner, dish);
        dinner.AddMenuItem(new(dish));
        var after = DinnerStateValueObject.From(dinner, dish);
        dinner.RemoveMenuItem(new(dish));
        dinner.AddMenuItem(new(dish));
        Assert.False(dinner.UndoMenuChange(dish, before, after));
        Assert.Equal(dish, Assert.Single(dinner.Menu).DishId);
    }

    [Fact]
    public void OptOutCannotBeRestoredAfterDayChangesAndReturnsToSameMenu()
    {
        var dish = Guid.NewGuid();
        var later = Guid.NewGuid();
        var dinner = Saved();
        dinner.SetOptOut("Eating out");
        var before = DinnerStateValueObject.From(dinner, dish);
        dinner.AddMenuItem(new(dish));
        var after = DinnerStateValueObject.From(dinner, dish);
        dinner.AddMenuItem(new(later));
        dinner.RemoveMenuItem(new(later));
        Assert.False(dinner.UndoMenuChange(dish, before, after));
        Assert.Null(dinner.OptOut);
        Assert.Equal(dish, Assert.Single(dinner.Menu).DishId);
    }

    [Fact]
    public void UndoAdditionPreservesLaterUnrelatedItems()
    {
        var dish = Guid.NewGuid();
        var unrelated = Guid.NewGuid();
        var later = Guid.NewGuid();
        var dinner = Saved(unrelated);
        var before = DinnerStateValueObject.From(dinner, dish);
        dinner.AddMenuItem(new(dish));
        var after = DinnerStateValueObject.From(dinner, dish);
        dinner.AddMenuItem(new(later));
        Assert.True(dinner.UndoMenuChange(dish, before, after));
        Assert.Equal([unrelated, later], dinner.Menu.Select(item => item.DishId));
    }

    [Fact]
    public void UndoRemovalPreservesLaterUnrelatedItems()
    {
        var dish = Guid.NewGuid();
        var unrelated = Guid.NewGuid();
        var later = Guid.NewGuid();
        var dinner = Saved(unrelated, dish);
        var before = DinnerStateValueObject.From(dinner, dish);
        dinner.RemoveMenuItem(new(dish));
        var after = DinnerStateValueObject.From(dinner, dish);
        dinner.AddMenuItem(new(later));
        Assert.True(dinner.UndoMenuChange(dish, before, after));
        Assert.Equal([unrelated, later, dish], dinner.Menu.Select(item => item.DishId));
    }

    [Fact]
    public void DuplicateAssignmentCannotCreateDestructiveUndo()
    {
        var dish = Guid.NewGuid();
        var dinner = Saved(dish);
        Assert.Throws<ArgumentException>(() => dinner.UndoMenuChange(dish, new([dish], null), new([dish], null)));
        Assert.Equal(dish, Assert.Single(dinner.Menu).DishId);
    }

    [Fact]
    public void ClearedOptOutIsRestoredOnlyForUnchangedDay()
    {
        var dish = Guid.NewGuid();
        var dinner = Saved();
        dinner.SetOptOut("Eating out");
        var before = DinnerStateValueObject.From(dinner, dish);
        dinner.AddMenuItem(new(dish));
        var after = DinnerStateValueObject.From(dinner, dish);
        Assert.True(dinner.UndoMenuChange(dish, before, after));
        Assert.Equal("Eating out", dinner.OptOut.Reason);
        Assert.Empty(dinner.Menu);
    }

    [Fact]
    public void LaterAssignmentPreventsDestructiveOptOutRestoration()
    {
        var dish = Guid.NewGuid();
        var later = Guid.NewGuid();
        var dinner = Saved(dish, later);
        Assert.False(dinner.UndoMenuChange(dish, new([], "Eating out"), new([dish], null)));
        Assert.Equal([dish, later], dinner.Menu.Select(item => item.DishId));
        Assert.Null(dinner.OptOut);
    }

    [Fact]
    public async Task StorageConflictRemainsConflict()
    {
        var dish = Guid.NewGuid();
        var dinner = Saved();
        var before = DinnerStateValueObject.From(dinner, dish);
        dinner.AddMenuItem(new(dish));
        var after = DinnerStateValueObject.From(dinner, dish);
        var repository = new Mock<IConditionalDinnerRepository>();
        repository.Setup(store => store.GetWithRevisionAsync(dinner.FamilyId, dinner.Date, default)).ReturnsAsync((dinner, "revision"));
        repository.Setup(store => store.SaveIfUnchangedAsync(dinner, "revision", default)).ReturnsAsync(false);
        Assert.False(await new UndoDinnerMenuChangeCommand(repository.Object).UndoAsync(dinner.FamilyId, dinner.Date, dish, before, after, default));
        repository.Verify(store => store.SaveIfUnchangedAsync(dinner, "revision", default), Times.Once);
    }

    [Fact]
    public void UndoCannotRestoreARemovalAfterAnotherRemovalOfSameDish()
    {
        var dish = Guid.NewGuid();
        var dinner = Saved(dish);
        var before = DinnerStateValueObject.From(dinner, dish);
        dinner.RemoveMenuItem(new(dish));
        var after = DinnerStateValueObject.From(dinner, dish);
        dinner.AddMenuItem(new(dish));
        dinner.RemoveMenuItem(new(dish));
        Assert.False(dinner.UndoMenuChange(dish, before, after));
        Assert.Empty(dinner.Menu);
    }

    [Fact]
    public void LaterOptOutDecisionInvalidatesRemovalReceiptEvenAfterClearingIt()
    {
        var dish = Guid.NewGuid();
        var dinner = Saved(dish);
        var before = DinnerStateValueObject.From(dinner, dish);
        dinner.RemoveMenuItem(new(dish));
        var after = DinnerStateValueObject.From(dinner, dish);
        dinner.SetOptOut("Eating out");
        dinner.RemoveOptOut();
        Assert.False(dinner.UndoMenuChange(dish, before, after));
        Assert.Empty(dinner.Menu);
    }

    [Fact]
    public void UndoProvenanceSurvivesPersistence()
    {
        var dish = Guid.NewGuid();
        var dinner = Saved(dish);
        var settings = new Newtonsoft.Json.JsonSerializerSettings().ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb);
        var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<Dinner>(Newtonsoft.Json.JsonConvert.SerializeObject(dinner, settings), settings);
        Assert.Equal(dinner.ChangeId, restored.ChangeId);
        Assert.Equal(dinner.DishChangeIds[dish], restored.DishChangeIds[dish]);
        Assert.Equal(dinner.OptOutChangeId, restored.OptOutChangeId);
    }

    [Fact]
    public async Task StaleMembershipDoesNotAttemptSave()
    {
        var dish = Guid.NewGuid();
        var dinner = Saved();
        var repository = new Mock<IConditionalDinnerRepository>();
        repository.Setup(store => store.GetWithRevisionAsync(dinner.FamilyId, dinner.Date, default)).ReturnsAsync((dinner, "revision"));
        Assert.False(await new UndoDinnerMenuChangeCommand(repository.Object).UndoAsync(dinner.FamilyId, dinner.Date, dish, new([], null), new([dish], null), default));
        repository.Verify(store => store.SaveIfUnchangedAsync(It.IsAny<Dinner>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Dinner Saved(params Guid[] dishes)
    {
        var dinner = Dinner.CreateNew(Guid.NewGuid(), new LocalDate(2026, 10, 5));
        foreach (var dish in dishes) dinner.AddMenuItem(new MenuItem(dish));
        return dinner;
    }
}
