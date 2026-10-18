using NodaTime;

namespace EzDinner.Core.DomainServices.DishRecommendations;

public sealed record GroceryOfferValueObject
{
    public string Reference { get; }
    public string StoreId { get; }
    public string ProductId { get; }
    public string ProductName { get; }
    public string IngredientKey { get; }
    public decimal Price { get; }
    public decimal? ReferencePrice { get; }
    public double? DiscountFraction { get; }
    public Instant ObservedAt { get; }
    public LocalDate? StartsOn { get; }
    public LocalDate? EndsOn { get; }
    public bool ConditionsEligible { get; }
    public IReadOnlyList<string> Conditions { get; }
    public string? SourceUrl { get; }

    public GroceryOfferValueObject(string reference, string storeId, string productId, string productName,
        string ingredientKey, decimal price, decimal? referencePrice, double? verifiedDiscountFraction,
        Instant observedAt, LocalDate? startsOn, LocalDate? endsOn, bool conditionsEligible = true,
        IReadOnlyList<string>? conditions = null, string? sourceUrl = null)
    {
        if (new[] { reference, storeId, productId, productName }.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("INVALID_OFFER_IDENTITY");
        if (!IngredientVocabularyFactory.IsKnown(ingredientKey)) throw new ArgumentException("UNKNOWN_INGREDIENT");
        if (price <= 0 || referencePrice <= 0 || (referencePrice.HasValue && referencePrice.Value < price) ||
            (startsOn.HasValue && endsOn.HasValue && startsOn > endsOn))
            throw new ArgumentException("INVALID_OFFER_PRICE_OR_DATES");
        if (verifiedDiscountFraction is double fraction && (!double.IsFinite(fraction) || fraction is < 0 or > 1))
            throw new ArgumentException("INVALID_DISCOUNT");
        if (sourceUrl is not null && (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("INVALID_OFFER_SOURCE_URL");
        Reference = reference; StoreId = storeId; ProductId = productId; ProductName = productName;
        IngredientKey = ingredientKey; Price = price; ReferencePrice = referencePrice;
        DiscountFraction = referencePrice.HasValue ? (double)((referencePrice.Value - price) / referencePrice.Value) : verifiedDiscountFraction;
        ObservedAt = observedAt; StartsOn = startsOn; EndsOn = endsOn; ConditionsEligible = conditionsEligible;
        Conditions = Array.AsReadOnly((conditions ?? []).ToArray()); SourceUrl = sourceUrl;
    }

    public Instant UsableUntil()
    {
        var zone = DateTimeZoneProviders.Tzdb["Europe/Copenhagen"];
        var finalDate = EndsOn ?? ObservedAt.InZone(zone).Date;
        var calendarDeadline = finalDate.PlusDays(1).AtStartOfDayInZone(zone).ToInstant();
        var ageDeadline = ObservedAt + Duration.FromHours(48);
        return calendarDeadline < ageDeadline ? calendarDeadline : ageDeadline;
    }

    public bool IsUsable(Instant now)
    {
        var today = now.InZone(DateTimeZoneProviders.Tzdb["Europe/Copenhagen"]).Date;
        return ConditionsEligible && ObservedAt <= now && now < UsableUntil() &&
            (!StartsOn.HasValue || StartsOn.Value <= today) && DiscountFraction >= 0.05;
    }
}
