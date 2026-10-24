using EzDinner.Core.DomainServices.DishRecommendations;
using NodaTime;

namespace EzDinner.Query.Core.DishRecommendationQueries;

public sealed record GroceryEnrichmentResult(IReadOnlyList<DishRecommendationEvidence> Dishes, GroceryCoverage Coverage);

public sealed class GroceryRecommendationEnrichment(IGroceryOfferReader offers,
    IDishIngredientProfileReader profiles, IClock clock)
{
    public async Task<GroceryEnrichmentResult> EnrichAsync(Guid familyId, IReadOnlyList<string> storeIds,
        IReadOnlyList<DishRecommendationEvidence> dishes, CancellationToken cancellationToken)
    {
        var ids = storeIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var now = clock.GetCurrentInstant();
        if (ids.Length == 0 || dishes.Count == 0)
            return new(dishes, new(ids.Length == 0 ? GroceryCoverageStatus.Disabled : GroceryCoverageStatus.Unknown, now, ids, []));
        GroceryOffersResult observed;
        try { observed = await offers.ReadAsync(ids, cancellationToken).WaitAsync(cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        { return new(dishes, new(GroceryCoverageStatus.Unavailable, now, ids, ["source-unavailable"])); }
        var qualifying = observed.Offers.Where(offer => ids.Contains(offer.StoreId, StringComparer.Ordinal) && offer.IsUsable(clock.GetCurrentInstant())).ToArray();
        if (qualifying.Length == 0) return new(dishes, observed.Coverage with { StoreIds = ids });
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        DishIngredientProfilesResult ingredients;
        try { ingredients = await profiles.ReadAsync(familyId, dishes, deadline.Token).WaitAsync(cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { ingredients = new(new Dictionary<Guid, IReadOnlyList<DishIngredientMatchValueObject>>(), true); }
        var coverage = ingredients.IsPartial ? observed.Coverage with
        {
            Status = GroceryCoverageStatus.Partial,
            Limitations = observed.Coverage.Limitations.Append("profiles-partial").Distinct().ToArray()
        } : observed.Coverage;
        var enriched = dishes.Select(dish =>
        {
            if (!ingredients.Profiles.TryGetValue(dish.DishId, out var profile)) return dish;
            var assessment = GroceryOfferBoostRule.Assess(profile, qualifying, clock.GetCurrentInstant());
            var opportunities = assessment.Matches.Select(match => Opportunity(match, observed)).ToArray();
            var sources = opportunities.Select(offer => new DishRecommendationSource(
                $"{dish.DishId}:grocery:{offer.OfferId}", $"{offer.StoreName}: {offer.ProductName}; relationship={offer.Relationship}; ingredient={offer.Ingredient}"));
            return dish with { GroceryOfferPoints = assessment.Points, GroceryOffers = opportunities,
                Sources = dish.Sources.Concat(sources).ToArray() };
        }).ToArray();
        return new(enriched, coverage with { StoreIds = ids });
    }

    private static GroceryOpportunity Opportunity(DishGroceryOfferMatchValueObject match,
        GroceryOffersResult observed)
    {
        var offer = match.Offer;
        var name = observed.StoreNames.TryGetValue(offer.StoreId, out var storeName) ? storeName : offer.StoreId;
        return new(offer.Reference, match.IngredientKey, offer.StoreId, name, offer.ProductName, offer.Price,
            offer.ReferencePrice, offer.DiscountFraction!.Value, "DKK", offer.StartsOn, offer.EndsOn,
            offer.ObservedAt, match.UsableUntil, offer.SourceUrl, match.EvidenceKind.ToString(), offer.Conditions,
            observed.StoreFreshness?.GetValueOrDefault(offer.StoreId) == GroceryCoverageStatus.Stale ? "Stale" : "Fresh");
    }
}
