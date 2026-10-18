using EzDinner.Application.Commands.Families;
using EzDinner.Query.Core.FamilyQueries;

namespace EzDinner.Functions.Models.Query;

public sealed class FamilyDetailsQueryModel(FamilyDetails family, IReadOnlyList<GroceryStoreDescriptor> stores)
{
    public Guid Id => family.Id;
    public Guid OwnerId => family.OwnerId;
    public string? Name => family.Name;
    public IEnumerable<FamilyMemberDetails>? FamilyMembers => family.FamilyMembers;
    public DateTime CreatedDate => family.CreatedDate;
    public DateTime UpdatedDate => family.UpdatedDate;
    public IReadOnlyList<string> GroceryStores => family.GroceryStores;
    public IReadOnlyList<GroceryStoreDescriptor> SupportedGroceryStores { get; } = stores
        .Concat(family.GroceryStores.Where(id => stores.All(store => store.Id != id))
            .Select(id => new GroceryStoreDescriptor(id, id, false, false, false)))
        .OrderBy(store => store.Id, StringComparer.Ordinal).ToArray();
}
