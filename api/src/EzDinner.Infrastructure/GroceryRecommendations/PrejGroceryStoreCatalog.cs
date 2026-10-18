using System.Text.Json;
using System.Text.RegularExpressions;
using EzDinner.Application.Commands.Families;
using Microsoft.Extensions.Logging;

namespace EzDinner.Infrastructure.GroceryRecommendations;

public sealed class PrejGroceryStoreCatalog(PrejHttpClient client, GrocerySnapshotRefresh refresh,
    GroceryRecommendationOptions options, ILogger<PrejGroceryStoreCatalog>? logger = null) : IGroceryStoreCatalog
{
    public async Task<IReadOnlyList<GroceryStoreDescriptor>> GetStoresAsync(CancellationToken cancellationToken = default)
    {
        var stores = Bootstrap();
        if (options.CatalogRefreshEnabled && !string.IsNullOrWhiteSpace(options.ApiKey))
        {
            try
            {
                var result = await refresh.ReadAsync("prej:chains", async token =>
                {
                    using var response = await client.GetAsync("/v1/chains", token);
                    return [JsonSerializer.Serialize(Parse(response.RootElement))];
                }, cancellationToken);
                if (result.Snapshot is not null)
                    stores = JsonSerializer.Deserialize<GroceryStoreDescriptor[]>(result.Snapshot.Pages.Single())
                        ?? throw new InvalidOperationException("INVALID_CHAIN_CACHE");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception)
            {
                logger?.LogWarning(exception, "Prej chain catalog read failed; using bundled catalog");
                stores = Bootstrap();
            }
        }
        return stores.Select(store => store with { IsAvailable = options.CanAcquire })
            .OrderBy(store => store.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static GroceryStoreDescriptor[] Parse(JsonElement response)
    {
        var rows = response.GetProperty("chains").EnumerateArray().ToArray();
        if (rows.Length is < 1 or > 100) throw new InvalidOperationException("INVALID_CHAIN_COUNT");
        var stores = rows.Select(row => new GroceryStoreDescriptor(
            row.GetProperty("slug").GetString() ?? "", row.GetProperty("name").GetString() ?? "",
            false, row.GetProperty("is_border_trade").GetBoolean(), row.GetProperty("is_flyer_only").GetBoolean())).ToArray();
        if (stores.Any(store => !Regex.IsMatch(store.Id, "^[a-z0-9][a-z0-9-]{0,63}$") ||
            string.IsNullOrWhiteSpace(store.Name) || store.Name.Length > 100) || stores.Select(store => store.Id).Distinct().Count() != stores.Length)
            throw new InvalidOperationException("INVALID_CHAIN_CATALOG");
        return stores;
    }

    private static GroceryStoreDescriptor[] Bootstrap()
    {
        using var stream = typeof(PrejGroceryStoreCatalog).Assembly.GetManifestResourceStream(
            "EzDinner.Infrastructure.GroceryRecommendations.prej-chains.json")
            ?? throw new InvalidOperationException("MISSING_CHAIN_BOOTSTRAP");
        using var response = JsonDocument.Parse(stream);
        return Parse(response.RootElement);
    }
}
