using EzDinner.Core.Aggregates.DinnerAggregate;
using NodaTime;

namespace EzDinner.Application.Commands.Dinners;

public abstract record DinnerMenuChangeResult
{
    public sealed record Changed(DinnerStateValueObject Before, DinnerStateValueObject After) : DinnerMenuChangeResult;
    public sealed record NoOp : DinnerMenuChangeResult;
    public sealed record Conflict : DinnerMenuChangeResult;
}

public sealed class ChangeDinnerMenuCommand(IConditionalDinnerRepository repository)
{
    public Task<DinnerMenuChangeResult> AddAsync(Guid familyId, LocalDate date, Guid dishId,
        DinnerStateValueObject expected, CancellationToken cancellationToken)
        => ChangeAsync(familyId, date, dishId, expected, true, cancellationToken);

    public Task<DinnerMenuChangeResult> RemoveAsync(Guid familyId, LocalDate date, Guid dishId,
        DinnerStateValueObject expected, CancellationToken cancellationToken)
        => ChangeAsync(familyId, date, dishId, expected, false, cancellationToken);

    private async Task<DinnerMenuChangeResult> ChangeAsync(Guid familyId, LocalDate date, Guid dishId,
        DinnerStateValueObject expected, bool adding, CancellationToken cancellationToken)
    {
        if (familyId == Guid.Empty || dishId == Guid.Empty) throw new ArgumentException("INVALID_MENU_CHANGE");
        var stored = await repository.GetWithRevisionAsync(familyId, date, cancellationToken);
        var dinner = stored?.Dinner ?? Dinner.CreateNew(familyId, date);
        var before = DinnerStateValueObject.From(dinner, dishId);
        if (before.DishIds.Contains(dishId) == adding) return new DinnerMenuChangeResult.NoOp();
        if (!expected.Matches(dinner)) return new DinnerMenuChangeResult.Conflict();
        if (adding) dinner.AddMenuItem(new(dishId));
        if (!adding) dinner.RemoveMenuItem(new(dishId));
        var after = DinnerStateValueObject.From(dinner, dishId);
        var saved = stored is {} current
            ? await repository.SaveIfUnchangedAsync(dinner, current.Revision, cancellationToken)
            : await repository.CreateIfAbsentAsync(dinner, cancellationToken);
        return saved ? new DinnerMenuChangeResult.Changed(before, after)
            : new DinnerMenuChangeResult.Conflict();
    }
}
