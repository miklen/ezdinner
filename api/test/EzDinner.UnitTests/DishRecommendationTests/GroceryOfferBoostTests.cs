using EzDinner.Core.DomainServices.DishRecommendations;
using NodaTime;
using Xunit;

namespace EzDinner.UnitTests.DishRecommendationTests;

public class GroceryOfferBoostTests
{
    private static readonly Instant Now = Instant.FromUtc(2026, 10, 18, 12, 0);

    [Fact]
    public void ExactPrimaryTenPercentOfferAddsFivePointsAndInferredRelationshipStaysInferred()
    {
        var fact = new DishIngredientMatchValueObject("minced-beef", 1, 1, IngredientEvidenceKind.Fact, ["recipe"], "minced beef");
        var assessment = GroceryOfferBoostRule.Assess([fact], [Offer()], Now);
        Assert.Equal(5, assessment.Points);
        Assert.Equal(0.025, assessment.SemanticBonus);
        Assert.Equal(IngredientEvidenceKind.Fact, Assert.Single(assessment.Matches).EvidenceKind);
        Assert.Equal(4.5, GroceryOfferBoostRule.Assess([Ingredient("minced-beef", .9)], [Offer()], Now).Points);
    }

    [Theory]
    [InlineData("salt", "salt", 1)]
    [InlineData("minced-beef", "prepared-meatballs", 1)]
    [InlineData("minced-beef", "minced-pork", 1)]
    [InlineData("lentils", "minced-beef", 1)]
    [InlineData("chicken", "chicken", .59)]
    [InlineData("poultry", "chicken", .8)]
    public void UnusableIngredientRelationshipsHaveNoBoost(string ingredient, string product, double confidence)
        => Assert.Empty(GroceryOfferBoostRule.Assess([Ingredient(ingredient, confidence)], [Offer(product)], Now).Matches);

    [Fact]
    public void BroadMeatConfidenceIsReducedAndParentSpeciesAndStoresDoNotCompound()
    {
        var broad = GroceryOfferBoostRule.Assess([Ingredient("minced-meat")], [Offer()], Now);
        Assert.Equal(3.5, broad.Points, 8);
        Assert.Equal(.7, Assert.Single(broad.Matches).Confidence);
        var result = GroceryOfferBoostRule.Assess([Ingredient("minced-meat"), Ingredient("minced-beef")],
            [Offer(), Offer(store: "other"), Offer("minced-pork")], Now);
        Assert.Single(result.Matches);
        Assert.Equal(5, result.Points);
    }

    [Fact]
    public void OnlyTwoStrongestDistinctIngredientsContributeAndTotalIsCapped()
    {
        var result = GroceryOfferBoostRule.Assess([Ingredient("minced-beef"), Ingredient("lentils"), Ingredient("potatoes")],
            [Offer(price: 70), Offer("lentils", price: 70), Offer("potatoes")], Now);
        Assert.Equal(2, result.Matches.Count);
        Assert.Equal(10, result.Points);
        Assert.Equal(.05, result.SemanticBonus);
    }

    [Fact]
    public void ThresholdsAndUnknownOrConditionalSavingsAreConservative()
    {
        Assert.Equal(1.5, GroceryOfferBoostRule.Assess([Ingredient("minced-beef", .6)], [Offer(price: 95)], Now).Points, 8);
        Assert.Empty(GroceryOfferBoostRule.Assess([Ingredient("minced-beef")], [Offer(price: 95.01m)], Now).Matches);
        var unknown = new GroceryOfferValueObject("ref", "shop", "1", "Meat", "minced-beef", 90, null, null, Now, null, null);
        Assert.False(unknown.IsUsable(Now));
        var conditional = new GroceryOfferValueObject("ref", "shop", "1", "Meat", "minced-beef", 90, 100, null, Now, null, null, false, ["membership"]);
        Assert.False(conditional.IsUsable(Now));
    }

    [Fact]
    public void ExpiryUsesInclusiveDanishDateAndExclusiveEarlierAgeDeadline()
    {
        var offer = Offer(observed: Now - Duration.FromHours(47));
        Assert.Equal(Now + Duration.FromHours(1), offer.UsableUntil());
        Assert.True(offer.IsUsable(offer.UsableUntil() - Duration.FromTicks(1)));
        Assert.False(offer.IsUsable(offer.UsableUntil()));
        var dated = Offer(end: new LocalDate(2026, 10, 18));
        Assert.Equal(Instant.FromUtc(2026, 10, 18, 22, 0), dated.UsableUntil());
    }

    [Theory]
    [InlineData(2026, 3, 29, 22)]
    [InlineData(2026, 10, 25, 23)]
    public void UnknownExpiryLastsOnlyObservationDayIncludingDst(int year, int month, int day, int utcHour)
    {
        var observed = new LocalDate(year, month, day).AtStartOfDayInZone(DateTimeZoneProviders.Tzdb["Europe/Copenhagen"]).ToInstant();
        var offer = new GroceryOfferValueObject("ref", "shop", "1", "Meat", "minced-beef", 90, 100, null, observed, null, null);
        Assert.Null(offer.EndsOn);
        Assert.Equal(Instant.FromUtc(year, month, day, utcHour, 0), offer.UsableUntil());
        Assert.False(offer.IsUsable(offer.UsableUntil()));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-.1)]
    [InlineData(1.1)]
    public void IngredientWeightsMustBeFiniteFractions(double weight)
        => Assert.Throws<ArgumentException>(() => Ingredient("chicken", weight));

    [Fact]
    public void FactNeedsEvidenceAndCollectionsCannotBeMutatedByCaller()
    {
        Assert.Throws<ArgumentException>(() => new DishIngredientMatchValueObject("chicken", 1, 1, IngredientEvidenceKind.Fact, []));
        var references = new List<string> { "recipe" };
        var ingredient = new DishIngredientMatchValueObject("chicken", 1, 1, IngredientEvidenceKind.Fact, references, "chicken");
        references.Clear();
        Assert.Single(ingredient.SourceReferences);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(-1, 100)]
    [InlineData(100, 90)]
    [InlineData(100, 0)]
    public void InvalidOrInvertedPricesCannotBecomeOfferValues(decimal price, decimal reference)
        => Assert.Throws<ArgumentException>(() => new GroceryOfferValueObject("ref", "shop", "1", "Meat", "minced-beef", price, reference, null, Now, null, null));

    private static DishIngredientMatchValueObject Ingredient(string key, double confidence = 1)
        => new(key, 1, confidence, IngredientEvidenceKind.Inference, []);
    private static GroceryOfferValueObject Offer(string key = "minced-beef", string store = "shop", decimal price = 90,
        Instant? observed = null, LocalDate? end = null)
        => new($"{store}:{key}", store, key, key, key, price, 100, null, observed ?? Now, null, end ?? new LocalDate(2026, 10, 20));
}
