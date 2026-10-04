using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.WishlistAggregate;
using EzDinner.Core.DomainServices.DishRecommendations;
using EzDinner.Query.Core.DishRecommendationQueries;
using Moq;
using NodaTime;
using NodaTime.Testing;
using Xunit;

namespace EzDinner.UnitTests.DishRecommendationTests;

public class RecommendationQueryTests
{
    private readonly Guid family = Guid.NewGuid();
    private readonly LocalDate monday = new(2026, 10, 5);
    private readonly FakeClock clock = new(Instant.FromUtc(2026, 10, 3, 12, 0));
    private readonly Mock<IDishRepository> dishes = new();
    private readonly Mock<IDinnerRepository> dinners = new();
    private readonly Mock<IWishlistRepository> wishes = new();
    private readonly Mock<IDishRecommendationLlmClient> provider = new();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ContextContainsCanonicalNotesAndOptionalRecipeWithoutWrites(bool hasRecipe)
    {
        var dish = Dish.CreateNew(family, "Potatoes");
        dish.SetNotes("Use frozen potatoes to skip peeling.");
        if (hasRecipe)
        {
            dish.SetUrl("https://example.com/potatoes");
            dish.SetRecipeSnapshot(new("Bake for 30 minutes.", "https://example.com/potatoes", DateTimeOffset.UtcNow, new string('a', 64)));
        }
        SetCatalog(dish);
        var result = await Assembler().AssembleAsync(family, monday, default);
        var evidence = Assert.Single(result.Dishes);
        Assert.Equal(dish.Id, evidence.DishId);
        Assert.Contains(new($"{dish.Id}:notes", dish.Notes), evidence.Sources);
        Assert.Equal(hasRecipe, evidence.Sources.Any(source => source.Reference == $"{dish.Id}:recipe"));
        dinners.Verify(repository => repository.SaveAsync(It.IsAny<Dinner>()), Times.Never);
    }

    [Fact]
    public async Task ContextIncludesWindowOptOutsAndActiveWishes()
    {
        var dish = Dish.CreateNew(family, "Soup");
        var dinner = Dinner.CreateNew(family, monday);
        dinner.SetOptOut("Eating out");
        SetCatalog(dish);
        dinners.Setup(repository => repository.GetAsync(family, LocalDate.MinIsoValue, LocalDate.MaxIsoValue)).Returns(Stream([dinner]));
        var wish = WishlistItem.CreateNew(family, dish.Id, dish.Name, Guid.NewGuid(), clock);
        wishes.Setup(repository => repository.GetActiveAsync(family)).ReturnsAsync([wish]);
        var context = await Assembler().AssembleAsync(family, monday, default);
        Assert.True(Assert.Single(context.Dishes).Candidate.IsWished);
        Assert.Equal(1, Assert.Single(context.Dishes).Candidate.WishVotes);
        Assert.Equal("Eating out", Assert.Single(context.Dinners).OptOutReason);
    }

    [Fact]
    public async Task SemanticSuitabilityRanksBeforeHistoricalScore()
    {
        var favourite = Dish.CreateNew(family, "Favourite");
        favourite.SetRating(Guid.Empty, Guid.Empty, true, 5);
        var fitting = Dish.CreateNew(family, "Fitting");
        SetCatalog(favourite, fitting);
        provider.Setup(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecommendationProviderResult(RecommendationOutcome.Matches,
                [Match(favourite, 0.6), Match(fitting, 1)], "Potato pairing", null));
        var result = await Query().RecommendAsync(family, Request(), default);
        Assert.Equal([fitting.Id, favourite.Id], result.Dishes.Select(dish => dish.DishId));
    }

    [Fact]
    public async Task SingleDishEvidenceIsNotDiscardedWhenItExceedsBatchBudget()
    {
        var dish = Dish.CreateNew(family, "Soup");
        dish.SetNotes(new string('x', 2_000));
        SetCatalog(dish);
        provider.Setup(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecommendationProviderResult(RecommendationOutcome.Matches, [Match(dish, 1)], "Soup", null));
        var result = await Query(new() { MaximumEvidenceCharacters = 1 }).RecommendAsync(family, Request(), default);
        Assert.Equal(dish.Id, Assert.Single(result.Dishes).DishId);
        provider.Verify(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(),
            It.Is<DishRecommendationContext>(context => context.Dishes[0].Sources.Any(source => source.Text.Length == 2_000)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(500, 100_000)]
    [InlineData(12, 3_000)]
    public async Task SemanticSearchEvaluatesEntireCatalogAndRanksMatchesAcrossBatches(int count, int evidenceBudget)
    {
        var catalog = Enumerable.Range(0, count).Select(index => Dish.CreateNew(family, $"Dish {index}")).ToArray();
        SetCatalog(catalog);
        var evaluated = new HashSet<Guid>();
        provider.Setup(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()))
            .Returns((DishRecommendationRequest intent, DishRecommendationContext context, CancellationToken _) => {
                Assert.Equal(["Works with potatoes"], intent.Turns);
                Assert.InRange(context.Dishes.Count, 1, 300);
                Assert.InRange(DishRecommendationEvidenceFactory.Serialize(intent, context).Length, 1, evidenceBudget);
                foreach (var dish in context.Dishes) evaluated.Add(dish.DishId);
                var matches = context.Dishes.Where(dish => dish.DishId == catalog[0].Id || dish.DishId == catalog[^1].Id)
                    .Select(dish => Match(dish.DishId == catalog[0].Id ? catalog[0] : catalog[^1], dish.DishId == catalog[0].Id ? 0.6 : 1)).ToArray();
                return Task.FromResult(new RecommendationProviderResult(matches.Length > 0 ? RecommendationOutcome.Matches : RecommendationOutcome.NoMatch, matches, "Potato pairing", null));
            });
        var result = await Query(new() { MaximumEvidenceCharacters = evidenceBudget }).RecommendAsync(family, Request(), default);
        Assert.Equal(count, evaluated.Count);
        Assert.Equal([catalog[^1].Id, catalog[0].Id], result.Dishes.Select(dish => dish.DishId));
    }

    [Fact]
    public async Task FinalistsFromDifferentBatchesAreComparedTogetherBeforeFinalRanking()
    {
        var catalog = Enumerable.Range(0, 5).Select(index => Dish.CreateNew(family, $"Dish {index}")).ToArray();
        SetCatalog(catalog);
        provider.Setup(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()))
            .Returns((DishRecommendationRequest _, DishRecommendationContext context, CancellationToken _) => {
                var first = context.Dishes.Any(dish => dish.DishId == catalog[0].Id);
                var last = context.Dishes.Any(dish => dish.DishId == catalog[^1].Id);
                if (first && last)
                    return Task.FromResult(new RecommendationProviderResult(RecommendationOutcome.Matches,
                        [Match(catalog[0], 0.7), Match(catalog[^1], 1)], "Best pairing across catalog", null));
                return Task.FromResult(new RecommendationProviderResult(RecommendationOutcome.Matches,
                    [first ? Match(catalog[0], 1) : Match(catalog[^1], 0.8)], "Batch pairing", null));
            });
        var result = await Query(new() { MaximumCandidates = 3 }).RecommendAsync(family, Request(), default);
        Assert.Equal([catalog[^1].Id, catalog[0].Id], result.Dishes.Select(dish => dish.DishId));
        Assert.Equal("Best pairing across catalog", result.ContextSummary);
    }

    [Fact]
    public async Task RoleNarrowingReducesCandidateBudgetBeforeProviderAccess()
    {
        var side = Dish.CreateNew(family, "Potatoes");
        side.UpdateMetadata(DishMetadataValueObject.FromUserEdit(side.Metadata, [DishRole.Side], null, null, null));
        SetCatalog(Dish.CreateNew(family, "Soup"), side);
        provider.Setup(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecommendationProviderResult(RecommendationOutcome.Matches, [Match(side, 1)], "Sides", null));
        var result = await Query(new() { MaximumCandidates = 1 }).RecommendAsync(family, Request() with { Role = DishRole.Side }, default);
        Assert.Equal(side.Id, Assert.Single(result.Dishes).DishId);
        provider.Verify(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(),
            It.Is<DishRecommendationContext>(context => context.Dishes.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BatchProviderFailureCannotReturnPartialCatalogRecommendations()
    {
        var first = Dish.CreateNew(family, "Soup");
        var second = Dish.CreateNew(family, "Roast");
        SetCatalog(first, second);
        provider.Setup(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()))
            .Returns((DishRecommendationRequest _, DishRecommendationContext context, CancellationToken _) =>
                context.Dishes.Any(dish => dish.DishId == second.Id)
                    ? Task.FromException<RecommendationProviderResult>(new DishRecommendationProviderException("PROVIDER_UNAVAILABLE"))
                    : Task.FromResult(new RecommendationProviderResult(RecommendationOutcome.Matches, [Match(first, 1)], "Soup", null)));
        var exception = await Assert.ThrowsAsync<DishRecommendationProviderException>(() =>
            Query(new() { MaximumCandidates = 1 }).RecommendAsync(family, Request(), default));
        Assert.Equal("PROVIDER_UNAVAILABLE", exception.Message);
    }

    [Fact]
    public async Task MoreEvaluatesAllRemainingBatchesWithoutRecyclingExcludedDishes()
    {
        var excluded = Dish.CreateNew(family, "Already shown");
        var noMatch = Dish.CreateNew(family, "Unknown");
        var fitting = Dish.CreateNew(family, "Roast");
        SetCatalog(excluded, noMatch, fitting);
        provider.Setup(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()))
            .Returns((DishRecommendationRequest intent, DishRecommendationContext context, CancellationToken _) => {
                Assert.DoesNotContain(context.Dishes, dish => dish.DishId == excluded.Id);
                Assert.Equal(["No fish"], intent.Constraints);
                return Task.FromResult(context.Dishes.Any(dish => dish.DishId == fitting.Id)
                    ? new RecommendationProviderResult(RecommendationOutcome.Matches, [Match(fitting, 1)], "No fish", null)
                    : new RecommendationProviderResult(RecommendationOutcome.NoMatch, [], "No fish", null));
            });
        var result = await Query(new() { MaximumCandidates = 1 }).RecommendAsync(family,
            Request() with { Mode = RecommendationMode.More, Constraints = ["No fish"], ExcludedDishIds = new HashSet<Guid> { excluded.Id } }, default);
        Assert.Equal(fitting.Id, Assert.Single(result.Dishes).DishId);
        Assert.Equal(RecommendationOutcome.Matches, result.Outcome);
    }

    [Fact]
    public async Task HistoricalDatesDoNotConsumeProviderEvidenceBudget()
    {
        var dish = Dish.CreateNew(family, "Soup");
        SetCatalog(dish);
        var history = Enumerable.Range(1, 400).Select(offset => {
            var dinner = Dinner.CreateNew(family, monday.PlusDays(-offset));
            dinner.AddMenuItem(new(dish.Id));
            return dinner;
        });
        dinners.Setup(repository => repository.GetAsync(family, LocalDate.MinIsoValue, LocalDate.MaxIsoValue)).Returns(Stream(history));
        provider.Setup(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecommendationProviderResult(RecommendationOutcome.Matches, [Match(dish, 1)], "Soup", null));
        var result = await Query(new() { MaximumEvidenceCharacters = 2_000 }).RecommendAsync(family, Request(), default);
        Assert.Equal(dish.Id, Assert.Single(result.Dishes).DishId);
    }

    [Fact]
    public async Task MoreIdeasReportsExhaustionInsteadOfRecyclingExclusions()
    {
        var dish = Dish.CreateNew(family, "Soup");
        SetCatalog(dish);
        var request = Request() with { Mode = RecommendationMode.More, ExcludedDishIds = new HashSet<Guid> { dish.Id } };
        var result = await Query().RecommendAsync(family, request, default);
        Assert.Equal(RecommendationOutcome.Exhausted, result.Outcome);
        Assert.Empty(result.Dishes);
        provider.Verify(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("unknown-id")]
    [InlineData("unknown-source")]
    [InlineData("duplicate")]
    [InlineData("nonfinite")]
    public async Task InvalidProviderMatchesAreRejected(string invalidity)
    {
        var dish = Dish.CreateNew(family, "Soup");
        SetCatalog(dish);
        var match = Match(dish, 1);
        if (invalidity == "unknown-id") match = match with { DishId = Guid.NewGuid() };
        if (invalidity == "unknown-source") match = match with { Explanations = [new("Unsupported fact", RecommendationEvidenceKind.Fact, ["invented"])] };
        if (invalidity == "nonfinite") match = match with { Suitability = double.NaN };
        provider.Setup(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecommendationProviderResult(RecommendationOutcome.Matches, invalidity == "duplicate" ? [match, match] : [match], "Soup", null));
        await Assert.ThrowsAsync<DishRecommendationProviderException>(() => Query().RecommendAsync(family, Request(), default));
    }

    [Fact]
    public async Task ProviderFailuresAreNotNoMatch()
    {
        SetCatalog(Dish.CreateNew(family, "Soup"));
        provider.Setup(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DishRecommendationProviderException("PROVIDER_UNAVAILABLE"));
        var exception = await Assert.ThrowsAsync<DishRecommendationProviderException>(() => Query().RecommendAsync(family, Request(), default));
        Assert.Equal("PROVIDER_UNAVAILABLE", exception.Message);
    }

    [Fact]
    public async Task CancellationPreventsFamilyLoading()
    {
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Assembler().AssembleAsync(family, monday, new CancellationToken(true)));
        dishes.Verify(repository => repository.GetDishesAsync(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task RoleMismatchCannotAppearAsSupportedRecommendation()
    {
        var side = Dish.CreateNew(family, "Potatoes");
        side.UpdateMetadata(DishMetadataValueObject.FromUserEdit(side.Metadata, [DishRole.Side], null, null, null));
        SetCatalog(side, Dish.CreateNew(family, "Main"));
        provider.Setup(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecommendationProviderResult(RecommendationOutcome.Matches, [Match(side, 1)], "Pairing", null));
        var exception = await Assert.ThrowsAsync<DishRecommendationProviderException>(() => Query().RecommendAsync(family, Request(), default));
        Assert.Equal("UNSUPPORTED_PROVIDER_MATCH", exception.Message);
        var result = await Query().RecommendAsync(family, Request() with { Role = DishRole.Side }, default);
        Assert.Equal(side.Id, Assert.Single(result.Dishes).DishId);
    }

    [Fact]
    public async Task DishNameFilterLetsSameRoleCatalogFitEvidenceBudget()
    {
        var soup = Dish.CreateNew(family, "Tomato soup");
        SetCatalog(soup, Dish.CreateNew(family, "Roast"));
        provider.Setup(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecommendationProviderResult(RecommendationOutcome.Matches, [Match(soup, 1)], "Soup", null));
        var result = await Query(new() { MaximumCandidates = 1 }).RecommendAsync(family, Request() with { NameFilter = "SOUP" }, default);
        Assert.Equal(soup.Id, Assert.Single(result.Dishes).DishId);
        provider.Verify(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(),
            It.Is<DishRecommendationContext>(context => context.Dishes.Count == 1 && context.Dishes[0].DishId == soup.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoreNoMatchIsExhaustionUnderUnchangedConstraints()
    {
        SetCatalog(Dish.CreateNew(family, "Soup"));
        provider.Setup(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecommendationProviderResult(RecommendationOutcome.NoMatch, [], "No fish", "No further matches"));
        var request = Request() with { Mode = RecommendationMode.More, Constraints = ["No fish"] };
        var result = await Query().RecommendAsync(family, request, default);
        Assert.Equal(RecommendationOutcome.Exhausted, result.Outcome);
        Assert.Equal(["No fish"], result.ActiveConstraints);
    }

    [Fact]
    public async Task ProviderTimeoutIsDistinctFromNoMatch()
    {
        SetCatalog(Dish.CreateNew(family, "Soup"));
        provider.Setup(client => client.RecommendAsync(It.IsAny<DishRecommendationRequest>(), It.IsAny<DishRecommendationContext>(), It.IsAny<CancellationToken>()))
            .Returns((DishRecommendationRequest _, DishRecommendationContext _, CancellationToken token) => WaitForCancellation(token));
        var exception = await Assert.ThrowsAsync<DishRecommendationProviderException>(() => Query(new() { ProviderTimeoutSeconds = 1 }).RecommendAsync(family, Request(), default));
        Assert.Equal("PROVIDER_TIMEOUT", exception.Message);
    }

    [Theory]
    [InlineData("locale")]
    [InlineData("monday")]
    [InlineData("target")]
    [InlineData("size")]
    public async Task InvalidIntentIsRejectedBeforeFamilyLoading(string invalidity)
    {
        var request = Request();
        if (invalidity == "locale") request = request with { Locale = "fr" };
        if (invalidity == "monday") request = request with { SelectedMonday = monday.PlusDays(1) };
        if (invalidity == "target") request = request with { TargetDate = monday.PlusDays(7) };
        if (invalidity == "size") request = request with { Turns = [new string('x', 4_001)] };
        await Assert.ThrowsAsync<ArgumentException>(() => Query().RecommendAsync(family, request, default));
        dishes.Verify(repository => repository.GetDishesAsync(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
    }

    private static async Task<RecommendationProviderResult> WaitForCancellation(CancellationToken token)
    {
        await Task.Delay(Timeout.Infinite, token);
        throw new InvalidOperationException();
    }

    private void SetCatalog(params Dish[] catalog)
    {
        dishes.Setup(repository => repository.GetDishesAsync(family, false)).ReturnsAsync(catalog);
        dinners.Setup(repository => repository.GetAsync(family, LocalDate.MinIsoValue, LocalDate.MaxIsoValue)).Returns(Stream([]));
        wishes.Setup(repository => repository.GetActiveAsync(family)).ReturnsAsync([]);
    }

    private DishRecommendationContextAssembler Assembler() => new(dishes.Object, dinners.Object, wishes.Object, clock);
    private DishRecommendationQuery Query(DishRecommendationLimits limits = null)
    {
        var rule = new ResurfacingRule();
        return new(Assembler(), new(rule), rule, provider.Object, limits ?? new());
    }
    private DishRecommendationRequest Request() => new(monday, null, RecommendationMode.Request, "en", ["Works with potatoes"], [], new HashSet<Guid>());
    private static RecommendationProviderMatch Match(Dish dish, double suitability)
        => new(dish.Id, suitability, [new("Possible pairing", RecommendationEvidenceKind.Inference, [$"{dish.Id}:title"])], ["Recipe unavailable"]);
    private static async IAsyncEnumerable<Dinner> Stream(IEnumerable<Dinner> saved)
    {
        await Task.CompletedTask;
        foreach (var dinner in saved) yield return dinner;
    }
}
