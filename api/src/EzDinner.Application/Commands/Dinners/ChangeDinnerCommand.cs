using EzDinner.Core.Aggregates.DinnerAggregate;
using NodaTime;

namespace EzDinner.Application.Commands.Dinners;

public sealed class ChangeDinnerCommand(IConditionalDinnerRepository repository)
{
    public Task AddAsync(Guid familyId, LocalDate date, Guid dishId)
        => ApplyAsync(familyId, date, dinner => dinner.AddMenuItem(new(dishId)));

    public Task RemoveAsync(Guid familyId, LocalDate date, Guid dishId)
        => ApplyAsync(familyId, date, dinner => dinner.RemoveMenuItem(new(dishId)));

    public Task SetOptOutAsync(Guid familyId, LocalDate date, string reason)
        => ApplyAsync(familyId, date, dinner => dinner.SetOptOut(reason));

    public Task RemoveOptOutAsync(Guid familyId, LocalDate date)
        => ApplyAsync(familyId, date, dinner => dinner.RemoveOptOut());

    private async Task ApplyAsync(Guid familyId, LocalDate date, Action<Dinner> change)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var stored = await repository.GetWithRevisionAsync(familyId, date, default);
            var dinner = stored?.Dinner ?? Dinner.CreateNew(familyId, date);
            change(dinner);
            var saved = stored is {} current
                ? await repository.SaveIfUnchangedAsync(dinner, current.Revision, default)
                : await repository.CreateIfAbsentAsync(dinner, default);
            if (saved) return;
        }
        throw new InvalidOperationException("DINNER_CHANGE_CONFLICT");
    }
}
