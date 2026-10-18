using EzDinner.Core.DomainServices.DishRecommendations;
using NodaTime;

namespace EzDinner.Query.Core.DishRecommendationQueries;

public enum GroceryCoverageStatus { Fresh, Stale, Unavailable, Partial, Disabled, Unknown }

public sealed record GroceryCoverage(GroceryCoverageStatus Status, Instant EvaluatedAt,
    IReadOnlyList<string> StoreIds, IReadOnlyList<string> Limitations)
{
    public IReadOnlyList<GroceryStoreCoverage> Stores { get; init; } = [];
}

public sealed record GroceryStoreCoverage(string StoreId, string StoreName,
    GroceryCoverageStatus Status, int UsableOfferCount);

public sealed record GroceryOffersResult(IReadOnlyList<GroceryOfferValueObject> Offers,
    GroceryCoverage Coverage, IReadOnlyDictionary<string, string> StoreNames)
{
    public IReadOnlyDictionary<string, GroceryCoverageStatus>? StoreFreshness { get; init; }
}

public interface IGroceryOfferReader
{
    Task<GroceryOffersResult> ReadAsync(IReadOnlyList<string> storeIds, CancellationToken cancellationToken);
}

public sealed record DishIngredientProfilesResult(
    IReadOnlyDictionary<Guid, IReadOnlyList<DishIngredientMatchValueObject>> Profiles, bool IsPartial);

public interface IDishIngredientProfileReader
{
    Task<DishIngredientProfilesResult> ReadAsync(Guid familyId,
        IReadOnlyList<DishRecommendationEvidence> dishes, CancellationToken cancellationToken);
}

public sealed record GroceryOpportunity(string OfferId, string Ingredient, string StoreId, string StoreName,
    string ProductName, decimal Price, decimal? ReferencePrice, double DiscountFraction,
    string Currency, LocalDate? ValidFrom, LocalDate? ValidThrough, Instant ObservedAt, Instant UsableUntil,
    string? SourceUrl, string Relationship, IReadOnlyList<string> Conditions, string Freshness);
