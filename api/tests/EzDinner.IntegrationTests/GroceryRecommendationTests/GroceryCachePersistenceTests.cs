using System.Net;
using EzDinner.Infrastructure.GroceryRecommendations;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NodaTime;
using Xunit;

namespace EzDinner.IntegrationTests.GroceryRecommendationTests;

public class GroceryCachePersistenceTests(StartupFixture startup) : IClassFixture<StartupFixture>
{
    [Fact]
    public Task Rate_limit_survives_restart_and_concurrent_updates_preserve_the_longest_deadline() => InScope(async (container, scope) =>
    {
        var clock = SystemClock.Instance;
        var first = new CosmosGroceryRecommendationCache(container, clock);
        var second = new CosmosGroceryRecommendationCache(container, clock);
        var now = clock.GetCurrentInstant().ToDateTimeUtc();
        var later = now.AddHours(12);
        await Task.WhenAll(first.ExtendRateLimitAsync(scope, now.AddMinutes(2), default),
            second.ExtendRateLimitAsync(scope, later, default));
        var restarted = new CosmosGroceryRecommendationCache(container, clock);
        Assert.Equal(later, await restarted.ReadRateLimitAsync(scope, default));
        await restarted.ExtendRateLimitAsync(scope, now.AddMinutes(5), default);
        Assert.Equal(later, await first.ReadRateLimitAsync(scope, default));
        Assert.Null(await restarted.ReadRateLimitAsync(scope + ":other-key", default));
        var persisted = await container.ReadItemAsync<JObject>("rate-limit", new PartitionKey(scope));
        Assert.Equal(-1, persisted.Resource.Value<int>("ttl"));
        Assert.Equal(scope, persisted.Resource.Value<string>("scopeKey"));
    });

    [Fact]
    public Task Competing_instances_share_cooldown_and_restart_can_recover_failed_day() => InScope(async (container, scope) =>
    {
        var clock = new AdjustableClock(SystemClock.Instance.GetCurrentInstant());
        var first = new CosmosGroceryRecommendationCache(container, clock);
        var second = new CosmosGroceryRecommendationCache(container, clock);
        var attempts = await Task.WhenAll(first.TryClaimAsync(scope, "2026-10-18", default), second.TryClaimAsync(scope, "2026-10-18", default));
        Assert.Single(attempts, attempt => attempt is not null);
        var restarted = new CosmosGroceryRecommendationCache(container, clock);
        Assert.Null(await restarted.TryClaimAsync(scope, "2026-10-18", default));
        Assert.Null(await restarted.ReadSnapshotAsync(scope, default));
        clock.Advance(Duration.FromMinutes(5));
        var retry = await restarted.TryClaimAsync(scope, "2026-10-18", default);
        Assert.NotNull(retry);
        await restarted.PublishAsync(retry, new[] { "recovered" }, default);
        Assert.Null(await first.TryClaimAsync(scope, "2026-10-18", default));
        var persisted = await container.ReadItemAsync<JObject>("head", new PartitionKey(scope));
        Assert.Equal(-1, persisted.Resource.Value<int>("ttl"));
        Assert.Equal(scope, persisted.Resource.Value<string>("scopeKey"));
    });

    [Fact]
    public Task Published_pages_restore_after_restart_and_are_isolated_by_partition() => InScope(async (container, scope) =>
    {
        var cache = new CosmosGroceryRecommendationCache(container, SystemClock.Instance);
        var claim = await cache.TryClaimAsync(scope, "2026-10-18", default);
        var pages = new[] { "{\"offers\":[\"æg\"]}", "{\"offers\":[\"kylling\"]}" };
        await cache.PublishAsync(claim, pages, default);
        var restarted = new CosmosGroceryRecommendationCache(container, SystemClock.Instance);
        var snapshot = await restarted.ReadSnapshotAsync(scope, default);
        Assert.Equal(pages, snapshot.Pages);
        Assert.Null(await restarted.ReadSnapshotAsync(scope + ":different", default));
        var page = await container.ReadItemAsync<JObject>(claim.AttemptId + ":0", new PartitionKey(scope));
        Assert.Equal(604_800, page.Resource.Value<int>("ttl"));
        Assert.Equal(scope, page.Resource.Value<string>("scopeKey"));
        await container.DeleteItemAsync<JObject>(claim.AttemptId + ":1", new PartitionKey(scope));
        Assert.Null(await restarted.ReadSnapshotAsync(scope, default));
    });

    [Fact]
    public Task Older_claim_is_fenced_and_cannot_replace_previous_complete_snapshot() => InScope(async (container, scope) =>
    {
        var clock = new AdjustableClock(SystemClock.Instance.GetCurrentInstant());
        var cache = new CosmosGroceryRecommendationCache(container, clock);
        var initial = await cache.TryClaimAsync(scope, "2026-10-18", default);
        await cache.PublishAsync(initial, new[] { "previous complete snapshot" }, default);
        var staleClaim = await cache.TryClaimAsync(scope, "2026-10-19", default);
        clock.Advance(Duration.FromMinutes(5));
        var nextClaim = await cache.TryClaimAsync(scope, "2026-10-19", default);
        var failure = await Assert.ThrowsAsync<CosmosException>(() => cache.PublishAsync(staleClaim, new[] { "obsolete writer" }, default));
        Assert.Equal(HttpStatusCode.PreconditionFailed, failure.StatusCode);
        Assert.Equal(new[] { "previous complete snapshot" }, (await cache.ReadSnapshotAsync(scope, default)).Pages);
        await cache.PublishAsync(nextClaim, new[] { "new complete snapshot" }, default);
        Assert.Equal(new[] { "new complete snapshot" }, (await cache.ReadSnapshotAsync(scope, default)).Pages);
    });

    [Fact]
    public Task Profile_reads_preserve_etag_and_fixed_expiry() => InScope(async (container, scope) =>
    {
        var clock = new AdjustableClock(SystemClock.Instance.GetCurrentInstant());
        var cache = new CosmosGroceryRecommendationCache(container, clock);
        await cache.WriteProfileAsync(scope, "profile", "{\"ingredients\":[]}", 900, default);
        var before = await container.ReadItemAsync<JObject>("profile", new PartitionKey(scope));
        clock.Advance(Duration.FromSeconds(899));
        Assert.Equal("{\"ingredients\":[]}", await cache.ReadProfileAsync(scope, "profile", default));
        Assert.Equal("{\"ingredients\":[]}", await cache.ReadProfileAsync(scope, "profile", default));
        var after = await container.ReadItemAsync<JObject>("profile", new PartitionKey(scope));
        Assert.Equal(before.ETag, after.ETag);
        Assert.Equal(before.Resource["expiresAt"], after.Resource["expiresAt"]);
        clock.Advance(Duration.FromSeconds(1));
        Assert.Null(await cache.ReadProfileAsync(scope, "profile", default));
    });

    private async Task InScope(Func<Container, string, Task> check)
    {
        var client = startup.Provider.GetRequiredService<CosmosClient>();
        var configuration = startup.Provider.GetRequiredService<IConfiguration>();
        var database = await client.CreateDatabaseIfNotExistsAsync(configuration["CosmosDb:Database"]);
        var response = await database.Database.CreateContainerIfNotExistsAsync(new ContainerProperties(CosmosGroceryRecommendationCache.ContainerName, "/scopeKey") { DefaultTimeToLive = -1 });
        var scope = "integration:" + Guid.NewGuid().ToString("N");
        try { await check(response.Container, scope); }
        finally
        {
            var query = new QueryDefinition("SELECT VALUE c.id FROM c WHERE c.scopeKey = @scope").WithParameter("@scope", scope);
            using var iterator = response.Container.GetItemQueryIterator<string>(query, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(scope) });
            while (iterator.HasMoreResults)
                foreach (var id in await iterator.ReadNextAsync())
                    await response.Container.DeleteItemAsync<JObject>(id, new PartitionKey(scope));
        }
    }

    private sealed class AdjustableClock(Instant now) : IClock
    {
        public Instant GetCurrentInstant() => now;
        public void Advance(Duration duration) => now += duration;
    }
}
