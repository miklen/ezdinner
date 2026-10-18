using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using EzDinner.Application.Commands.Families;
using EzDinner.Infrastructure.GroceryRecommendations;
using EzDinner.Query.Core.DishRecommendationQueries;
using NodaTime;
using NodaTime.Testing;
using Xunit;

namespace EzDinner.UnitTests.DishRecommendationTests;

public class GroceryOfferReaderTests
{
    private static readonly Instant Now = Instant.FromUtc(2026, 10, 18, 8, 0);

    [Theory]
    [InlineData("netto")]
    [InlineData("lidl")]
    [InlineData("calle")]
    public async Task Any_selected_catalog_chain_can_acquire_without_store_configuration(string storeId)
    {
        using var fixture = new ReaderFixture(_ => JsonResponse("{\"offers\":[],\"total\":0,\"page\":1,\"limit\":100,\"as_of\":\"2026-10-18\"}"));
        fixture.Catalog.Stores = new[] { new GroceryStoreDescriptor(storeId, storeId, true, storeId == "calle", false) };
        var result = await fixture.Reader.ReadAsync(new[] { storeId }, default);
        Assert.Equal(GroceryCoverageStatus.Fresh, result.Coverage.Status);
        Assert.Equal($"/v1/offers?chain={storeId}&limit=100&page=1", Assert.Single(fixture.Transport.Paths));
        Assert.Contains(Scope(storeId), fixture.Cache.PublishedScopes);
        var checkedStore = Assert.Single(result.Coverage.Stores);
        Assert.Equal(storeId, checkedStore.StoreId);
        Assert.Equal(GroceryCoverageStatus.Fresh, checkedStore.Status);
        Assert.Equal(0, checkedStore.UsableOfferCount);
    }

    [Fact]
    public async Task Failed_nemlig_refresh_preserves_fresh_rema_snapshot_and_independent_coverage()
    {
        using var fixture = new ReaderFixture();
        fixture.Cache.Snapshots[Scope("rema1000")] = Snapshot("rema1000");
        var result = await fixture.Reader.ReadAsync(new[] { "nemlig", "rema1000" }, default);
        Assert.Equal("rema1000", Assert.Single(result.Offers).StoreId);
        Assert.Equal(GroceryCoverageStatus.Partial, result.Coverage.Status);
        Assert.Equal(GroceryCoverageStatus.Fresh, result.StoreFreshness["rema1000"]);
        Assert.Equal(GroceryCoverageStatus.Unavailable, result.StoreFreshness["nemlig"]);
        Assert.Contains("source-unavailable", result.Coverage.Limitations);
        Assert.Equal(new[] {
            new GroceryStoreCoverage("nemlig", "Nemlig", GroceryCoverageStatus.Unavailable, 0),
            new GroceryStoreCoverage("rema1000", "REMA 1000", GroceryCoverageStatus.Fresh, 1)
        }, result.Coverage.Stores);
        Assert.Contains("chain=nemlig", Assert.Single(fixture.Transport.Paths));
        Assert.Equal("2026-10-18", fixture.Cache.AttemptDays[Scope("nemlig")]);
        var again = await fixture.Reader.ReadAsync(new[] { "nemlig", "rema1000" }, default);
        Assert.Single(again.Offers);
        Assert.Single(fixture.Transport.Paths);
    }

    [Fact]
    public async Task Fresh_rema_acquisition_survives_independent_nemlig_http_failure()
    {
        using var fixture = new ReaderFixture(request =>
        {
            var path = request.RequestUri.PathAndQuery;
            if (path.Contains("chain=nemlig")) return new(HttpStatusCode.ServiceUnavailable);
            if (path.StartsWith("/v1/products/batch", StringComparison.Ordinal))
                return JsonResponse(ProductFixture());
            return JsonResponse(OfferFixture());
        });
        var result = await fixture.Reader.ReadAsync(new[] { "nemlig", "rema1000" }, default);
        var offer = Assert.Single(result.Offers);
        Assert.Equal("rema1000", offer.StoreId);
        Assert.Equal(39m, offer.Price);
        Assert.Equal(GroceryCoverageStatus.Fresh, result.StoreFreshness["rema1000"]);
        Assert.Equal(GroceryCoverageStatus.Unavailable, result.StoreFreshness["nemlig"]);
        Assert.Equal(GroceryCoverageStatus.Partial, result.Coverage.Status);
        Assert.Contains(Scope("rema1000"), fixture.Cache.PublishedScopes);
        Assert.DoesNotContain(Scope("nemlig"), fixture.Cache.PublishedScopes);
    }

    [Fact]
    public async Task Per_store_freshness_distinguishes_fresh_and_stale_usable_offers()
    {
        using var fixture = new ReaderFixture();
        fixture.Cache.Snapshots[Scope("rema1000")] = Snapshot("rema1000");
        fixture.Cache.Snapshots[Scope("nemlig")] = Snapshot("nemlig", "2026-10-17", Now - Duration.FromDays(1));
        fixture.Cache.AttemptDays[Scope("nemlig")] = "2026-10-18";
        var result = await fixture.Reader.ReadAsync(new[] { "rema1000", "nemlig" }, default);
        Assert.Equal(2, result.Offers.Count);
        Assert.Equal(GroceryCoverageStatus.Fresh, result.StoreFreshness["rema1000"]);
        Assert.Equal(GroceryCoverageStatus.Stale, result.StoreFreshness["nemlig"]);
        Assert.Equal(GroceryCoverageStatus.Partial, result.Coverage.Status);
        Assert.Contains("source-stale", result.Coverage.Limitations);
        Assert.Empty(fixture.Transport.Paths);
    }

    [Theory]
    [InlineData(false, true, GroceryCoverageStatus.Disabled)]
    [InlineData(true, false, GroceryCoverageStatus.Unavailable)]
    public async Task Disabled_or_unconfigured_source_never_calls_http_or_snapshot_store(bool enabled, bool hasKey, GroceryCoverageStatus status)
    {
        using var fixture = new ReaderFixture();
        fixture.Options.Enabled = enabled;
        fixture.Options.ApiKey = hasKey ? "fixture-only" : null;
        var result = await fixture.Reader.ReadAsync(new[] { "rema1000" }, default);
        Assert.Empty(result.Offers);
        Assert.Equal(status, result.Coverage.Status);
        Assert.Empty(fixture.Transport.Paths);
        Assert.Empty(fixture.Cache.ReadScopes);
        Assert.Empty(fixture.Cache.AttemptDays);
        if (!enabled) Assert.Equal(0, fixture.Catalog.Reads);
    }

    [Fact]
    public async Task Empty_selection_skips_catalog_cache_and_http()
    {
        using var fixture = new ReaderFixture();
        var result = await fixture.Reader.ReadAsync(Array.Empty<string>(), default);
        Assert.Equal(GroceryCoverageStatus.Disabled, result.Coverage.Status);
        Assert.Equal(0, fixture.Catalog.Reads);
        Assert.Empty(fixture.Cache.ReadScopes);
        Assert.Empty(fixture.Transport.Paths);
    }

    [Fact]
    public async Task Selected_chain_missing_from_catalog_is_unavailable_without_outbound_requests()
    {
        using var fixture = new ReaderFixture();
        var result = await fixture.Reader.ReadAsync(new[] { "removed-chain" }, default);
        Assert.Equal(GroceryCoverageStatus.Unavailable, result.Coverage.Status);
        Assert.Equal(GroceryCoverageStatus.Unavailable, result.StoreFreshness["removed-chain"]);
        Assert.Empty(fixture.Transport.Paths);
        Assert.Empty(fixture.Cache.ReadScopes);
    }

    [Theory]
    [InlineData("wrong-scope")]
    [InlineData("malformed")]
    public async Task Invalid_store_snapshot_cannot_contaminate_other_store(string failure)
    {
        using var fixture = new ReaderFixture();
        fixture.Cache.Snapshots[Scope("rema1000")] = Snapshot("rema1000");
        fixture.Cache.Snapshots[Scope("nemlig")] = failure == "wrong-scope"
            ? Snapshot("rema1000") : new(new[] { "not-json" }, Now.ToDateTimeUtc());
        var result = await fixture.Reader.ReadAsync(new[] { "nemlig", "rema1000" }, default);
        Assert.Equal("rema1000", Assert.Single(result.Offers).StoreId);
        Assert.Equal(GroceryCoverageStatus.Unavailable, result.StoreFreshness["nemlig"]);
        Assert.Equal(GroceryCoverageStatus.Fresh, result.StoreFreshness["rema1000"]);
        Assert.Empty(fixture.Transport.Paths);
    }

    [Fact]
    public async Task Canonical_store_identity_deduplicates_shared_snapshot_reads()
    {
        using var fixture = new ReaderFixture();
        fixture.Cache.Snapshots[Scope("rema1000")] = Snapshot("rema1000");
        fixture.Cache.Snapshots[Scope("nemlig")] = Snapshot("nemlig");
        var result = await fixture.Reader.ReadAsync(new[] { "rema1000", "nemlig", "rema1000" }, default);
        Assert.Equal(new[] { "nemlig", "rema1000" }, result.Coverage.StoreIds);
        Assert.Equal(2, result.Offers.Count);
        Assert.Equal(2, fixture.Cache.ReadScopes.Count);
        Assert.Equal(2, result.StoreFreshness.Count);
        Assert.Empty(fixture.Transport.Paths);
    }

    [Fact]
    public async Task Refreshed_snapshot_with_expired_source_observation_is_stale_and_has_no_offer_influence()
    {
        using var fixture = new ReaderFixture();
        fixture.Cache.Snapshots[Scope("rema1000")] = Snapshot("rema1000", "2026-10-16");
        var result = await fixture.Reader.ReadAsync(new[] { "rema1000" }, default);
        Assert.Empty(result.Offers);
        Assert.Equal(GroceryCoverageStatus.Stale, result.Coverage.Status);
        Assert.Equal(GroceryCoverageStatus.Stale, result.StoreFreshness["rema1000"]);
        Assert.Equal(new GroceryStoreCoverage("rema1000", "REMA 1000", GroceryCoverageStatus.Stale, 0), Assert.Single(result.Coverage.Stores));
        Assert.Empty(fixture.Transport.Paths);
    }

    private static string Scope(string id) => $"retailer:{id}:chain";
    [Fact]
    public async Task Partial_snapshot_keeps_stale_freshness_after_failed_refresh()
    {
        using var fixture = new ReaderFixture();
        fixture.Cache.Snapshots[Scope("nemlig")] = Snapshot("nemlig", "2026-10-17", Now - Duration.FromDays(1), partial: true);
        var result = await fixture.Reader.ReadAsync(new[] { "nemlig" }, default);
        Assert.Single(result.Offers);
        Assert.Single(fixture.Transport.Paths);
        Assert.Equal(GroceryCoverageStatus.Partial, result.Coverage.Status);
        Assert.Equal(GroceryCoverageStatus.Stale, result.StoreFreshness["nemlig"]);
        Assert.Contains("source-stale", result.Coverage.Limitations);
    }

    private static GrocerySnapshot Snapshot(string id, string observed = "2026-10-18", Instant? retrieved = null, bool partial = false)
    {
        var offer = new StoredGroceryOffer($"prej:{id}:1", id, "1", "Hakket oksekød", "minced-beef", 36m, 40m, observed,
            null, "2026-10-22", new[] { "chain-price" });
        return new(new[] { JsonSerializer.Serialize(new StoredGroceryOfferPage(new[] { offer }, partial)) }, (retrieved ?? Now).ToDateTimeUtc());
    }

    private static string OfferFixture()
    {
        var fixture = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Prej", "prej-rema1000-offers.json")));
        return new JsonObject
        {
            ["offers"] = new JsonArray(fixture["response"]["offers"][2].DeepClone()),
            ["page"] = 1, ["limit"] = 100, ["total"] = 1, ["as_of"] = "2026-10-18"
        }.ToJsonString();
    }

    private static string ProductFixture()
    {
        var fixture = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Prej", "prej-product-observations.json")));
        return new JsonObject { ["products"] = new JsonArray(fixture["response"]["products"][1].DeepClone()) }.ToJsonString();
    }

    private static HttpResponseMessage JsonResponse(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body) };

    private sealed class ReaderFixture : IDisposable
    {
        public GroceryRecommendationOptions Options { get; } = new()
        {
            Enabled = true, ApiKey = "fixture-only"
        };
        public ScopedSnapshots Cache { get; } = new();
        public StoreCatalog Catalog { get; } = new();
        public RecordingTransport Transport { get; }
        public CachedGroceryOfferReader Reader { get; }
        private readonly HttpClient _client;

        public ReaderFixture(Func<HttpRequestMessage, HttpResponseMessage> response = null)
        {
            Transport = new(response ?? (_ => new(HttpStatusCode.ServiceUnavailable)));
            _client = new(Transport);
            var clock = new FakeClock(Now);
            Reader = new(new(new(_client, Options), Options), new(Cache, clock), Catalog, Options, clock);
        }

        public void Dispose() { Reader.Dispose(); _client.Dispose(); }
    }

    private sealed class StoreCatalog : IGroceryStoreCatalog
    {
        public int Reads { get; private set; }
        public IReadOnlyList<GroceryStoreDescriptor> Stores { get; set; } = new[]
        {
            new GroceryStoreDescriptor("nemlig", "Nemlig", true, false, false),
            new GroceryStoreDescriptor("rema1000", "REMA 1000", true, false, false)
        };
        public Task<IReadOnlyList<GroceryStoreDescriptor>> GetStoresAsync(CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult(Stores);
        }
    }

    private sealed class RecordingTransport(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public ConcurrentBag<string> Paths { get; } = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri.PathAndQuery);
            return Task.FromResult(response(request));
        }
    }

    private sealed class ScopedSnapshots : IGrocerySnapshotStore
    {
        public ConcurrentDictionary<string, GrocerySnapshot> Snapshots { get; } = new();
        public ConcurrentDictionary<string, string> AttemptDays { get; } = new();
        public ConcurrentBag<string> ReadScopes { get; } = new();
        public ConcurrentBag<string> PublishedScopes { get; } = new();

        public Task<GrocerySnapshot> ReadSnapshotAsync(string scope, CancellationToken cancellationToken)
        {
            ReadScopes.Add(scope);
            Snapshots.TryGetValue(scope, out var snapshot);
            return Task.FromResult(snapshot);
        }

        public Task<GroceryRefreshClaim> TryClaimAsync(string scope, string day, CancellationToken cancellationToken)
        {
            if (!AttemptDays.TryAdd(scope, day)) return Task.FromResult<GroceryRefreshClaim>(null);
            return Task.FromResult(new GroceryRefreshClaim(scope, Guid.NewGuid().ToString("N"), day, "test-etag", null, 0, null));
        }

        public Task PublishAsync(GroceryRefreshClaim claim, IReadOnlyList<string> pages, CancellationToken cancellationToken)
        {
            PublishedScopes.Add(claim.Scope);
            Snapshots[claim.Scope] = new(pages.ToArray(), Now.ToDateTimeUtc());
            return Task.CompletedTask;
        }
    }
}
