using System.Text.Json;
using EzDinner.Application.Commands.Families;
using EzDinner.Core.DomainServices.DishRecommendations;
using EzDinner.Query.Core.DishRecommendationQueries;
using NodaTime;
using Microsoft.Extensions.Logging;

namespace EzDinner.Infrastructure.GroceryRecommendations;

public sealed class CachedGroceryOfferReader(PrejGroceryOfferSource source, GrocerySnapshotRefresh refresh,
    IGroceryStoreCatalog catalog, GroceryRecommendationOptions options, IClock clock,
    ILogger<CachedGroceryOfferReader>? logger = null) : IGroceryOfferReader, IDisposable
{
    private readonly SemaphoreSlim concurrency = new(2);

    public async Task<GroceryOffersResult> ReadAsync(IReadOnlyList<string> storeIds, CancellationToken cancellationToken)
    {
        var ids = storeIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var now = clock.GetCurrentInstant();
        if (ids.Length == 0 || !options.Enabled)
            return new([], new(GroceryCoverageStatus.Disabled, now, ids, ["source-disabled"]), new Dictionary<string, string>());
        var stores = await catalog.GetStoresAsync(cancellationToken);
        var names = stores.ToDictionary(store => store.Id, store => store.Name);
        using var sourceBudget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        sourceBudget.CancelAfter(TimeSpan.FromSeconds(9));
        var responses = await Task.WhenAll(ids.Select(async id =>
        {
            if (!options.CanAcquire || !names.ContainsKey(id))
                return new StoreRead([], GroceryCoverageStatus.Unavailable);
            var entered = false;
            try
            {
                await concurrency.WaitAsync(sourceBudget.Token);
                entered = true;
                var read = await refresh.ReadAsync($"retailer:{id}:chain", token => source.AcquireAsync(id, token), sourceBudget.Token);
                if (read.Snapshot is null) return new StoreRead([], GroceryCoverageStatus.Unavailable);
                var pages = read.Snapshot.Pages.Select(page => JsonSerializer.Deserialize<StoredGroceryOfferPage>(page)
                    ?? throw new InvalidOperationException("INVALID_OFFER_CACHE")).ToArray();
                var offers = pages.SelectMany(page => page.Offers).Select(offer => offer.ToDomain()).ToArray();
                if (offers.Any(offer => offer.StoreId != id)) throw new InvalidOperationException("WRONG_OFFER_SCOPE");
                var usable = offers.Where(offer => offer.IsUsable(now)).ToArray();
                var stale = read.IsStale || offers.Any(offer => offer.ObservedAt + Duration.FromHours(48) <= now);
                var status = pages.Any(page => page.IsPartial) ? GroceryCoverageStatus.Partial
                    : stale ? GroceryCoverageStatus.Stale : GroceryCoverageStatus.Fresh;
                return new StoreRead(usable, status, stale);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                logger?.LogWarning(exception, "Grocery offer read failed for {StoreId}", id);
                return new StoreRead([], GroceryCoverageStatus.Unavailable);
            }
            finally { if (entered) concurrency.Release(); }
        }));
        var statuses = responses.Select(response => response.Status).Distinct().ToArray();
        var coverage = statuses.Length == 1 ? statuses[0] : GroceryCoverageStatus.Partial;
        var limitations = statuses.Where(status => status != GroceryCoverageStatus.Fresh)
            .Select(status => "source-" + status.ToString().ToLowerInvariant())
            .Concat(responses.Any(response => response.IsStale) ? ["source-stale"] : Array.Empty<string>())
            .Distinct().ToArray();
        var storeCoverage = ids.Select((id, index) => new GroceryStoreCoverage(id,
            names.TryGetValue(id, out var name) ? name : id, responses[index].Status, responses[index].Offers.Length)).ToArray();
        return new(responses.SelectMany(response => response.Offers).ToArray(),
            new(coverage, now, ids, limitations) { Stores = storeCoverage }, names)
        {
            StoreFreshness = ids.Select((id, index) => (id, Freshness: responses[index].IsStale
                    ? GroceryCoverageStatus.Stale : responses[index].Status == GroceryCoverageStatus.Partial
                        ? GroceryCoverageStatus.Fresh : responses[index].Status))
                .ToDictionary(item => item.id, item => item.Freshness)
        };
    }

    public void Dispose() => concurrency.Dispose();
    private sealed record StoreRead(GroceryOfferValueObject[] Offers, GroceryCoverageStatus Status, bool IsStale = false);
}
