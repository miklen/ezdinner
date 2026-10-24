using System.Net;
using EzDinner.Infrastructure.GroceryRecommendations;
using EzDinner.Infrastructure.Models.Json;
using Microsoft.Azure.Cosmos;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using NodaTime.Testing;
using Xunit;
using CachePage = EzDinner.Infrastructure.GroceryRecommendations.CosmosGroceryRecommendationCache.CachePage;
using SnapshotHead = EzDinner.Infrastructure.GroceryRecommendations.CosmosGroceryRecommendationCache.SnapshotHead;
using RateLimitState = EzDinner.Infrastructure.GroceryRecommendations.CosmosGroceryRecommendationCache.RateLimitState;

namespace EzDinner.UnitTests.DishRecommendationTests;

public class CosmosGroceryRecommendationCacheTests
{
    private const string Scope = "retailer:netto:chain";
    private readonly Mock<Container> _container = new();
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 10, 18, 8, 0));
    private readonly CosmosGroceryRecommendationCache _cache;

    public CosmosGroceryRecommendationCacheTests()
    {
        _cache = new(_container.Object, _clock);
        _container.Setup(container => container.ReadItemAsync<SnapshotHead>(It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(Failure(HttpStatusCode.NotFound));
        _container.Setup(container => container.ReadItemAsync<CachePage>(It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(Failure(HttpStatusCode.NotFound));
        _container.Setup(container => container.ReadItemAsync<RateLimitState>(It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(Failure(HttpStatusCode.NotFound));
        _container.Setup(container => container.CreateItemAsync(It.IsAny<SnapshotHead>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response(Head(), "created-etag"));
        _container.Setup(container => container.ReplaceItemAsync(It.IsAny<SnapshotHead>(), "head", It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response(Head(), "replaced-etag"));
    }

    [Fact]
    public async Task Initial_rate_limit_is_persisted_in_its_key_partition_without_expiry()
    {
        var reset = FutureRetry();
        await _cache.ExtendRateLimitAsync("prej:quota:keyhash", reset, default);
        _container.Verify(container => container.CreateItemAsync(
            It.Is<RateLimitState>(state => state.Id == "rate-limit" && state.ScopeKey == "prej:quota:keyhash" &&
                state.RetryAfter == reset && state.Ttl == -1), new PartitionKey("prej:quota:keyhash"),
            It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Shorter_rate_limit_never_overwrites_a_later_reset()
    {
        var state = new RateLimitState("rate-limit", Scope, Future(), -1);
        _container.Setup(container => container.ReadItemAsync<RateLimitState>("rate-limit", new PartitionKey(Scope),
            It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(Response(state));
        Assert.Equal(state.RetryAfter, await _cache.ReadRateLimitAsync(Scope, default));
        await _cache.ExtendRateLimitAsync(Scope, FutureRetry(), default);
        _container.Verify(container => container.ReplaceItemAsync(It.IsAny<RateLimitState>(), It.IsAny<string>(),
            It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Racing_rate_limit_extension_reloads_the_winner_and_preserves_its_later_reset()
    {
        var earlier = new RateLimitState("rate-limit", Scope, _clock.GetCurrentInstant().ToDateTimeUtc(), -1);
        var later = earlier with { RetryAfter = Future() };
        _container.SetupSequence(container => container.ReadItemAsync<RateLimitState>("rate-limit", new PartitionKey(Scope),
                It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response(earlier, "old-etag")).ReturnsAsync(Response(later, "winner-etag"));
        _container.Setup(container => container.ReplaceItemAsync(It.IsAny<RateLimitState>(), "rate-limit", new PartitionKey(Scope),
                It.Is<ItemRequestOptions>(options => options.IfMatchEtag == "old-etag"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(Failure(HttpStatusCode.PreconditionFailed));
        await _cache.ExtendRateLimitAsync(Scope, FutureRetry(), default);
        _container.Verify(container => container.ReplaceItemAsync(It.IsAny<RateLimitState>(), "rate-limit", It.IsAny<PartitionKey?>(),
            It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _container.Verify(container => container.ReadItemAsync<RateLimitState>("rate-limit", new PartitionKey(Scope),
            It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Concurrent_initial_rate_limit_creation_preserves_the_winning_longer_block()
    {
        _container.SetupSequence(container => container.ReadItemAsync<RateLimitState>("rate-limit", new PartitionKey(Scope),
                It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(Failure(HttpStatusCode.NotFound))
            .ReturnsAsync(Response(new RateLimitState("rate-limit", Scope, Future(), -1)));
        _container.Setup(container => container.CreateItemAsync(It.IsAny<RateLimitState>(), new PartitionKey(Scope),
            It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>())).ThrowsAsync(Failure(HttpStatusCode.Conflict));
        await _cache.ExtendRateLimitAsync(Scope, FutureRetry(), default);
        _container.Verify(container => container.CreateItemAsync(It.IsAny<RateLimitState>(), It.IsAny<PartitionKey?>(),
            It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _container.Verify(container => container.ReplaceItemAsync(It.IsAny<RateLimitState>(), It.IsAny<string>(),
            It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(409)]
    [InlineData(412)]
    public async Task Competing_initial_claim_loses_without_retry(int status)
    {
        _container.Setup(container => container.CreateItemAsync(It.IsAny<SnapshotHead>(), new PartitionKey(Scope), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(Failure((HttpStatusCode)status));
        Assert.Null(await _cache.TryClaimAsync(Scope, "2026-10-18", default));
        _container.Verify(container => container.CreateItemAsync(It.IsAny<SnapshotHead>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoHeadReplacement();
    }

    [Fact]
    public async Task Initial_claim_persists_nonexpiring_attempt_and_uses_response_etag()
    {
        var claim = await _cache.TryClaimAsync(Scope, "2026-10-18", default);
        Assert.Equal("created-etag", claim.ETag);
        Assert.Null(claim.PreviousVersion);
        _container.Verify(container => container.CreateItemAsync(It.Is<SnapshotHead>(head => head.ScopeKey == Scope && head.AttemptId == claim.AttemptId && head.AttemptDay == "2026-10-18" && head.Ttl == -1),
            new PartitionKey(Scope), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("2026-10-18")]
    [InlineData("2026-10-17")]
    public async Task Published_day_and_earlier_days_cannot_retry_after_restart(string requestedDay)
    {
        ReadHead(Head() with { AttemptDay = "2026-10-18", AttemptId = "published" });
        var restarted = new CosmosGroceryRecommendationCache(_container.Object, _clock);
        Assert.Null(await restarted.TryClaimAsync(Scope, requestedDay, default));
        VerifyNoHeadReplacement();
        _container.Verify(container => container.CreateItemAsync(It.IsAny<SnapshotHead>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unpublished_attempt_retries_after_persisted_cooldown_even_after_restart(bool hasPreviousSnapshot)
    {
        var previous = Head() with { AttemptDay = "2026-10-18", RetryAfter = FutureRetry() };
        if (!hasPreviousSnapshot) previous = previous with { Version = null, PageCount = 0, RetrievedAt = null };
        ReadHead(previous, "failed-attempt-etag");
        _clock.Advance(Duration.FromMinutes(5) - Duration.FromMilliseconds(1));
        Assert.Null(await _cache.TryClaimAsync(Scope, "2026-10-18", default));
        _clock.Advance(Duration.FromMilliseconds(1));
        var restarted = new CosmosGroceryRecommendationCache(_container.Object, _clock);
        var claim = await restarted.TryClaimAsync(Scope, "2026-10-18", default);
        Assert.NotNull(claim);
        Assert.Equal(previous.Version, claim.PreviousVersion);
        Assert.NotEqual(previous.AttemptId, claim.AttemptId);
        _container.Verify(container => container.ReplaceItemAsync(It.Is<SnapshotHead>(head => head.RetryAfter == FutureRetry()),
            "head", new PartitionKey(Scope), It.Is<ItemRequestOptions>(options => options.IfMatchEtag == "failed-attempt-etag"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Older_failed_head_without_cooldown_can_recover_today()
    {
        ReadHead(Head() with { AttemptDay = "2026-10-18", Version = null, PageCount = 0, RetrievedAt = null });
        Assert.NotNull(await _cache.TryClaimAsync(Scope, "2026-10-18", default));
    }

    [Fact]
    public async Task New_daily_boundary_does_not_bypass_active_attempt_cooldown()
    {
        ReadHead(Head() with { RetryAfter = FutureRetry() });
        Assert.Null(await _cache.TryClaimAsync(Scope, "2026-10-18", default));
        VerifyNoHeadReplacement();
    }

    [Fact]
    public async Task Competing_same_day_retry_loses_conditional_write_race()
    {
        ReadHead(Head() with { AttemptDay = "2026-10-18", RetryAfter = _clock.GetCurrentInstant().ToDateTimeUtc() });
        _container.Setup(container => container.ReplaceItemAsync(It.IsAny<SnapshotHead>(), "head", It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(Failure(HttpStatusCode.PreconditionFailed));
        Assert.Null(await _cache.TryClaimAsync(Scope, "2026-10-18", default));
    }

    [Fact]
    public async Task Next_day_claim_preserves_previous_snapshot_and_compares_etag()
    {
        var previous = Head();
        ReadHead(previous, "previous-etag");
        var claim = await _cache.TryClaimAsync(Scope, "2026-10-18", default);
        Assert.Equal(previous.Version, claim.PreviousVersion);
        Assert.Equal(previous.PageCount, claim.PreviousPageCount);
        Assert.Equal(previous.RetrievedAt, claim.PreviousRetrievedAt);
        Assert.Equal("replaced-etag", claim.ETag);
        _container.Verify(container => container.ReplaceItemAsync(It.Is<SnapshotHead>(head => head.Version == previous.Version && head.AttemptId == claim.AttemptId && head.AttemptDay == "2026-10-18"),
            "head", new PartitionKey(Scope), It.Is<ItemRequestOptions>(options => options.IfMatchEtag == "previous-etag"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Concurrent_next_day_claim_loses_etag_race_without_retry()
    {
        ReadHead(Head());
        _container.Setup(container => container.ReplaceItemAsync(It.IsAny<SnapshotHead>(), "head", It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(Failure(HttpStatusCode.PreconditionFailed));
        Assert.Null(await _cache.TryClaimAsync(Scope, "2026-10-18", default));
        _container.Verify(container => container.ReplaceItemAsync(It.IsAny<SnapshotHead>(), "head", It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publication_writes_all_versioned_pages_before_fenced_head()
    {
        var calls = new List<string>();
        var pages = new List<CachePage>();
        _container.Setup(container => container.UpsertItemAsync(It.IsAny<CachePage>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .Callback((CachePage page, PartitionKey? partition, ItemRequestOptions options, CancellationToken token) => { calls.Add(page.Id); pages.Add(page); });
        _container.Setup(container => container.ReplaceItemAsync(It.IsAny<SnapshotHead>(), "head", It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("head"));
        var claim = Claim();
        await _cache.PublishAsync(claim, new[] { "first", "second" }, default);
        Assert.Equal(new[] { "attempt:0", "attempt:1", "head" }, calls);
        Assert.All(pages, page => { Assert.Equal(Scope, page.ScopeKey); Assert.Equal(604_800, page.Ttl); });
        _container.Verify(container => container.ReplaceItemAsync(It.Is<SnapshotHead>(head => head.Version == claim.AttemptId && head.PageCount == 2 && head.RetrievedAt == _clock.GetCurrentInstant().ToDateTimeUtc() && head.Ttl == -1),
            "head", new PartitionKey(Scope), It.Is<ItemRequestOptions>(options => options.IfMatchEtag == claim.ETag), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Partial_page_failure_never_publishes_head()
    {
        _container.SetupSequence(container => container.UpsertItemAsync(It.IsAny<CachePage>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response(new CachePage("attempt:0", Scope, "first", DateTime.UtcNow, 604_800)))
            .ThrowsAsync(Failure(HttpStatusCode.TooManyRequests));
        await Assert.ThrowsAsync<CosmosException>(() => _cache.PublishAsync(Claim(), new[] { "first", "second" }, default));
        VerifyNoHeadReplacement();
    }

    [Fact]
    public async Task Expired_claim_cannot_publish_over_new_head()
    {
        _container.Setup(container => container.ReplaceItemAsync(It.IsAny<SnapshotHead>(), "head", It.IsAny<PartitionKey?>(), It.Is<ItemRequestOptions>(options => options.IfMatchEtag == "claim-etag"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(Failure(HttpStatusCode.PreconditionFailed));
        var failure = await Assert.ThrowsAsync<CosmosException>(() => _cache.PublishAsync(Claim(), new[] { "orphaned page" }, default));
        Assert.Equal(HttpStatusCode.PreconditionFailed, failure.StatusCode);
        _container.Verify(container => container.ReplaceItemAsync(It.IsAny<SnapshotHead>(), "head", It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancellation_after_page_write_does_not_publish_head()
    {
        using var cancellation = new CancellationTokenSource();
        _container.Setup(container => container.UpsertItemAsync(It.IsAny<CachePage>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .Callback(() => cancellation.Cancel());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _cache.PublishAsync(Claim(), new[] { "page" }, cancellation.Token));
        VerifyNoHeadReplacement();
    }

    [Fact]
    public async Task Profile_expiry_is_fixed_at_write_and_cache_hits_never_write()
    {
        CachePage written = null;
        _container.Setup(container => container.UpsertItemAsync(It.IsAny<CachePage>(), new PartitionKey(Scope), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .Callback((CachePage page, PartitionKey? partition, ItemRequestOptions options, CancellationToken token) => written = page);
        await _cache.WriteProfileAsync(Scope, "profile", "payload", 900, default);
        Assert.Equal(_clock.GetCurrentInstant().Plus(Duration.FromSeconds(900)).ToDateTimeUtc(), written.ExpiresAt);
        ReadPage(written);
        _clock.Advance(Duration.FromSeconds(899));
        Assert.Equal("payload", await _cache.ReadProfileAsync(Scope, "profile", default));
        Assert.Equal("payload", await _cache.ReadProfileAsync(Scope, "profile", default));
        _clock.Advance(Duration.FromSeconds(1));
        Assert.Null(await _cache.ReadProfileAsync(Scope, "profile", default));
        _container.Verify(container => container.UpsertItemAsync(It.IsAny<CachePage>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Snapshot_reads_only_its_partition_and_published_page_version()
    {
        ReadHead(Head());
        ReadPage(new("published:0", Scope, "first", Future(), 604_800));
        ReadPage(new("published:1", Scope, "second", Future(), 604_800));
        var snapshot = await _cache.ReadSnapshotAsync(Scope, default);
        Assert.Equal(new[] { "first", "second" }, snapshot.Pages);
        Assert.Equal(Head().RetrievedAt, snapshot.RetrievedAt);
        Assert.Null(await _cache.ReadSnapshotAsync("other-family", default));
        _container.Verify(container => container.ReadItemAsync<CachePage>("published:0", new PartitionKey(Scope), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _container.Verify(container => container.ReadItemAsync<CachePage>("published:1", new PartitionKey(Scope), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _container.Verify(container => container.UpsertItemAsync(It.IsAny<CachePage>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_or_expired_page_never_returns_partial_snapshot(bool expired)
    {
        ReadHead(Head());
        ReadPage(new("published:0", Scope, "first", Future(), 604_800));
        if (expired) ReadPage(new("published:1", Scope, "second", _clock.GetCurrentInstant().ToDateTimeUtc(), 604_800));
        Assert.Null(await _cache.ReadSnapshotAsync(Scope, default));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Invalid_snapshot_page_count_is_rejected_before_writes(int pageCount)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _cache.PublishAsync(Claim(), Enumerable.Repeat("{}", pageCount).ToArray(), default));
        _container.Verify(container => container.UpsertItemAsync(It.IsAny<CachePage>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNoHeadReplacement();
    }

    [Fact]
    public async Task Utf8_byte_bound_prevents_oversized_profile_and_snapshot_writes()
    {
        var payload = new string('æ', 125_001);
        await Assert.ThrowsAsync<ArgumentException>(() => _cache.WriteProfileAsync(Scope, "profile", payload, 900, default));
        await Assert.ThrowsAsync<ArgumentException>(() => _cache.PublishAsync(Claim(), new[] { payload }, default));
        _container.Verify(container => container.UpsertItemAsync(It.IsAny<CachePage>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2_592_001)]
    public async Task Profile_retention_bounds_are_enforced(int ttl)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _cache.WriteProfileAsync(Scope, "profile", "{}", ttl, default));
    }

    [Fact]
    public void Production_serializer_round_trips_cache_documents_with_cosmos_field_names()
    {
        var serializer = new CosmosJsonNetSerializer(new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
        }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb));
        var page = new CachePage("profile", Scope, "{\"ingredients\":[\"æg\"]}", Future(), 900);
        using var stream = serializer.ToStream(page);
        using var reader = new StreamReader(stream, leaveOpen: true);
        var document = JObject.Parse(reader.ReadToEnd());
        Assert.Equal("profile", document.Value<string>("id"));
        Assert.Equal(Scope, document.Value<string>("scopeKey"));
        Assert.Equal(900, document.Value<int>("ttl"));
        Assert.NotNull(document["expiresAt"]);
        stream.Position = 0;
        Assert.Equal(page, serializer.FromStream<CachePage>(stream));
        var head = Head();
        Assert.Equal(head, serializer.FromStream<SnapshotHead>(serializer.ToStream(head)));
        var rateLimit = new RateLimitState("rate-limit", Scope, FutureRetry(), -1);
        Assert.Equal(rateLimit, serializer.FromStream<RateLimitState>(serializer.ToStream(rateLimit)));
    }

    private DateTime Future() => _clock.GetCurrentInstant().Plus(Duration.FromDays(7)).ToDateTimeUtc();
    private DateTime FutureRetry() => _clock.GetCurrentInstant().Plus(Duration.FromMinutes(5)).ToDateTimeUtc();
    private SnapshotHead Head() => new("head", Scope, "2026-10-17", "old-attempt", "published", 2, _clock.GetCurrentInstant().ToDateTimeUtc(), -1);
    private GroceryRefreshClaim Claim() => new(Scope, "attempt", "2026-10-18", "claim-etag", "published", 2, _clock.GetCurrentInstant().ToDateTimeUtc());
    private void ReadHead(SnapshotHead head, string etag = "head-etag") => _container.Setup(container => container.ReadItemAsync<SnapshotHead>("head", new PartitionKey(Scope), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(Response(head, etag));
    private void ReadPage(CachePage page) => _container.Setup(container => container.ReadItemAsync<CachePage>(page.Id, new PartitionKey(page.ScopeKey), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(Response(page));
    private void VerifyNoHeadReplacement() => _container.Verify(container => container.ReplaceItemAsync(It.IsAny<SnapshotHead>(), It.IsAny<string>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    private static CosmosException Failure(HttpStatusCode status) => new("test", status, 0, "test", 0);
    private static ItemResponse<T> Response<T>(T resource, string etag = "etag")
    {
        var response = new Mock<ItemResponse<T>>();
        response.SetupGet(response => response.Resource).Returns(resource);
        response.SetupGet(response => response.ETag).Returns(etag);
        return response.Object;
    }
}
