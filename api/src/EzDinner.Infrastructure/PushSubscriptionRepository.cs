using EzDinner.Core.Aggregates.PushSubscriptionAggregate;
using EzDinner.Query.Core.PushSubscriptionQueries;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EzDinner.Infrastructure
{
    public class PushSubscriptionRepository : IPushSubscriptionRepository, IPushSubscriptionQueryRepository
    {
        private readonly Container _container;
        public const string CONTAINER = "PushSubscriptions";

        public PushSubscriptionRepository(CosmosClient client, IConfiguration configuration)
        {
            _container = client.GetContainer(configuration.GetValue<string>("CosmosDb:Database"), CONTAINER);
        }

        public Task SaveAsync(PushSubscription subscription)
        {
            return _container.UpsertItemAsync(subscription, new PartitionKey(subscription.PartitionKey.ToString()));
        }

        public async Task DeleteByUserIdAsync(Guid userId)
        {
            var existing = await GetByUserIdAsync(userId);
            if (existing is null) return;
            await DeleteAsync(existing);
        }

        public Task DeleteAsync(PushSubscription subscription)
        {
            return _container.DeleteItemAsync<PushSubscription>(
                subscription.Id.ToString(),
                new PartitionKey(subscription.FamilyId.ToString()));
        }

        public async Task<IEnumerable<PushSubscription>> GetByFamilyIdAsync(Guid familyId)
        {
            var sql = new QueryDefinition("SELECT * FROM c WHERE c.familyId = @familyId")
                .WithParameter("@familyId", familyId.ToString());
            var iterator = _container.GetItemQueryIterator<PushSubscription>(sql, requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(familyId.ToString())
            });

            var results = new List<PushSubscription>();
            while (iterator.HasMoreResults)
            {
                foreach (var item in await iterator.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public async Task<PushSubscription?> GetByUserIdAsync(Guid userId)
        {
            var sql = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
                .WithParameter("@userId", userId.ToString());
            var iterator = _container.GetItemQueryIterator<PushSubscription>(sql);

            while (iterator.HasMoreResults)
            {
                foreach (var item in await iterator.ReadNextAsync())
                    return item;
            }
            return null;
        }

        public async Task<bool> HasActiveSubscriptionAsync(Guid userId)
        {
            var subscription = await GetByUserIdAsync(userId);
            return subscription is not null;
        }

        public async Task<IEnumerable<Guid>> GetAllFamilyIdsAsync()
        {
            var sql = new QueryDefinition("SELECT DISTINCT VALUE c.familyId FROM c");
            var iterator = _container.GetItemQueryIterator<string>(sql);

            var familyIds = new List<Guid>();
            while (iterator.HasMoreResults)
            {
                foreach (var id in await iterator.ReadNextAsync())
                {
                    if (Guid.TryParse(id, out var guid))
                        familyIds.Add(guid);
                }
            }
            return familyIds;
        }
    }
}
