using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Query.Core.DishQueries;
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
    public class DishRepository : IDishRepository, IDishQueryRepository
    {
        private readonly CosmosClient _client;
        private readonly Container _container;
        public const string CONTAINER = "Dishes";

        public DishRepository(CosmosClient client, IConfiguration configuration)
        {
            _client = client;
            _container = _client.GetContainer(configuration.GetValue<string>("CosmosDb:Database"), CONTAINER);
        }

        public async Task<IEnumerable<Dish>> GetDishesAsync(Guid familyId, bool includeArchived = false)
        {
            var sql = "SELECT * FROM c WHERE c.familyId = @familyId AND (c.deleted = @deleted OR IS_DEFINED(c.deleted) = false)";
            if (!includeArchived)
                sql += " AND (c.isArchived = false OR IS_DEFINED(c.isArchived) = false)";
            var queryDefinition = new QueryDefinition(sql)
                .WithParameter("@familyId", familyId.ToString())
                .WithParameter("@deleted", false);
            var queryResultSetIterator = _container.GetItemQueryIterator<Dish>(queryDefinition);
            var dishes = new List<Dish>();

            while (queryResultSetIterator.HasMoreResults)
            {
                foreach (var family in await queryResultSetIterator.ReadNextAsync())
                {
                    dishes.Add(family);
                }
            }
            return dishes;
        }

        public Task SaveAsync(Dish dish)
        {
            return _container.UpsertItemAsync(dish);
        }

        public async Task<Dish?> GetDishAsync(Guid dishId)
        {
            var sql = new QueryDefinition($"SELECT * FROM c WHERE c.id = @dishId")
                .WithParameter("@dishId", dishId.ToString());
            var resultIterator = _container.GetItemQueryIterator<Dish>(sql);
            while (resultIterator.HasMoreResults)
            {
                foreach (var dish in await resultIterator.ReadNextAsync()) return dish;
            }
            return null;
        }
    }
}
