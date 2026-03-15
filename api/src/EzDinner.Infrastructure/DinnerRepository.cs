using EzDinner.Core.Aggregates.DinnerAggregate;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzDinner.Infrastructure
{
    public class DinnerRepository : IDinnerRepository
    {
        private readonly CosmosClient _client;
        private readonly Container _container;
        public const string CONTAINER = "Dinners";

        public DinnerRepository(CosmosClient client, IConfiguration configuration)
        {
            _client = client;
            _container = _client.GetContainer(configuration.GetValue<string>("CosmosDb:Database"), CONTAINER);
        }

        public Task DeleteAsync(Dinner dinner)
        {
            return _container.DeleteItemAsync<Dinner>(dinner.Id.ToString(), new PartitionKey(dinner.PartitionKey.ToString()));
        }

        /// <summary>
        /// The serialization of DateTime is important when querying. Since CosmosDb stores datetimes as strings
        /// The format of the SQL query must match exactly the serialization format.
        /// 
        /// TODO: Store datetimes as UTC as that's what CosmosDb supports. https://docs.microsoft.com/en-us/azure/cosmos-db/working-with-dates
        /// 
        /// </summary>
        /// <param name="familyId"></param>
        /// <param name="localDate"></param>
        /// <returns></returns>
        public async Task<Dinner?> GetAsync(Guid familyId, LocalDate localDate)
        {
            var sql = $"SELECT * FROM c WHERE c.familyId = @familyId AND c.date = @date";
            var queryDefinition = new QueryDefinition(sql)
                .WithParameter("@familyId", familyId)
                .WithParameter("@date", localDate);
            var queryResultSetIterator = _container.GetItemQueryIterator<Dinner>(queryDefinition);

            while (queryResultSetIterator.HasMoreResults)
            {
                foreach (var dinner in await queryResultSetIterator.ReadNextAsync())
                {
                    return dinner;
                }
            }
            return null;
        }

        public async IAsyncEnumerable<Dinner> GetAsync(Guid familyId, LocalDate fromDate, LocalDate toDate)
        {
            var sql = $"SELECT * FROM c WHERE c.familyId = @familyId AND c.date >= @fromDate and c.date <= @toDate ORDER BY c.date";
            var queryDefinition = new QueryDefinition(sql)
                .WithParameter("@familyId", familyId)
                .WithParameter("@fromDate", fromDate)
                .WithParameter("@toDate", toDate);
            var queryResultSetIterator = _container.GetItemQueryIterator<Dinner>(queryDefinition);

            while (queryResultSetIterator.HasMoreResults)
            {
                foreach (var dinner in await queryResultSetIterator.ReadNextAsync())
                {
                    yield return dinner;
                }
            }
        }

        public async IAsyncEnumerable<Dinner> GetAsync(Guid familyId, Guid dishId)
        {
            var sql = $"SELECT VALUE c FROM c JOIN s in c.menu WHERE c.familyId = @familyId AND CONTAINS(s.dishId, @dishId)";
            var queryDefinition = new QueryDefinition(sql)
                .WithParameter("@familyId", familyId)
                .WithParameter("@dishId", dishId);
   
            var queryResultSetIterator = _container.GetItemQueryIterator<Dinner>(queryDefinition);

            while (queryResultSetIterator.HasMoreResults)
            {
                foreach (var dinner in await queryResultSetIterator.ReadNextAsync())
                {
                    yield return dinner;
                }
            }
        }

        public async IAsyncEnumerable<string> GetOptOutReasonsAsync(Guid familyId)
        {
            var sql = "SELECT VALUE c.optOut.reason FROM c WHERE c.familyId = @familyId AND c.optOut != null";
            var queryDefinition = new QueryDefinition(sql)
                .WithParameter("@familyId", familyId);
            var queryResultSetIterator = _container.GetItemQueryIterator<string>(queryDefinition);

            while (queryResultSetIterator.HasMoreResults)
            {
                foreach (var reason in await queryResultSetIterator.ReadNextAsync())
                {
                    yield return reason;
                }
            }
        }

        public Task SaveAsync(Dinner dinner)
        {
            return _container.UpsertItemAsync(dinner);
        }
    }
}
