using NodaTime;

namespace EzDinner.Core.DomainServices.DishRecommendations;

public sealed record DishGroceryOfferMatchValueObject(string IngredientKey, GroceryOfferValueObject Offer,
    double Confidence, IngredientEvidenceKind EvidenceKind, double Points, Instant UsableUntil);

public sealed record GroceryOfferAssessmentValueObject
{
    public IReadOnlyList<DishGroceryOfferMatchValueObject> Matches { get; }
    public double Points { get; }
    public double SemanticBonus => 0.05 * Points / 10;
    public static GroceryOfferAssessmentValueObject Empty { get; } = new([]);

    public GroceryOfferAssessmentValueObject(IReadOnlyList<DishGroceryOfferMatchValueObject> matches)
    {
        Matches = Array.AsReadOnly(matches.ToArray());
        Points = Math.Min(10, Matches.Sum(match => match.Points));
    }
}

public static class GroceryOfferBoostRule
{
    public static GroceryOfferAssessmentValueObject Assess(IReadOnlyList<DishIngredientMatchValueObject> ingredients,
        IReadOnlyList<GroceryOfferValueObject> offers, Instant now)
    {
        var matches = ingredients.Where(ingredient => ingredient.Role == IngredientRole.Primary && ingredient.Importance > 0)
            .SelectMany(ingredient => offers.Where(offer => offer.IsUsable(now))
                .Select(offer => Match(ingredient, offer)))
            .Where(match => match is not null).Select(match => match!)
            .OrderByDescending(match => match.Points)
            .ThenBy(match => match.Offer.StoreId, StringComparer.Ordinal)
            .ThenBy(match => match.Offer.ProductId, StringComparer.Ordinal)
            .ThenBy(match => match.IngredientKey, StringComparer.Ordinal)
            .GroupBy(match => IngredientVocabularyFactory.OverlapGroup(match.IngredientKey))
            .Select(group => group.First()).Take(2).ToArray();
        return new(matches);
    }

    private static DishGroceryOfferMatchValueObject? Match(DishIngredientMatchValueObject ingredient, GroceryOfferValueObject offer)
    {
        var specificity = IngredientVocabularyFactory.Compatibility(ingredient.Key, offer.IngredientKey);
        var confidence = ingredient.Confidence * specificity;
        if (confidence < 0.6) return null;
        var points = 10 * confidence * ingredient.Importance * Math.Min(offer.DiscountFraction!.Value / 0.2, 1);
        var kind = specificity < 1 ? IngredientEvidenceKind.Inference : ingredient.EvidenceKind;
        return new(ingredient.Key, offer, confidence, kind, points, offer.UsableUntil());
    }
}
