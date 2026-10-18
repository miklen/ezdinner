using EzDinner.Core.Aggregates.FamilyAggregate;

namespace EzDinner.Application.Commands.Families;

public sealed class SetGroceryStoresCommand(IFamilyRepository families, IGroceryStoreCatalog catalog)
{
    public async Task<IReadOnlyList<string>> ExecuteAsync(Guid familyId, IEnumerable<string> storeIds, CancellationToken cancellationToken = default)
    {
        var family = await families.GetFamily(familyId) ?? throw new KeyNotFoundException("FAMILY_NOT_FOUND");
        var stores = await catalog.GetStoresAsync(cancellationToken);
        family.ReplaceGroceryStores(storeIds, stores.Select(store => store.Id));
        cancellationToken.ThrowIfCancellationRequested();
        await families.SaveGroceryStoresAsync(family);
        return family.GroceryStores;
    }
}
