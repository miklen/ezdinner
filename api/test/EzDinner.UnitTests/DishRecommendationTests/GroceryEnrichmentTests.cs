#nullable enable
using System.Collections.Concurrent;
using System.Text.Json;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.FamilyAggregate;
using EzDinner.Core.Aggregates.WishlistAggregate;
using EzDinner.Core.DomainServices.DishRecommendations;
using EzDinner.Query.Core.DishRecommendationQueries;
using EzDinner.Infrastructure.DishRecommendations;
using EzDinner.Infrastructure.GroceryRecommendations;
using Moq;
using EzDinner.Authorization.Core;
using EzDinner.Functions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using NodaTime;
using NodaTime.Testing;
using Xunit;

namespace EzDinner.UnitTests.DishRecommendationTests;

public class GroceryEnrichmentTests
{
    [Fact]
    public async Task EndpointSerializesNamedStoreResultsEvenWithoutOffersOrIngredientInference()
    {
        SetCatalog(Dish.CreateNew(family.Id, "Chicken curry"));
        offers.Setup(reader => reader.ReadAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroceryOffersResult([], new(GroceryCoverageStatus.Unavailable, clock.GetCurrentInstant(), ["rema1000"], ["source-unavailable"])
            { Stores = [new("rema1000", "REMA 1000", GroceryCoverageStatus.Unavailable, 0)] }, new Dictionary<string, string>()));
        var authorization = new Mock<IAuthzService>();
        authorization.Setup(service => service.Authorize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), Actions.Read)).Returns(true);
        var endpoint = new DishRecommendationsFunction(Query(), authorization.Object, new());
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "test"));
        http.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""
            {"selectedMonday":"2026-10-19","locale":"en","mode":"automatic","turns":[],"constraints":[],"excludedDishIds":[]}
            """));
        var result = Assert.IsType<OkObjectResult>(await endpoint.Run(http.Request, family.Id.ToString()));
        using var response = JsonDocument.Parse(JsonSerializer.Serialize(result.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var store = response.RootElement.GetProperty("groceryCoverage").GetProperty("stores")[0];
        Assert.Equal("rema1000", store.GetProperty("storeId").GetString());
        Assert.Equal("REMA 1000", store.GetProperty("storeName").GetString());
        Assert.Equal("Unavailable", store.GetProperty("status").GetString());
        Assert.Equal(0, store.GetProperty("usableOfferCount").GetInt32());
        profiles.Verify(reader => reader.ReadAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<DishRecommendationEvidence>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private readonly Family family = Family.CreateNew(Guid.NewGuid(), "Test");
    private readonly FakeClock clock = new(Instant.FromUtc(2026, 10, 18, 12, 0));
    private readonly Mock<IDishRepository> dishes = new();
    private readonly Mock<IDinnerRepository> dinners = new();
    private readonly Mock<IWishlistRepository> wishes = new();
    private readonly Mock<IFamilyRepository> families = new();
    private readonly Mock<IGroceryOfferReader> offers = new();
    private readonly Mock<IDishIngredientProfileReader> profiles = new();
    private readonly Mock<IDishRecommendationLlmClient> provider = new();

    public GroceryEnrichmentTests()
    {
        family.ReplaceGroceryStores(["rema1000"], ["rema1000"]);
        families.Setup(repository => repository.GetFamily(family.Id)).ReturnsAsync(family);
        dinners.Setup(repository => repository.GetAsync(family.Id, LocalDate.MinIsoValue, LocalDate.MaxIsoValue)).Returns(EmptyDinners());
        wishes.Setup(repository => repository.GetActiveAsync(family.Id)).ReturnsAsync([]);
        SetOffers([new GroceryOfferValueObject("offer", "rema1000", "chicken", "Chicken", "chicken", 90, 100, null,
            clock.GetCurrentInstant(), null, new LocalDate(2026, 10, 21))]);
        profiles.Setup(reader => reader.ReadAsync(family.Id, It.IsAny<IReadOnlyList<DishRecommendationEvidence>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, IReadOnlyList<DishRecommendationEvidence> evidence, CancellationToken _) =>
                new DishIngredientProfilesResult(evidence.ToDictionary(dish => dish.DishId,
                    dish => (IReadOnlyList<DishIngredientMatchValueObject>)[new(dish.Name.Contains("Chicken") ? "chicken" : "lentils", 1, 1, IngredientEvidenceKind.Inference, [])]), false));
    }

    [Fact]
    public async Task Source_acquisition_has_its_own_budget_before_ingredient_inference()
    {
        var chicken = Evidence("Chicken");
        var available = new GroceryOffersResult(
            [new("offer", "rema1000", "chicken", "Chicken", "chicken", 90, 100, null,
                clock.GetCurrentInstant(), null, new LocalDate(2026, 10, 21))],
            new(GroceryCoverageStatus.Fresh, clock.GetCurrentInstant(), ["rema1000"], []),
            new Dictionary<string, string> { ["rema1000"] = "REMA 1000" });
        offers.Setup(reader => reader.ReadAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Returns(async (IReadOnlyList<string> _, CancellationToken token) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(31), token);
                return available;
            });
        var result = await new GroceryRecommendationEnrichment(offers.Object, profiles.Object, clock)
            .EnrichAsync(family.Id, ["rema1000"], [chicken], default);
        Assert.Equal(GroceryCoverageStatus.Fresh, result.Coverage.Status);
        Assert.Single(Assert.Single(result.Dishes).GroceryOffers);
    }

    [Fact]
    public async Task ColdProfilesTakingLongerThanTenSecondsStillEnrichRecommendations()
    {
        var chicken = Evidence("Chicken");
        profiles.Setup(reader => reader.ReadAsync(family.Id, It.IsAny<IReadOnlyList<DishRecommendationEvidence>>(), It.IsAny<CancellationToken>()))
            .Returns(async (Guid _, IReadOnlyList<DishRecommendationEvidence> _, CancellationToken token) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(11), token);
                return new DishIngredientProfilesResult(new Dictionary<Guid, IReadOnlyList<DishIngredientMatchValueObject>>
                { [chicken.DishId] = [new("chicken", 1, 1, IngredientEvidenceKind.Inference, [])] }, false);
            });
        var result = await new GroceryRecommendationEnrichment(offers.Object, profiles.Object, clock)
            .EnrichAsync(family.Id, ["rema1000"], [chicken], default);
        Assert.Equal(GroceryCoverageStatus.Fresh, result.Coverage.Status);
        Assert.Single(Assert.Single(result.Dishes).GroceryOffers);
    }

    [Fact]
    public async Task ExistingExclusionNameAndRoleFiltersRunBeforeProfilesAreLoaded()
    {
        var selected = Dish.CreateNew(family.Id, "Chicken curry");
        var excluded = Dish.CreateNew(family.Id, "Chicken roast");
        var side = Dish.CreateNew(family.Id, "Chicken side");
        side.UpdateMetadata(DishMetadataValueObject.FromUserEdit(side.Metadata, [DishRole.Side], null, null, null));
        SetCatalog(selected, excluded, side, Dish.CreateNew(family.Id, "Lentil soup"));
        var result = await Query().RecommendAsync(family.Id, Request() with
        { NameFilter = "Chicken", ExcludedDishIds = new HashSet<Guid> { excluded.Id } }, default);
        Assert.Equal(selected.Id, Assert.Single(result.Dishes).DishId);
        profiles.Verify(reader => reader.ReadAsync(family.Id,
            It.Is<IReadOnlyList<DishRecommendationEvidence>>(evidence => evidence.Count == 1 && evidence[0].DishId == selected.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EmptySelectionOrNoEligibleCandidatesSkipBothOptionalReaders(bool emptySelection)
    {
        SetCatalog(Dish.CreateNew(family.Id, "Chicken curry"));
        if (emptySelection) family.ReplaceGroceryStores([], ["rema1000"]);
        await Query().RecommendAsync(family.Id, Request() with { NameFilter = emptySelection ? "" : "absent" }, default);
        offers.Verify(reader => reader.ReadAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        profiles.Verify(reader => reader.ReadAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<DishRecommendationEvidence>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NoQualifyingOffersSkipsInferenceWithoutChangingOrdinaryRecommendations()
    {
        var dish = Dish.CreateNew(family.Id, "Chicken curry");
        SetCatalog(dish);
        SetOffers([]);
        var result = await Query().RecommendAsync(family.Id, Request(), default);
        Assert.Equal(dish.Id, Assert.Single(result.Dishes).DishId);
        Assert.Empty(result.Dishes[0].GroceryOffers);
        profiles.Verify(reader => reader.ReadAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<DishRecommendationEvidence>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(true, GroceryCoverageStatus.Unavailable)]
    [InlineData(false, GroceryCoverageStatus.Partial)]
    public async Task ReaderFailuresPreserveOrdinaryEligibilityWithHonestCoverage(bool sourceFailure, GroceryCoverageStatus expected)
    {
        var dish = Dish.CreateNew(family.Id, "Chicken curry");
        SetCatalog(dish);
        if (sourceFailure) offers.Setup(reader => reader.ReadAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException());
        if (!sourceFailure) profiles.Setup(reader => reader.ReadAsync(family.Id, It.IsAny<IReadOnlyList<DishRecommendationEvidence>>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException());
        var result = await Query().RecommendAsync(family.Id, Request(), default);
        Assert.Equal(dish.Id, Assert.Single(result.Dishes).DishId);
        Assert.Equal(expected, result.GroceryCoverage!.Status);
    }

    [Fact]
    public async Task FullCatalogOfferPointsApplyBeforeAutomaticPageLimit()
    {
        var chicken = Dish.CreateNew(family.Id, "Z Chicken");
        SetCatalog(Enumerable.Range(0, 9).Select(index => Dish.CreateNew(family.Id, $"A lentils {index}")).Append(chicken).ToArray());
        var result = await Query(new() { MaximumResults = 2 }).RecommendAsync(family.Id, Request(), default);
        Assert.Equal(chicken.Id, result.Dishes[0].DishId);
        var offer = Assert.Single(result.Dishes[0].GroceryOffers);
        Assert.Equal(new LocalDate(2026, 10, 21), offer.ValidThrough);
        Assert.Equal(clock.GetCurrentInstant() + Duration.FromHours(48), offer.UsableUntil);
        Assert.Equal("Inference", offer.Relationship);
    }

    [Fact]
    public async Task EverySemanticBatchAndFinalistReceiveServerOfferEvidenceBeforeSelection()
    {
        var catalog = Enumerable.Range(0, 4).Select(index => Dish.CreateNew(family.Id, $"Chicken {index}")).ToArray();
        SetCatalog(catalog);
        var contexts = new ConcurrentBag<DishRecommendationContext>();
        provider.Setup(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DishRecommendationRequest _, DishRecommendationContext context, CancellationToken _) =>
            {
                contexts.Add(context);
                return new RecommendationProviderResult(RecommendationOutcome.Matches,
                    [Match(context.Dishes[0].DishId, .8)], "Chicken", null);
            });
        await Query(new() { MaximumCandidates = 2 }).RecommendAsync(family.Id, Request() with { Mode = RecommendationMode.Request, Turns = ["Chicken"] }, default);
        Assert.Equal(3, contexts.Count);
        Assert.All(contexts.SelectMany(context => context.Dishes), dish =>
        {
            Assert.Equal(5, dish.GroceryOfferPoints);
            Assert.Equal(.025, dish.SemanticOfferBonus);
            Assert.Single(dish.GroceryOffers);
            Assert.Contains(dish.Sources, source => source.Reference == $"{dish.DishId}:grocery:offer");
        });
    }

    [Fact]
    public async Task SemanticBonusIsAddedOnceAndCannotReintroduceProviderRejectedDish()
    {
        var chicken = Dish.CreateNew(family.Id, "Chicken");
        var lentils = Dish.CreateNew(family.Id, "Lentils");
        var rejected = Dish.CreateNew(family.Id, "Chicken rejected");
        SetCatalog(chicken, lentils, rejected);
        provider.Setup(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecommendationProviderResult(RecommendationOutcome.Matches, [Match(chicken.Id, .80), Match(lentils.Id, .84)], "Fit", null));
        var result = await Query().RecommendAsync(family.Id, Request() with { Mode = RecommendationMode.Request, Turns = ["Suitable dishes"] }, default);
        Assert.Equal([lentils.Id, chicken.Id], result.Dishes.Select(dish => dish.DishId));
    }

    [Fact]
    public async Task ProviderCannotInventGroceryReferences()
    {
        var chicken = Dish.CreateNew(family.Id, "Chicken");
        SetCatalog(chicken);
        var match = Match(chicken.Id, 1) with { Explanations = [new("Invented offer", RecommendationEvidenceKind.Fact, [$"{chicken.Id}:grocery:invented"])] };
        provider.Setup(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecommendationProviderResult(RecommendationOutcome.Matches, [match], "Fit", null));
        await Assert.ThrowsAsync<DishRecommendationProviderException>(() => Query().RecommendAsync(family.Id,
            Request() with { Mode = RecommendationMode.Request, Turns = ["Chicken"] }, default));
    }

    [Fact]
    public async Task OptionalDeadlineRetainsCachedOpportunityWhileColdInferenceStalls()
    {
        var cached = Evidence("Chicken cached");
        var cold = Evidence("Chicken cold");
        var completion = new StallingCompletion(cached.DishId);
        var reader = new CachedDishIngredientProfileReader(new ProfileCache(), new(completion));
        Assert.False((await reader.ReadAsync(family.Id, [cached], default)).IsPartial);
        completion.Stall = true;
        var enrichment = new GroceryRecommendationEnrichment(offers.Object, reader, clock);

        var result = await enrichment.EnrichAsync(family.Id, ["rema1000"], [cached, cold], default)
            .WaitAsync(TimeSpan.FromSeconds(35));

        Assert.Equal(GroceryCoverageStatus.Partial, result.Coverage.Status);
        Assert.Contains("profiles-partial", result.Coverage.Limitations);
        Assert.Equal(2, result.Dishes.Count);
        Assert.Equal(5, result.Dishes.Single(dish => dish.DishId == cached.DishId).GroceryOfferPoints);
        Assert.Single(result.Dishes.Single(dish => dish.DishId == cached.DishId).GroceryOffers);
        Assert.Empty(result.Dishes.Single(dish => dish.DishId == cold.DishId).GroceryOffers);
        Assert.Equal(2, completion.Calls);
    }

    [Fact]
    public async Task MixedStoreCoverageDoesNotLabelFreshRetailerOpportunityStale()
    {
        var chicken = Evidence("Chicken");
        var lentils = Evidence("Lentils");
        var now = clock.GetCurrentInstant();
        GroceryOfferValueObject[] observations =
        [
            new("stale-offer", "rema1000", "chicken", "Chicken", "chicken", 90, 100, null, now, null, null),
            new("fresh-offer", "nemlig", "lentils", "Lentils", "lentils", 90, 100, null, now, null, null)
        ];
        offers.Setup(reader => reader.ReadAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroceryOffersResult(observations,
                new(GroceryCoverageStatus.Partial, now, ["nemlig", "rema1000"], ["source-stale"]),
                new Dictionary<string, string> { ["rema1000"] = "REMA 1000", ["nemlig"] = "Nemlig" })
            {
                StoreFreshness = new Dictionary<string, GroceryCoverageStatus>
                    { ["rema1000"] = GroceryCoverageStatus.Stale, ["nemlig"] = GroceryCoverageStatus.Fresh }
            });
        var result = await new GroceryRecommendationEnrichment(offers.Object, profiles.Object, clock)
            .EnrichAsync(family.Id, ["nemlig", "rema1000"], [chicken, lentils], default);

        Assert.Equal(GroceryCoverageStatus.Partial, result.Coverage.Status);
        Assert.Equal("Stale", Assert.Single(result.Dishes.Single(dish => dish.DishId == chicken.DishId).GroceryOffers).Freshness);
        Assert.Equal("Fresh", Assert.Single(result.Dishes.Single(dish => dish.DishId == lentils.DishId).GroceryOffers).Freshness);
    }

    private DishRecommendationQuery Query(DishRecommendationLimits? limits = null)
    {
        var rule = new ResurfacingRule();
        return new(new(dishes.Object, dinners.Object, wishes.Object, clock, families.Object), new(rule), rule,
            provider.Object, limits ?? new(), new(offers.Object, profiles.Object, clock));
    }
    private void SetCatalog(params Dish[] catalog) => dishes.Setup(repository => repository.GetDishesAsync(family.Id, false)).ReturnsAsync(catalog);
    private void SetOffers(IReadOnlyList<GroceryOfferValueObject> values) => offers
        .Setup(reader => reader.ReadAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new GroceryOffersResult(values, new(GroceryCoverageStatus.Fresh, clock.GetCurrentInstant(), ["rema1000"], []), new Dictionary<string, string> { ["rema1000"] = "REMA 1000" }));
    private static DishRecommendationRequest Request() => new(new LocalDate(2026, 10, 19), null, RecommendationMode.Automatic, "en", [], [], new HashSet<Guid>());
    private static RecommendationProviderMatch Match(Guid id, double suitability) => new(id, suitability,
        [new("Possible fit", RecommendationEvidenceKind.Inference, [$"{id}:title"])], []);
    private static async IAsyncEnumerable<Dinner> EmptyDinners() { await Task.CompletedTask; yield break; }

    private static DishRecommendationEvidence Evidence(string name)
    {
        var id = Guid.NewGuid();
        return new(id, name, [], [new($"{id}:title", name)],
            new(id, name, null, [], false, 0, new([], new LocalDate(2026, 10, 18)), []));
    }

    private sealed class ProfileCache : IGroceryRecommendationCache
    {
        private readonly ConcurrentDictionary<string, string> entries = new();
        public Task<string?> ReadProfileAsync(string scope, string key, CancellationToken cancellationToken)
            => Task.FromResult(entries.GetValueOrDefault(scope + key));
        public Task WriteProfileAsync(string scope, string key, string payload, int ttlSeconds, CancellationToken cancellationToken)
        {
            entries[scope + key] = payload;
            return Task.CompletedTask;
        }
    }

    private sealed class StallingCompletion(Guid cachedDishId) : IDishRecommendationCompletionClient
    {
        public bool Stall { get; set; }
        public int Calls { get; private set; }
        public async Task<string> CompleteAsync(string systemPrompt, string evidenceJson, CancellationToken cancellationToken)
        {
            Calls++;
            if (Stall) await Task.Delay(Timeout.Infinite, cancellationToken);
            return JsonSerializer.Serialize(new { profiles = new[] { new { dishId = cachedDishId, ingredients = new[]
            { new { key = "chicken", importance = 1d, confidence = 1d, evidenceKind = "Inference", sourceReferences = Array.Empty<string>(), supportingExcerpt = (string?)null, role = "Primary" } } } } });
        }
    }
}
