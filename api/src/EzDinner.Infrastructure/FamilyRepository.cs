using EzDinner.Core.Aggregates.FamilyAggregate;
using EzDinner.Core.Aggregates.UserAggregate;
using EzDinner.Query.Core.FamilyQueries;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public Task SaveAsync(Family family)
        {
            return _container.UpsertItemAsync(family);
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
