using EzDinner.Core.Aggregates.WishlistAggregate;
using EzDinner.Query.Core.WishlistQueries;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EzDinner.Infrastructure
{
    public class WishlistRepository : IWishlistRepository, IWishlistQueryRepository
    {
        private readonly Container _container;
        public const string CONTAINER = "Wishlist";

        public WishlistRepository(CosmosClient client, IConfiguration configuration)
        {
            _container = client.GetContainer(configuration.GetValue<string>("CosmosDb:Database"), CONTAINER);
        }

        public async Task<IReadOnlyList<WishlistItem>> GetActiveAsync(Guid familyId)
        {
            var sql = new QueryDefinition(
                "SELECT * FROM c WHERE c.familyId = @familyId")
                .WithParameter("@familyId", familyId.ToString());
            return await QueryAsync(familyId, sql);
        }

        public async Task<IReadOnlyList<WishlistItem>> GetActiveAsync(Guid familyId, Instant now)
        {
            // Returns all items for the family; expiry filtering is done in the query layer
            return await GetActiveAsync(familyId);
        }

        public async Task<WishlistItem?> GetByDishAsync(Guid familyId, Guid dishId)
        {
            var sql = new QueryDefinition(
                "SELECT * FROM c WHERE c.familyId = @familyId AND c.dishId = @dishId")
                .WithParameter("@familyId", familyId.ToString())
                .WithParameter("@dishId", dishId.ToString());

            var iterator = _container.GetItemQueryIterator<WishlistItem>(sql,
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(familyId.ToString()) });

            while (iterator.HasMoreResults)
            {
                foreach (var item in await iterator.ReadNextAsync())
                    return item;
            }
            return null;
        }

        public Task AddAsync(WishlistItem item)
        {
            return _container.CreateItemAsync(item, new PartitionKey(item.FamilyId.ToString()));
        }

        public Task UpdateAsync(WishlistItem item)
        {
            return _container.UpsertItemAsync(item, new PartitionKey(item.FamilyId.ToString()));
        }

        public Task DeleteAsync(WishlistItem item)
        {
            return _container.DeleteItemAsync<WishlistItem>(
                item.Id.ToString(),
                new PartitionKey(item.FamilyId.ToString()));
        }

        private async Task<IReadOnlyList<WishlistItem>> QueryAsync(Guid familyId, QueryDefinition query)
        {
            var iterator = _container.GetItemQueryIterator<WishlistItem>(query,
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(familyId.ToString()) });
            var results = new List<WishlistItem>();
            while (iterator.HasMoreResults)
            {
                foreach (var item in await iterator.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }
    }
}
