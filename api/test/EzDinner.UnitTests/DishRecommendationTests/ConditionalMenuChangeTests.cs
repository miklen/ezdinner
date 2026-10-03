using EzDinner.Application.Commands.Dinners;
using EzDinner.Core.Aggregates.DinnerAggregate;
using Moq;
using NodaTime;
using Xunit;

namespace EzDinner.UnitTests.DishRecommendationTests;

public class ConditionalMenuChangeTests
{
    private readonly Guid family = Guid.NewGuid();
    private readonly Guid dish = Guid.NewGuid();
    private readonly LocalDate date = new(2026, 10, 5);

    [Fact]
    public void NewDinnerIdentityIsStableWithinFamilyAndDateAndDoesNotRewriteExistingIds()
    {
        var first = Dinner.CreateNew(family, date);
        Assert.Equal(first.Id, Dinner.CreateNew(family, date).Id);
        Assert.NotEqual(first.Id, Dinner.CreateNew(family, date.PlusDays(1)).Id);
        Assert.NotEqual(first.Id, Dinner.CreateNew(Guid.NewGuid(), date).Id);
        var existing = Guid.NewGuid();
        Assert.Equal(existing, new Dinner(existing, family, date, [], []).Id);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AnotherMembersAlreadyCompletedChangeProducesNoUndoReceipt(bool adding)
    {
        var dinner = Dinner.CreateNew(family, date);
        if (adding) dinner.AddMenuItem(new(dish));
        var repository = Stored(dinner);
        var command = new ChangeDinnerMenuCommand(repository.Object);
        var expected = new DinnerStateValueObject(adding ? [] : [dish], null);
        var result = adding
            ? await command.AddAsync(family, date, dish, expected, default)
            : await command.RemoveAsync(family, date, dish, expected, default);
        Assert.IsType<DinnerMenuChangeResult.NoOp>(result);
        repository.Verify(store => store.SaveIfUnchangedAsync(It.IsAny<Dinner>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task AnUnseenOptOutCannotBeCleared()
    {
        var dinner = Dinner.CreateNew(family, date);
        dinner.SetOptOut("Eating out");
        var repository = Stored(dinner);
        var result = await new ChangeDinnerMenuCommand(repository.Object).AddAsync(family, date, dish, new([], null), default);
        Assert.IsType<DinnerMenuChangeResult.Conflict>(result);
        Assert.Equal("Eating out", dinner.OptOut.Reason);
        repository.Verify(store => store.SaveIfUnchangedAsync(It.IsAny<Dinner>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task SuccessfulConditionalWriteReturnsActualCanonicalStates()
    {
        var dinner = Dinner.CreateNew(family, date);
        var other = Guid.NewGuid();
        dinner.AddMenuItem(new(other));
        var repository = Stored(dinner);
        repository.Setup(store => store.SaveIfUnchangedAsync(dinner, "revision", default)).ReturnsAsync(true);
        var receipt = Assert.IsType<DinnerMenuChangeResult.Changed>(await new ChangeDinnerMenuCommand(repository.Object)
            .AddAsync(family, date, dish, new([other], null), default));
        Assert.True(receipt.Before.DishIds.SetEquals([other]));
        Assert.True(receipt.After.DishIds.SetEquals([other, dish]));
    }

    [Fact]
    public async Task SaveRaceCannotProduceAnUndoReceipt()
    {
        var dinner = Dinner.CreateNew(family, date);
        var repository = Stored(dinner);
        repository.Setup(store => store.SaveIfUnchangedAsync(dinner, "revision", default)).ReturnsAsync(false);
        Assert.IsType<DinnerMenuChangeResult.Conflict>(await new ChangeDinnerMenuCommand(repository.Object)
            .AddAsync(family, date, dish, new([], null), default));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FirstAssignmentUsesConditionalCreation(bool wonCreation)
    {
        var repository = new Mock<IConditionalDinnerRepository>(MockBehavior.Strict);
        repository.Setup(store => store.GetWithRevisionAsync(family, date, default)).ReturnsAsync(((Dinner, string)?)null);
        repository.Setup(store => store.CreateIfAbsentAsync(It.Is<Dinner>(dinner => dinner.Id == Dinner.CreateNew(family, date).Id), default))
            .ReturnsAsync(wonCreation);
        var result = await new ChangeDinnerMenuCommand(repository.Object).AddAsync(family, date, dish, new([], null), default);
        if (wonCreation) Assert.IsType<DinnerMenuChangeResult.Changed>(result);
        if (!wonCreation) Assert.IsType<DinnerMenuChangeResult.Conflict>(result);
        repository.VerifyAll();
    }

    [Fact]
    public async Task OversizedResultIsRejectedBeforePersistence()
    {
        var dinner = Dinner.CreateNew(family, date);
        foreach (var id in Enumerable.Range(0, 100).Select(_ => Guid.NewGuid())) dinner.AddMenuItem(new(id));
        var repository = Stored(dinner);
        await Assert.ThrowsAsync<ArgumentException>(() => new ChangeDinnerMenuCommand(repository.Object)
            .AddAsync(family, date, dish, DinnerStateValueObject.From(dinner), default));
        repository.Verify(store => store.SaveIfUnchangedAsync(It.IsAny<Dinner>(), It.IsAny<string>(), default), Times.Never);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("remove")]
    [InlineData("removeOptOut")]
    public async Task LegacyFirstCreationRaceReappliesIntentWithoutOverwritingWinningDish(string operation)
    {
        var winner = Dinner.CreateNew(family, date);
        var other = Guid.NewGuid();
        winner.AddMenuItem(new(other));
        var repository = new Mock<IConditionalDinnerRepository>(MockBehavior.Strict);
        repository.SetupSequence(store => store.GetWithRevisionAsync(family, date, default))
            .ReturnsAsync(((Dinner, string)?)null).ReturnsAsync((winner, "winner"));
        repository.Setup(store => store.CreateIfAbsentAsync(It.IsAny<Dinner>(), default)).ReturnsAsync(false);
        repository.Setup(store => store.SaveIfUnchangedAsync(winner, "winner", default)).ReturnsAsync(true);
        var command = new ChangeDinnerCommand(repository.Object);
        if (operation == "add") await command.AddAsync(family, date, dish);
        if (operation == "remove") await command.RemoveAsync(family, date, dish);
        if (operation == "removeOptOut") await command.RemoveOptOutAsync(family, date);
        Assert.Contains(winner.Menu, item => item.DishId == other);
        Assert.Equal(operation == "add", winner.Menu.Any(item => item.DishId == dish));
        repository.VerifyAll();
    }

    private Mock<IConditionalDinnerRepository> Stored(Dinner dinner)
    {
        var repository = new Mock<IConditionalDinnerRepository>();
        repository.Setup(store => store.GetWithRevisionAsync(family, date, default)).ReturnsAsync((dinner, "revision"));
        return repository;
    }
}
