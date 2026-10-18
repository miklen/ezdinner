using System.Text.RegularExpressions;

namespace EzDinner.Core.Aggregates.FamilyAggregate;

public sealed class GroceryStoreSelectionValueObject : IEquatable<GroceryStoreSelectionValueObject>
{
    public IReadOnlyList<string> StoreIds { get; }

    public GroceryStoreSelectionValueObject(IEnumerable<string> storeIds)
    {
        ArgumentNullException.ThrowIfNull(storeIds);
        StoreIds = Array.AsReadOnly(storeIds.Select(Canonicalize).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
    }

    public GroceryStoreSelectionValueObject Replace(IEnumerable<string> storeIds, IEnumerable<string> availableStoreIds)
    {
        var replacement = new GroceryStoreSelectionValueObject(storeIds);
        var allowed = availableStoreIds.Concat(StoreIds).ToHashSet(StringComparer.Ordinal);
        if (replacement.StoreIds.Any(id => !allowed.Contains(id)))
            throw new ArgumentException("GROCERY_STORE_UNSUPPORTED");
        return replacement;
    }

    private static string Canonicalize(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("GROCERY_STORES_INVALID");
        var canonical = id.Trim().ToLowerInvariant();
        if (canonical.Length > 80 || !Regex.IsMatch(canonical, "^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant))
            throw new ArgumentException("GROCERY_STORES_INVALID");
        return canonical;
    }

    public bool Equals(GroceryStoreSelectionValueObject? other) => other is not null && StoreIds.SequenceEqual(other.StoreIds);
    public override bool Equals(object? obj) => obj is GroceryStoreSelectionValueObject other && Equals(other);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var id in StoreIds) hash.Add(id, StringComparer.Ordinal);
        return hash.ToHashCode();
    }
}
