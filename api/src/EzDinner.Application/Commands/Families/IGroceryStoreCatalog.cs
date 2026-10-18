namespace EzDinner.Application.Commands.Families;

public interface IGroceryStoreCatalog
{
    Task<IReadOnlyList<GroceryStoreDescriptor>> GetStoresAsync(CancellationToken cancellationToken = default);
}

public sealed record GroceryStoreDescriptor(string Id, string Name, bool IsAvailable, bool IsBorderTrade, bool IsFlyerOnly);
