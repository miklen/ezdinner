using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using EzDinner.Infrastructure;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.FamilyAggregate;
using System.Collections.Generic;
using EzDinner.Authorization.Core;  
using Casbin.Persist.Adapter.EFCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using EzDinner.Application.Commands.Authorization;
using EzDinner.Functions.Models.Migrate;

namespace EzDinner.Functions
{
    public class Migrate
    {
        private readonly ILogger<Migrate> _logger;
        private readonly IServiceProvider _provider;
        private readonly CosmosClient _cosmosClient;
        private readonly CasbinDbContext<string> _casbinContext;
        private readonly IConfiguration _configuration;

        public Migrate(ILogger<Migrate> logger, IServiceProvider provider, CosmosClient cosmosClient, CasbinDbContext<string> casbinContext, IConfiguration configuration)
        {
            _logger = logger;
            _provider = provider;
            _cosmosClient = cosmosClient;
            _casbinContext = casbinContext;
            _configuration = configuration;
        }
        
        [Function(nameof(Migrate))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "migrate")] HttpRequest req)
        {
            _logger.LogInformation("Migration started");
            _logger.LogInformation("Creating EzDinner database and repository collections");
            var database = await EnsureDatabaseAndCollectionsCreated(_configuration);
            _logger.LogInformation("Converting families to V2");
            await ConvertFamiliesToV2(database);
            _logger.LogInformation("Creating CasbinRules collection");
            await _casbinContext.Database.EnsureCreatedAsync();
            _logger.LogInformation("Updating family roles and permissions");
            await UpdateFamiliesPermissions(database);

            _logger.LogInformation("Migration done");
            return new OkResult();
        }

        private async Task UpdateFamiliesPermissions(Database database)
        {
            var families = await GetFamilies<Family>(database);
            
            // We cannot inject the AuthzService in the ctor, as the Casbin Enforcer tries to load policies in it's ctor and the CasbinRules container may not exist yet.
            // This will cause the function to fail on startup never reaching the Db.EnsureCreated line that would have created the container for the Enforcer to work.
            // Therefore we load it using the provider here after the container has been created.
            var authz = _provider.GetService<IAuthzService>();
            var updateAuthorizationPoliciesCommand = new UpdateAuthorizationPoliciesCommand(authz);
            foreach (var family in families)
            {
                await updateAuthorizationPoliciesCommand.Handle(family);
            }
        }

        private static async Task<IEnumerable<TFamilyType>> GetFamilies<TFamilyType>(Database database)
        {
            var familiesContainer = database.GetContainer(FamilyRepository.CONTAINER);
            var query = new QueryDefinition("SELECT * FROM c");
            var familiesQuery = familiesContainer.GetItemQueryIterator<TFamilyType>(query);
            var families = new List<TFamilyType>();
            while (familiesQuery.HasMoreResults)
            {
                foreach (var family in await familiesQuery.ReadNextAsync())
                {
                    families.Add(family);
                }
            }
            return families;
        }

        private async Task<Database> EnsureDatabaseAndCollectionsCreated(IConfiguration configuration)
        {
            var dbName = configuration.GetValue<string>("CosmosDb:Database");
            var dbResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(dbName);
            var db = dbResponse.Database;
            await db.CreateContainerIfNotExistsAsync(new ContainerProperties(DishRepository.CONTAINER, $"/{nameof(Dish.PartitionKey).ToCamelCase()}"));
            await db.CreateContainerIfNotExistsAsync(new ContainerProperties(DinnerRepository.CONTAINER, $"/{nameof(Dinner.PartitionKey).ToCamelCase()}"));
            await db.CreateContainerIfNotExistsAsync(new ContainerProperties(FamilyRepository.CONTAINER, $"/{nameof(Family.PartitionKey).ToCamelCase()}"));
            return db;
        }

        private async Task ConvertFamiliesToV2(Database database)
        {
            var families = await GetFamilies<V1Family>(database);
            var famRepo = _provider.GetService<IFamilyRepository>();
            foreach (var family in families)
            {
                var converted = family.ConvertToV2();
                if (converted is null) continue;
                await famRepo.SaveAsync(converted);
            }
        }
    }

    public static class StringExtension
    {
        public static string ToCamelCase(this string str)
        {
            if (!string.IsNullOrEmpty(str) && str.Length > 1)
            {
                return char.ToLowerInvariant(str[0]) + str.Substring(1);
            }
            return str;
        }
    }
}
