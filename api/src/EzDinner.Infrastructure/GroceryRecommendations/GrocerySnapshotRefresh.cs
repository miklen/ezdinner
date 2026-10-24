using NodaTime;
using NodaTime.Text;
using Microsoft.Extensions.Logging;

namespace EzDinner.Infrastructure.GroceryRecommendations;

public sealed record GrocerySnapshotRead(GrocerySnapshot? Snapshot, bool IsStale);

public sealed class GrocerySnapshotRefresh(IGrocerySnapshotStore cache, IClock clock,
    ILogger<GrocerySnapshotRefresh>? logger = null, GroceryRecommendationOptions? options = null)
{
    private static readonly DateTimeZone Copenhagen = DateTimeZoneProviders.Tzdb["Europe/Copenhagen"];
    private readonly int refreshTimeoutSeconds = options?.RefreshTimeoutSeconds ?? 45;

    public async Task<GrocerySnapshotRead> ReadAsync(string scope,
        Func<CancellationToken, Task<IReadOnlyList<string>>> acquire, CancellationToken cancellationToken)
    {
        var now = clock.GetCurrentInstant().InZone(Copenhagen);
        var snapshot = await cache.ReadSnapshotAsync(scope, cancellationToken);
        var boundaryDate = now.TimeOfDay < new LocalTime(5, 0) ? now.Date.PlusDays(-1) : now.Date;
        var boundary = boundaryDate.At(new LocalTime(5, 0)).InZoneStrictly(Copenhagen).ToInstant().ToDateTimeUtc();
        if (snapshot is not null && snapshot.RetrievedAt >= boundary) return new(snapshot, false);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(refreshTimeoutSeconds));
        var claim = await cache.TryClaimAsync(scope, LocalDatePattern.Iso.Format(boundaryDate), timeout.Token);
        if (claim is null)
        {
            logger?.LogInformation("Grocery refresh skipped for {Scope}: refresh already completed or an attempt is within its five-minute cooldown; cached snapshot available: {HasSnapshot}", scope, snapshot is not null);
            return new(snapshot, true);
        }
        try
        {
            var pages = await acquire(timeout.Token).WaitAsync(timeout.Token);
            await cache.PublishAsync(claim, pages, timeout.Token);
            logger?.LogInformation("Grocery refresh published for {Scope}: {PageCount} cache pages", scope, pages.Count);
            return new(new(pages.ToArray(), clock.GetCurrentInstant().ToDateTimeUtc()), false);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger?.LogWarning(exception, "Grocery refresh timed out for {Scope} after its {TimeoutSeconds}-second budget; a later request may retry after the five-minute attempt cooldown", scope, refreshTimeoutSeconds);
            return new(snapshot, true);
        }
        catch (OperationCanceledException)
        {
            logger?.LogInformation("Grocery refresh cancelled for {Scope}; a later request may retry after the five-minute attempt cooldown", scope);
            throw;
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception, "Grocery refresh failed for {Scope}; a later request may retry after the five-minute attempt cooldown; cached snapshot available: {HasSnapshot}", scope, snapshot is not null);
            return new(snapshot, true);
        }
    }
}
