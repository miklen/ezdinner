using System.Net;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using NodaTime;

namespace EzDinner.Infrastructure.GroceryRecommendations;

public sealed class CosmosGroceryRecommendationCache : IGroceryRecommendationCache, IGrocerySnapshotStore
{
    public const string ContainerName = "GroceryRecommendationCache";
    public static readonly Duration FailedAttemptCooldown = Duration.FromMinutes(5);
    private readonly Container container;
    private readonly IClock clock;

    public CosmosGroceryRecommendationCache(CosmosClient client, IConfiguration configuration, IClock clock)
        : this(client.GetContainer(configuration["CosmosDb:Database"], ContainerName), clock) { }

    public CosmosGroceryRecommendationCache(Container container, IClock clock)
    {
        this.container = container;
        this.clock = clock;
    }

    public async Task<string?> ReadProfileAsync(string scope, string key, CancellationToken cancellationToken)
    {
        var response = await ReadAsync<CachePage>(scope, key, cancellationToken);
        return response is not null && response.Resource.ExpiresAt > clock.GetCurrentInstant().ToDateTimeUtc()
            ? response.Resource.Payload : null;
    }

    public Task WriteProfileAsync(string scope, string key, string payload, int ttlSeconds, CancellationToken cancellationToken)
    {
        if (ttlSeconds is < 1 or > 2_592_000 || Encoding.UTF8.GetByteCount(payload) > 250_000)
            throw new ArgumentException("INVALID_PROFILE_CACHE_BOUNDS");
        var page = new CachePage(key, scope, payload, clock.GetCurrentInstant().Plus(Duration.FromSeconds(ttlSeconds)).ToDateTimeUtc(), ttlSeconds);
        return container.UpsertItemAsync(page, new PartitionKey(scope), cancellationToken: cancellationToken);
    }

    public async Task<GrocerySnapshot?> ReadSnapshotAsync(string scope, CancellationToken cancellationToken)
    {
        var response = await ReadAsync<SnapshotHead>(scope, "head", cancellationToken);
        var head = response?.Resource;
        if (head?.Version is null || head.RetrievedAt is null || head.PageCount is < 1 or > 100) return null;
        var pages = new List<string>();
        for (var index = 0; index < head.PageCount; index++)
        {
            var page = await ReadProfileAsync(scope, $"{head.Version}:{index}", cancellationToken);
            if (page is null) return null;
            pages.Add(page);
        }
        return new(pages.ToArray(), head.RetrievedAt.Value);
    }

    public async Task<GroceryRefreshClaim?> TryClaimAsync(string scope, string day, CancellationToken cancellationToken)
    {
        var prior = await ReadAsync<SnapshotHead>(scope, "head", cancellationToken);
        var now = clock.GetCurrentInstant();
        if (prior is not null && !CanClaim(prior.Resource, day, now.ToDateTimeUtc())) return null;
        var attempt = Guid.NewGuid().ToString("N");
        var head = new SnapshotHead("head", scope, day, attempt, prior?.Resource.Version,
            prior?.Resource.PageCount ?? 0, prior?.Resource.RetrievedAt, -1, now.Plus(FailedAttemptCooldown).ToDateTimeUtc());
        try
        {
            var response = prior is null
                ? await container.CreateItemAsync(head, new PartitionKey(scope), cancellationToken: cancellationToken)
                : await container.ReplaceItemAsync(head, "head", new PartitionKey(scope),
                    new ItemRequestOptions { IfMatchEtag = prior.ETag }, cancellationToken);
            return new(scope, attempt, day, response.ETag, head.Version, head.PageCount, head.RetrievedAt);
        }
        catch (CosmosException exception) when (exception.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed)
        { return null; }
    }

    private static bool CanClaim(SnapshotHead head, string day, DateTime now)
    {
        var order = string.CompareOrdinal(head.AttemptDay, day);
        if (order > 0 || head.RetryAfter > now) return false;
        return order < 0 || head.AttemptId != head.Version;
    }

    public async Task PublishAsync(GroceryRefreshClaim claim, IReadOnlyList<string> pages, CancellationToken cancellationToken)
    {
        if (pages.Count is < 1 or > 100 || pages.Any(page => Encoding.UTF8.GetByteCount(page) > 250_000))
            throw new ArgumentException("INVALID_SNAPSHOT_BOUNDS");
        for (var index = 0; index < pages.Count; index++)
            await WriteProfileAsync(claim.Scope, $"{claim.AttemptId}:{index}", pages[index], 604_800, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var head = new SnapshotHead("head", claim.Scope, claim.AttemptDay, claim.AttemptId,
            claim.AttemptId, pages.Count, clock.GetCurrentInstant().ToDateTimeUtc(), -1);
        await container.ReplaceItemAsync(head, "head", new PartitionKey(claim.Scope),
            new ItemRequestOptions { IfMatchEtag = claim.ETag }, cancellationToken);
    }

    private async Task<ItemResponse<T>?> ReadAsync<T>(string scope, string id, CancellationToken cancellationToken)
    {
        try { return await container.ReadItemAsync<T>(id, new PartitionKey(scope), cancellationToken: cancellationToken); }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound) { return null; }
    }

    public sealed record CachePage(string Id, string ScopeKey, string Payload, DateTime ExpiresAt, int Ttl);
    public sealed record SnapshotHead(string Id, string ScopeKey, string AttemptDay, string AttemptId,
        string? Version, int PageCount, DateTime? RetrievedAt, int Ttl, DateTime? RetryAfter = null);
}
