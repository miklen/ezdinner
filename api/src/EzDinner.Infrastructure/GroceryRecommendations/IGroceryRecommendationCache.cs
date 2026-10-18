namespace EzDinner.Infrastructure.GroceryRecommendations;

public interface IGroceryRecommendationCache
{
    Task<string?> ReadProfileAsync(string scope, string key, CancellationToken cancellationToken);
    Task WriteProfileAsync(string scope, string key, string payload, int ttlSeconds, CancellationToken cancellationToken);
}

public sealed record GrocerySnapshot(string[] Pages, DateTime RetrievedAt);
public sealed record GroceryRefreshClaim(string Scope, string AttemptId, string AttemptDay, string ETag,
    string? PreviousVersion, int PreviousPageCount, DateTime? PreviousRetrievedAt);

public interface IGrocerySnapshotStore
{
    Task<GrocerySnapshot?> ReadSnapshotAsync(string scope, CancellationToken cancellationToken);
    Task<GroceryRefreshClaim?> TryClaimAsync(string scope, string day, CancellationToken cancellationToken);
    Task PublishAsync(GroceryRefreshClaim claim, IReadOnlyList<string> pages, CancellationToken cancellationToken);
}
