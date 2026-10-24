using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using EzDinner.Infrastructure.GroceryRecommendations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Testing;
using Xunit;

namespace EzDinner.UnitTests.DishRecommendationTests;

public class GroceryAcquisitionTests
{
    [Fact]
    public async Task Failed_page_cancels_inflight_pages_and_never_starts_queued_pages()
    {
        using var transport = new FailingPaginationTransport();
        using var http = new HttpClient(transport);
        var source = new PrejGroceryOfferSource(new(http, new() { ApiKey = "test-credential" }), new());
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var failure = await Assert.ThrowsAsync<HttpRequestException>(() => source.AcquireAsync("nemlig", deadline.Token));
        Assert.Equal(HttpStatusCode.TooManyRequests, failure.StatusCode);
        Assert.Equal(5, transport.Started);
        Assert.Equal(3, transport.Cancelled);
    }

    private sealed class FailingPaginationTransport : HttpMessageHandler
    {
        private readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Started;
        public int Cancelled;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var call = Interlocked.Increment(ref Started);
            if (call == 1)
                return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new
                {
                    offers = Enumerable.Range(1, 100).Select(id => new { product_id = id }),
                    total = 1200, page = 1, limit = 100, as_of = "2026-10-24"
                })) };
            if (call == 5) started.TrySetResult();
            if (request.RequestUri.Query.EndsWith("page=2", StringComparison.Ordinal))
            {
                await started.Task.WaitAsync(cancellationToken);
                return new(HttpStatusCode.TooManyRequests);
            }
            try { await Task.Delay(Timeout.Infinite, cancellationToken); }
            catch (OperationCanceledException) { Interlocked.Increment(ref Cancelled); throw; }
            throw new InvalidOperationException("Unexpected completed page");
        }
    }

    [Theory]
    [InlineData("missing-observation", "invalid-source-field:observation:last_seen_date:KeyNotFoundException", true)]
    [InlineData("missing-ai-match", "invalid-source-field:identity:ai_matched:KeyNotFoundException", true)]
    [InlineData("invalid-price-type", "invalid-source-field:offer:price:InvalidOperationException", true)]
    [InlineData("price-mismatch", "observation-not-found", true)]
    [InlineData("ai-matched", "ai-matched-identity", true)]
    [InlineData("missing-reference", "missing-reference-price", false)]
    [InlineData("unsupported-ingredient", "unsupported-ingredient", false)]
    public async Task Empty_normalized_snapshot_reports_exclusion_reason_without_source_content(
        string scenario, string reason, bool partial)
    {
        var (offer, product) = MutableRows();
        switch (scenario)
        {
            case "missing-observation": product["prices"][0].AsObject().Remove("last_seen_date"); break;
            case "missing-ai-match": product["prices"][0].AsObject().Remove("ai_matched"); break;
            case "invalid-price-type": offer["price"] = "invalid"; break;
            case "price-mismatch": product["prices"][0]["price"] = 1; break;
            case "ai-matched": product["prices"][0]["ai_matched"] = true; break;
            case "missing-reference": offer["normal_price"] = null; break;
            case "unsupported-ingredient": offer["name"] = "Private diagnostic sentinel"; break;
        }
        var logger = new DiagnosticLogger<PrejGroceryOfferSource>();
        var pages = await AcquireWithDiagnostics(offer, product, logger);
        var page = JsonSerializer.Deserialize<StoredGroceryOfferPage>(Assert.Single(pages));
        Assert.Empty(page.Offers);
        Assert.Equal(partial, page.IsPartial);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(partial ? LogLevel.Warning : LogLevel.Information, entry.Level);
        Assert.Equal("rema1000", entry.Fields["StoreId"]);
        Assert.Equal(1, entry.Fields["RawOfferCount"]);
        Assert.Equal(0, entry.Fields["NormalizedOfferCount"]);
        var counts = JsonSerializer.Deserialize<Dictionary<string, int>>((string)entry.Fields["Exclusions"]);
        Assert.Equal(1, Assert.Single(counts).Value);
        Assert.Equal(reason, Assert.Single(counts).Key);
        var examples = JsonSerializer.Deserialize<Dictionary<string, long>>((string)entry.Fields["ExclusionExamples"]);
        Assert.Equal(37260, examples[reason]);
        Assert.DoesNotContain("Private diagnostic sentinel", entry.Message);
        Assert.DoesNotContain("test-credential", entry.Message);
    }

    [Fact]
    public async Task Accepted_offer_reports_normalized_count_without_changing_cached_evidence()
    {
        var (offer, product) = MutableRows();
        var logger = new DiagnosticLogger<PrejGroceryOfferSource>();
        var pages = await AcquireWithDiagnostics(offer, product, logger);
        var page = JsonSerializer.Deserialize<StoredGroceryOfferPage>(Assert.Single(pages));
        Assert.Equal(39m, Assert.Single(page.Offers).Price);
        Assert.False(page.IsPartial);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal(1, entry.Fields["RawOfferCount"]);
        Assert.Equal(1, entry.Fields["NormalizedOfferCount"]);
        Assert.Equal("{}", entry.Fields["Exclusions"]);
    }

    private static async Task<IReadOnlyList<string>> AcquireWithDiagnostics(JsonNode offer, JsonNode product,
        DiagnosticLogger<PrejGroceryOfferSource> logger)
    {
        using var transport = new StubTransport(request => request.RequestUri.AbsolutePath == "/v1/offers"
            ? JsonSerializer.Serialize(new { offers = new[] { offer }, total = 1, page = 1, limit = 100, as_of = "2026-10-18" })
            : JsonSerializer.Serialize(new { products = new[] { product } }));
        using var http = new HttpClient(transport);
        var options = new GroceryRecommendationOptions { ApiKey = "test-credential" };
        using var services = new ServiceCollection().AddSingleton<ILogger<PrejGroceryOfferSource>>(logger).BuildServiceProvider();
        var source = ActivatorUtilities.CreateInstance<PrejGroceryOfferSource>(services, new PrejHttpClient(http, options), options);
        return await source.AcquireAsync("rema1000", default);
    }

    [Fact]
    public async Task Rate_limit_is_logged_before_throwing_with_retry_and_quota_headers()
    {
        using var http = new HttpClient(new RateLimitedTransport());
        var logger = new DiagnosticLogger<PrejHttpClient>();
        using var services = new ServiceCollection().AddSingleton<ILogger<PrejHttpClient>>(logger).BuildServiceProvider();
        var client = ActivatorUtilities.CreateInstance<PrejHttpClient>(services, http,
            new GroceryRecommendationOptions { ApiKey = "test-credential" });
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/v1/products/batch?ids=37260", default));
        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(429, entry.Fields["StatusCode"]);
        Assert.Equal("/v1/products/batch", entry.Fields["Endpoint"]);
        Assert.Equal("19008", entry.Fields["RetryAfter"]);
        Assert.Equal("25", entry.Fields["DailyLimit"]);
        Assert.Equal("0", entry.Fields["DailyRemaining"]);
        Assert.DoesNotContain("test-credential", entry.Message);
        Assert.DoesNotContain("ids=", entry.Message);
    }

    private sealed class RateLimitedTransport : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.TryAddWithoutValidation("Retry-After", "19008");
            response.Headers.TryAddWithoutValidation("X-RateLimit-Limit-Day", "25");
            response.Headers.TryAddWithoutValidation("X-RateLimit-Remaining-Day", "0");
            return Task.FromResult(response);
        }
    }

    private sealed class DiagnosticLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, Dictionary<string, object> Fields, string Message)> Entries { get; } = [];
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter) => Entries.Add((logLevel,
                ((IEnumerable<KeyValuePair<string, object>>)state).ToDictionary(pair => pair.Key, pair => pair.Value),
                formatter(state, exception)));
    }

    [Fact]
    public void LiveFixturesJoinPriceObservationRatherThanRetrievalDate()
    {
        using var offers = Fixture("prej-rema1000-offers.json");
        using var products = Fixture("prej-product-observations.json");
        var offer = offers.RootElement.GetProperty("response").GetProperty("offers")[2];
        var product = products.RootElement.GetProperty("response").GetProperty("products")[1];
        var result = PrejGroceryOfferSource.Normalize("rema1000", offer, product);
        Assert.False(result.IsPartial);
        Assert.Equal(39m, result.Offer.Price);
        Assert.Equal(44.95m, result.Offer.ReferencePrice);
        Assert.Equal("beef", result.Offer.IngredientKey);
        Assert.Equal(Instant.FromUtc(2026, 10, 17, 22, 0), result.Offer.ToDomain().ObservedAt);
        Assert.False(result.Offer.ToDomain().IsUsable(Instant.FromUtc(2026, 10, 19, 22, 0)));
    }

    [Theory]
    [InlineData("Kyllingenuggets")]
    [InlineData("Kyllingefilet paneret")]
    [InlineData("Kylling tikka")]
    [InlineData("Munkebo Laksep\u00e2t\u00e9")]
    [InlineData("Munkebo Laksepate")]
    [InlineData("Munkebo Laksep\u00c3\u00a2t\u00c3\u00a9")]
    [InlineData("Vegetarisk hakket oksekød")]
    [InlineData("Kyllingepålæg")]
    [InlineData("Hakket svine- og kalvekød")]
    [InlineData("Hakket okse- og svinekød")]
    public void PreparedAndAlternativeProductsCannotBecomeRawIngredientOffers(string name)
    {
        var (offer, product) = MutableRows();
        offer["name"] = name;
        Assert.Null(PrejGroceryOfferSource.Normalize("rema1000", Element(offer), Element(product)).Offer);
    }

    [Fact]
    public void MissingReferenceAndUnsupportedMultiBuyHaveNoDiscountInfluence()
    {
        var (offer, product) = MutableRows();
        offer["normal_price"] = null;
        Assert.Null(PrejGroceryOfferSource.Normalize("rema1000", Element(offer), Element(product)).Offer);
        offer["normal_price"] = 4495;
        offer["multi_buy"] = new JsonObject { ["quantity"] = 3, ["price"] = 9000, ["price_each"] = 3000 };
        Assert.Null(PrejGroceryOfferSource.Normalize("rema1000", Element(offer), Element(product)).Offer);
    }

    [Fact]
    public void CapturedAiMatchedConflictingChickenPackageIsNotVerifiedEvidence()
    {
        using var offers = Fixture("prej-nemlig-offers.json");
        using var products = Fixture("prej-product-observations.json");
        var offer = offers.RootElement.GetProperty("response").GetProperty("offers")
            .EnumerateArray().Single(row => row.GetProperty("product_id").GetInt64() == 17590);
        var product = products.RootElement.GetProperty("response").GetProperty("products")[0];
        var result = PrejGroceryOfferSource.Normalize("nemlig", offer, product);
        Assert.Null(result.Offer);
        Assert.True(result.IsPartial);
    }

    [Fact]
    public void ConflictingDescriptionSizeIsRejectedEvenWithoutAiMatching()
    {
        var (offer, product) = MutableRows();
        product["description"] = "Ungkvæg i tern. 350 g.";
        var result = PrejGroceryOfferSource.Normalize("rema1000", Element(offer), Element(product));
        Assert.Null(result.Offer);
        Assert.True(result.IsPartial);
    }

    [Fact]
    public async Task MissingObservationProductsRetainPreviousSnapshotDuringCooldown()
    {
        var (offer, _) = MutableRows();
        var calls = 0;
        using var transport = new StubTransport(request =>
        {
            calls++;
            return request.RequestUri.AbsolutePath == "/v1/offers"
                ? JsonSerializer.Serialize(new { offers = new[] { offer }, total = 1, page = 1, limit = 100, as_of = "2026-10-18" })
                : "{\"products\":[],\"as_of\":\"2026-10-18\"}";
        });
        var source = new PrejGroceryOfferSource(new(new HttpClient(transport), new() { ApiKey = "test-credential" }), new());
        var clock = new FakeClock(Instant.FromUtc(2026, 10, 18, 8, 0));
        var cache = new SharedCache(clock) { Snapshot = new(["previous"], new DateTime(2026, 10, 17, 8, 0, 0, DateTimeKind.Utc)) };
        var refresh = new GrocerySnapshotRefresh(cache, clock);
        var first = await refresh.ReadAsync("scope", token => source.AcquireAsync("rema1000", token), default);
        var second = await refresh.ReadAsync("scope", token => source.AcquireAsync("rema1000", token), default);
        Assert.True(first.IsStale);
        Assert.Equal("previous", Assert.Single(second.Snapshot.Pages));
        Assert.Equal(2, calls);
    }

    [Fact]
    public void MismatchedUnitsOrMissingObservationCannotUseAsOf()
    {
        var (offer, product) = MutableRows();
        product["prices"][0]["unit"] = "kg";
        Assert.True(PrejGroceryOfferSource.Normalize("rema1000", Element(offer), Element(product)).IsPartial);
        product["prices"][0]["unit"] = "g";
        product["prices"][0].AsObject().Remove("last_seen_date");
        Assert.True(PrejGroceryOfferSource.Normalize("rema1000", Element(offer), Element(product)).IsPartial);
    }

    [Fact]
    public void CatalogIncludesEveryReturnedChainButNotOrphanAuxiliarySlugs()
    {
        using var fixture = Fixture("prej-chains.json");
        var stores = PrejGroceryStoreCatalog.Parse(fixture.RootElement.GetProperty("response"));
        Assert.Equal(21, stores.Length);
        Assert.Contains(stores, store => store.Id == "calle" && store.IsBorderTrade);
        Assert.Contains(stores, store => store.Id == "365discount" && store.IsFlyerOnly);
        Assert.DoesNotContain(stores, store => store.Id == "otto-duborg");
    }

    [Fact]
    public async Task PartialPaginationIsRejectedBeforeProductObservationRequests()
    {
        var rows = Enumerable.Range(1, 100).Select(id => new { product_id = id }).ToArray();
        var paths = new List<string>();
        using var transport = new StubTransport(request =>
        {
            paths.Add(request.RequestUri.PathAndQuery);
            return JsonSerializer.Serialize(new { offers = paths.Count == 1 ? rows : [],
                total = 101, page = paths.Count, limit = 100, as_of = "2026-10-18" });
        });
        var source = new PrejGroceryOfferSource(new(new HttpClient(transport), new() { ApiKey = "test-credential" }), new());
        await Assert.ThrowsAsync<InvalidOperationException>(() => source.AcquireAsync("rema1000", default));
        Assert.Equal(2, paths.Count);
        Assert.All(paths, path => Assert.StartsWith("/v1/offers?", path));
    }

    [Fact]
    public async Task SuccessfulEmptySourcePublishesAnEmptyCompletePage()
    {
        using var transport = new StubTransport(_ => "{\"offers\":[],\"total\":0,\"page\":1,\"limit\":100,\"as_of\":\"2026-10-18\"}");
        var source = new PrejGroceryOfferSource(new(new HttpClient(transport), new() { ApiKey = "test-credential" }), new());
        var pages = await source.AcquireAsync("rema1000", default);
        var page = JsonSerializer.Deserialize<StoredGroceryOfferPage>(Assert.Single(pages));
        Assert.Empty(page.Offers);
        Assert.False(page.IsPartial);
    }

    [Fact]
    public async Task MalformedAndOversizedTransportBodiesCannotEnterCache()
    {
        using var malformed = new StubTransport(_ => "not-json");
        var client = new PrejHttpClient(new HttpClient(malformed), new() { ApiKey = "test-credential" });
        await Assert.ThrowsAnyAsync<JsonException>(() => client.GetAsync("/v1/chains", default));
        using var oversized = new StubTransport(_ => new string(' ', 2_000_001));
        client = new PrejHttpClient(new HttpClient(oversized), new() { ApiKey = "test-credential" });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/v1/chains", default));
    }

    [Fact]
    public async Task TransportUsesOnlyPrejAndNeverSendsFamilyContent()
    {
        using var transport = new StubTransport(request =>
        {
            Assert.Equal("https", request.RequestUri.Scheme);
            Assert.Equal("api.prej.app", request.RequestUri.Host);
            Assert.Equal("/v1/offers?chain=rema1000", request.RequestUri.PathAndQuery);
            Assert.Null(request.Content);
            Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
            return "{}";
        });
        var client = new PrejHttpClient(new HttpClient(transport), new() { ApiKey = "test-credential" });
        using var response = await client.GetAsync("/v1/offers?chain=rema1000", default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("https://untrusted.example/v1/", default));
    }

    [Fact]
    public async Task ConcurrentReadersAndRestartsShareFailedAttemptUntilCooldownEnds()
    {
        var clock = new FakeClock(Instant.FromUtc(2026, 10, 18, 8, 0));
        var cache = new SharedCache(clock);
        var calls = 0;
        Task<IReadOnlyList<string>> Acquire(CancellationToken _) { Interlocked.Increment(ref calls); throw new HttpRequestException(); }
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => new GrocerySnapshotRefresh(cache, clock).ReadAsync("scope", Acquire, default)));
        Assert.Equal(1, calls);
        await new GrocerySnapshotRefresh(cache, clock).ReadAsync("scope", Acquire, default);
        Assert.Equal(1, calls);
        clock.Advance(Duration.FromMinutes(5));
        await new GrocerySnapshotRefresh(cache, clock).ReadAsync("scope", Acquire, default);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task CancelledRefreshLeavesLastCompleteSnapshotAndCanRecoverAfterCooldown()
    {
        var clock = new FakeClock(Instant.FromUtc(2026, 10, 18, 8, 0));
        var cache = new SharedCache(clock) { Snapshot = new(["previous"], new DateTime(2026, 10, 17, 8, 0, 0, DateTimeKind.Utc)) };
        using var cancel = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new GrocerySnapshotRefresh(cache, clock).ReadAsync("scope", token =>
        {
            cancel.Cancel();
            return Task.FromCanceled<IReadOnlyList<string>>(token);
        }, cancel.Token));
        var called = false;
        var retained = await new GrocerySnapshotRefresh(cache, clock).ReadAsync("scope", _ =>
        {
            called = true;
            return Task.FromResult<IReadOnlyList<string>>(["new"]);
        }, default);
        Assert.False(called);
        Assert.True(retained.IsStale);
        Assert.Equal("previous", Assert.Single(retained.Snapshot.Pages));
        clock.Advance(Duration.FromMinutes(5));
        var recovered = await new GrocerySnapshotRefresh(cache, clock).ReadAsync("scope", _ => Task.FromResult<IReadOnlyList<string>>(["new"]), default);
        Assert.False(recovered.IsStale);
        Assert.Equal("new", Assert.Single(recovered.Snapshot.Pages));
    }

    [Fact]
    public async Task MissingSnapshotCanBeAcquiredBeforeFiveUsingPreviousRefreshDay()
    {
        var clock = new FakeClock(Instant.FromUtc(2026, 10, 18, 2, 59));
        var cache = new SharedCache(clock);
        var result = await new GrocerySnapshotRefresh(cache, clock).ReadAsync("scope", _ => Task.FromResult<IReadOnlyList<string>>(["offers"]), default);
        Assert.Equal("2026-10-17", cache.Day);
        Assert.False(result.IsStale);
    }

    [Fact]
    public async Task TimedOutRefreshCanRecoverAfterCooldownWithoutImmediateRetry()
    {
        var clock = new FakeClock(Instant.FromUtc(2026, 10, 18, 8, 0));
        var cache = new SharedCache(clock);
        var calls = 0;
        async Task<IReadOnlyList<string>> Acquire(CancellationToken token)
        {
            calls++;
            await Task.Delay(Timeout.Infinite, token);
            return ["unreachable"];
        }
        var refresh = new GrocerySnapshotRefresh(cache, clock, options: new() { RefreshTimeoutSeconds = 1 });
        var first = await refresh.ReadAsync("scope", Acquire, default);
        var second = await refresh.ReadAsync("scope", Acquire, default);
        Assert.Null(first.Snapshot);
        Assert.Null(second.Snapshot);
        Assert.Equal(1, calls);
        clock.Advance(Duration.FromMinutes(5));
        var recovered = await new GrocerySnapshotRefresh(cache, clock).ReadAsync("scope", _ => Task.FromResult<IReadOnlyList<string>>(["recovered"]), default);
        Assert.False(recovered.IsStale);
        Assert.Equal("recovered", Assert.Single(recovered.Snapshot.Pages));
    }

    [Fact]
    public async Task SuccessfulEmptySnapshotIsReusedUntilNextFiveOClockBoundary()
    {
        var clock = new FakeClock(Instant.FromUtc(2026, 10, 18, 8, 0));
        var cache = new SharedCache(clock);
        var calls = 0;
        Task<IReadOnlyList<string>> Acquire(CancellationToken _) { calls++; return Task.FromResult<IReadOnlyList<string>>(["{\"Offers\":[]}"]); }
        var refresh = new GrocerySnapshotRefresh(cache, clock);
        await refresh.ReadAsync("scope", Acquire, default);
        clock.Advance(Duration.FromHours(18) + Duration.FromMinutes(59));
        Assert.False((await refresh.ReadAsync("scope", Acquire, default)).IsStale);
        Assert.Equal(1, calls);
        clock.Advance(Duration.FromMinutes(1));
        Assert.False((await refresh.ReadAsync("scope", Acquire, default)).IsStale);
        Assert.Equal(2, calls);
    }

    private static JsonDocument Fixture(string name) => JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Prej", name)));
    private static (JsonNode Offer, JsonNode Product) MutableRows()
    {
        using var offers = Fixture("prej-rema1000-offers.json");
        using var products = Fixture("prej-product-observations.json");
        return (JsonNode.Parse(offers.RootElement.GetProperty("response").GetProperty("offers")[2].GetRawText()),
            JsonNode.Parse(products.RootElement.GetProperty("response").GetProperty("products")[1].GetRawText()));
    }
    private static JsonElement Element(JsonNode node) => JsonSerializer.SerializeToElement(node);
    private sealed class StubTransport(Func<HttpRequestMessage, string> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(respond(request)) });
    }
    private sealed class SharedCache(IClock clock) : IGrocerySnapshotStore
    {
        public string Day { get; private set; }
        private Instant? retryAfter;
        private bool published;
        public GrocerySnapshot Snapshot { get; set; }
        public Task<GrocerySnapshot> ReadSnapshotAsync(string scope, CancellationToken cancellationToken) => Task.FromResult(Snapshot);
        public Task<GroceryRefreshClaim> TryClaimAsync(string scope, string day, CancellationToken cancellationToken)
        {
            lock (this)
            {
                if (retryAfter > clock.GetCurrentInstant() || (Day == day && published)) return Task.FromResult<GroceryRefreshClaim>(null);
                Day = day;
                retryAfter = clock.GetCurrentInstant().Plus(Duration.FromMinutes(5));
                published = false;
                return Task.FromResult(new GroceryRefreshClaim(scope, Guid.NewGuid().ToString(), day, "etag", null, 0, null));
            }
        }
        public Task PublishAsync(GroceryRefreshClaim claim, IReadOnlyList<string> pages, CancellationToken cancellationToken)
        {
            Snapshot = new(pages.ToArray(), clock.GetCurrentInstant().ToDateTimeUtc());
            published = true;
            retryAfter = null;
            return Task.CompletedTask;
        }
    }
}
