using EzDinner.Core.Aggregates.FamilyAggregate;
using EzDinner.Core.Aggregates.UserAggregate;
using EzDinner.Query.Core.FamilyQueries;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EzDinner.Infrastructure
{
    public class FamilyRepository : IFamilyRepository, IFamilyQueryRepository
    {
        private readonly CosmosClient _client;
        private readonly Container _container;
        public const string CONTAINER = "Families";

        public FamilyRepository(CosmosClient client, IConfiguration configuration)
        {
            _client = client;
            _container = _client.GetContainer(configuration.GetValue<string>("CosmosDb:Database"), CONTAINER);
        }

        public async Task SaveAsync(Family family)
        {
            try
            {
                await PatchFamilyAsync(family);
            }
            catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
            {
                await CreateFamilyAsync(family);
            }
        }

        private Task PatchFamilyAsync(Family family) => _container.PatchItemAsync<Family>(family.Id.ToString(), new PartitionKey(family.PartitionKey.ToString()),
            new[] { PatchOperation.Set("/name", family.Name), PatchOperation.Set("/familyMembers", family.FamilyMembers),
                PatchOperation.Set("/ownerId", family.OwnerId), PatchOperation.Set("/updatedDate", family.UpdatedDate) });

        private async Task CreateFamilyAsync(Family family)
        {
            try
            {
                await _container.CreateItemAsync(family, new PartitionKey(family.PartitionKey.ToString()));
            }
            catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
            {
                await PatchFamilyAsync(family);
            }
        }

        public async Task SaveGroceryStoresAsync(Family family)
        {
            try
            {
                await _container.PatchItemAsync<Family>(family.Id.ToString(), new PartitionKey(family.PartitionKey.ToString()),
                    new[] { PatchOperation.Set("/groceryStores", family.GroceryStores), PatchOperation.Set("/updatedDate", family.UpdatedDate) });
            }
            catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
            {
                throw new KeyNotFoundException("FAMILY_NOT_FOUND", exception);
            }
        }

        public async Task<IEnumerable<FamilyDetails>> GetFamiliesDetailsAsync(Guid userId)
        {
            var sql = $"SELECT VALUE c FROM c JOIN s in c.familyMembers WHERE CONTAINS(s.id, @userId)";
            var queryDefinition = new QueryDefinition(sql)
                .WithParameter("@userId", userId);
            var queryResultSetIterator = _container.GetItemQueryIterator<FamilyDetails>(queryDefinition);

            var families = new List<FamilyDetails>();
            while (queryResultSetIterator.HasMoreResults)
            {
                foreach (var family in await queryResultSetIterator.ReadNextAsync())
                {
                    families.Add(family);
                }
            }
            return families;
        }

        public async Task<FamilyDetails?> GetFamilyDetailsAsync(Guid familyId)
        {
            var sql = $"SELECT * FROM c WHERE c.id = @familyId";
            var queryDefinition = new QueryDefinition(sql)
                .WithParameter("@familyId", familyId);
            var queryResultSetIterator = _container.GetItemQueryIterator<FamilyDetails>(queryDefinition);

            while (queryResultSetIterator.HasMoreResults)
            {
                foreach (var family in await queryResultSetIterator.ReadNextAsync())
                {
                    return family;
                }
            }
            return null;
        }

        public async Task<Family?> GetFamily(Guid familyId)
        {
            var sql = "SELECT * FROM c WHERE c.id = @familyId";
            var queryDefinition = new QueryDefinition(sql).WithParameter("@familyId", familyId.ToString());
            var queryResultSetIterator = _container.GetItemQueryIterator<Family>(queryDefinition);

            while (queryResultSetIterator.HasMoreResults)
            {
                foreach (var family in await queryResultSetIterator.ReadNextAsync())
                {
                    return family;
                }
            }
            return null;
        }
    }
}
