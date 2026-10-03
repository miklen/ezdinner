using EzDinner.Core.Aggregates.DinnerAggregate;
using NodaTime;

namespace EzDinner.Application.Commands.Dinners;

public sealed class UndoDinnerMenuChangeCommand(IConditionalDinnerRepository repository)
{
    public async Task<bool> UndoAsync(Guid familyId, LocalDate date, Guid dishId,
        DinnerStateValueObject before, DinnerStateValueObject after, CancellationToken cancellationToken)
    {
        var loaded = await repository.GetWithRevisionAsync(familyId, date, cancellationToken);
        if (loaded is null) return false;
        var (dinner, revision) = loaded.Value;
        if (!dinner.UndoMenuChange(dishId, before, after)) return false;
        return await repository.SaveIfUnchangedAsync(dinner, revision, cancellationToken);
    }
}
