namespace EzDinner.Core.Aggregates.DinnerAggregate;

public sealed class DinnerStateValueObject
{
    public IReadOnlySet<Guid> DishIds { get; }
    public string? OptOutReason { get; }
    public Guid ChangeId { get; }
    public Guid DishChangeId { get; }
    public Guid OptOutChangeId { get; }

    public DinnerStateValueObject(IEnumerable<Guid> dishIds, string? optOutReason,
        Guid changeId = default, Guid dishChangeId = default, Guid optOutChangeId = default)
    {
        var ids = dishIds.ToHashSet();
        if (ids.Contains(Guid.Empty) || ids.Count > 100) throw new ArgumentException("INVALID_DINNER_STATE");
        if (optOutReason is not null && (string.IsNullOrWhiteSpace(optOutReason) || optOutReason.Length > 500 || ids.Count > 0))
            throw new ArgumentException("INVALID_DINNER_STATE");
        DishIds = System.Collections.Frozen.FrozenSet.ToFrozenSet(ids);
        OptOutReason = optOutReason;
        ChangeId = changeId;
        DishChangeId = dishChangeId;
        OptOutChangeId = optOutChangeId;
    }

    public static DinnerStateValueObject From(Dinner dinner, Guid dishId = default) => new(dinner.Menu.Select(item => item.DishId),
        dinner.OptOut?.Reason, dinner.ChangeId, dinner.DishChangeIds.GetValueOrDefault(dishId), dinner.OptOutChangeId);
    public bool Matches(Dinner dinner) => DishIds.SetEquals(dinner.Menu.Select(item => item.DishId)) && OptOutReason == dinner.OptOut?.Reason;
}
