using System;
using System.Threading.Tasks;
using EzDinner.Core.Aggregates.FamilyAggregate;
using EzDinner.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EzDinner.IntegrationTests.FamilyRepositoryTests;

public class GroceryStoresPersistenceTests : IClassFixture<StartupFixture>
{
    private readonly IFamilyRepository _families;
    private readonly CosmosClient _client;
    private readonly string _database;

    public GroceryStoresPersistenceTests(StartupFixture startup)
    {
        _families = startup.Provider.GetRequiredService<IFamilyRepository>();
        _client = startup.Provider.GetRequiredService<CosmosClient>();
        _database = startup.Provider.GetRequiredService<IConfiguration>().GetValue<string>("CosmosDb:Database");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Concurrent_membership_and_store_writers_preserve_each_others_changes(bool storeWritesFirst)
    {
        var database = await _client.CreateDatabaseIfNotExistsAsync(_database);
        var response = await database.Database.CreateContainerIfNotExistsAsync(new ContainerProperties(FamilyRepository.CONTAINER, "/partitionKey"));
        var family = Family.CreateNew(Guid.NewGuid(), "Grocery concurrency test");
        family.ReplaceGroceryStores(new[] { "netto" }, new[] { "netto" });
        await _families.SaveAsync(family);
        try
        {
            var memberWriter = await _families.GetFamily(family.Id);
            var storeWriter = await _families.GetFamily(family.Id);
            Assert.Equal(new[] { "netto" }, storeWriter.GroceryStores);
            var memberId = Guid.NewGuid();
            memberWriter.InviteFamilyMember(memberId);
            storeWriter.ReplaceGroceryStores(new[] { "foetex" }, new[] { "foetex" });
            if (storeWritesFirst)
            {
                await _families.SaveGroceryStoresAsync(storeWriter);
                await _families.SaveAsync(memberWriter);
            }
            else
            {
                await _families.SaveAsync(memberWriter);
                await _families.SaveGroceryStoresAsync(storeWriter);
            }
            var result = await _families.GetFamily(family.Id);
            Assert.Equal(new[] { "foetex" }, result.GroceryStores);
            Assert.Contains(result.FamilyMembers, member => member.Id == memberId);
            Assert.Equal(family.CreatedDate, result.CreatedDate);
        }
        finally
        {
            await response.Container.DeleteItemAsync<Family>(family.Id.ToString(), new PartitionKey(family.Id.ToString()));
        }
    }
}
