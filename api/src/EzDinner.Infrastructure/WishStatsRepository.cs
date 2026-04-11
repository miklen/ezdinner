using EzDinner.Core.Aggregates.WishlistAggregate;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace EzDinner.Infrastructure
{
    public class WishStats
    {
        public string Id { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public Guid FamilyId { get; set; }
        public int WishesAdded { get; set; }
        public int WishesGranted { get; set; }

        public static string MakeId(Guid familyId, Guid userId) => $"{familyId}:{userId}";
    }

    public class WishStatsRepository : IWishStatsRepository
    {
        private readonly Container _container;
        public const string CONTAINER = "WishStats";

        public WishStatsRepository(CosmosClient client, IConfiguration configuration)
        {
            _container = client.GetContainer(configuration.GetValue<string>("CosmosDb:Database"), CONTAINER);
        }

        public async Task IncrementWishesAddedAsync(Guid familyId, Guid userId)
        {
            var stats = await GetOrCreateAsync(familyId, userId);
            stats.WishesAdded++;
            await _container.UpsertItemAsync(stats, new PartitionKey(familyId.ToString()));
        }

        public async Task IncrementWishesGrantedAsync(Guid familyId, Guid userId)
        {
            var stats = await GetOrCreateAsync(familyId, userId);
            stats.WishesGranted++;
            await _container.UpsertItemAsync(stats, new PartitionKey(familyId.ToString()));
        }

        private async Task<WishStats> GetOrCreateAsync(Guid familyId, Guid userId)
        {
            var id = WishStats.MakeId(familyId, userId);
            try
            {
                var response = await _container.ReadItemAsync<WishStats>(id, new PartitionKey(familyId.ToString()));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new WishStats
                {
                    Id = id,
                    UserId = userId,
                    FamilyId = familyId,
                    WishesAdded = 0,
                    WishesGranted = 0,
                };
            }
        }
    }
}
