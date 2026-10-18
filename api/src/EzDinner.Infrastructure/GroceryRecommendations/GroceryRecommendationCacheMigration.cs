using Microsoft.Azure.Cosmos;

namespace EzDinner.Infrastructure.GroceryRecommendations;

public static class GroceryRecommendationCacheMigration
{
    public static async Task EnsureAsync(Database database)
    {
        var response = await database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(CosmosGroceryRecommendationCache.ContainerName, "/scopeKey")
            { DefaultTimeToLive = -1 });
        var properties = response.Resource;
        if (properties.PartitionKeyPath != "/scopeKey")
            throw new InvalidOperationException(
                $"Expected {CosmosGroceryRecommendationCache.ContainerName} partition key /scopeKey. Got {properties.PartitionKeyPath}.");
        if (properties.DefaultTimeToLive == -1) return;

        properties.DefaultTimeToLive = -1;
        await response.Container.ReplaceContainerAsync(properties,
            new ContainerRequestOptions { IfMatchEtag = response.ETag });
    }
}
