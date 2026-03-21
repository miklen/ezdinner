using Anthropic.SDK;
using Azure.Identity;
using Casbin;
using Casbin.Persist.Adapter.EFCore;
using Casbin.Model;
using EzDinner.Application.Commands.Dishes;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.FamilyAggregate;
using EzDinner.Core.Aggregates.UserAggregate;
using EzDinner.Infrastructure.Models.Json;
using EzDinner.Query.Core.DishQueries;
using EzDinner.Query.Core.FamilyQueries;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace EzDinner.Infrastructure
{
    public static class Setup
    {
        public static IServiceCollection RegisterMsGraph(this IServiceCollection services, IConfigurationSection adConfig)
        {
            var credential = new ClientSecretCredential(
                adConfig.GetValue<string>("TenantId"),
                adConfig.GetValue<string>("ClientId"),
                adConfig.GetValue<string>("ClientSecret"));
            var graphClient = new GraphServiceClient(credential);
            var tenantDomain = adConfig.GetValue<string>("Domain")!;

            services.AddSingleton(graphClient);
            services.AddSingleton<IUserRepository>(new UserRepository(graphClient, tenantDomain));
            return services;
        }

        public static IServiceCollection RegisterCosmosDb(this IServiceCollection services, IConfigurationSection section)
        {
            services.AddSingleton(_ => new CosmosClientBuilder(section.GetValue<string>("ConnectionString"))
                .WithCustomSerializer(new CosmosJsonNetSerializer(new JsonSerializerSettings()
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    }
                }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)))
                .Build());
            return services;
        }


        public static IServiceCollection RegisterCasbin(this IServiceCollection services, IConfigurationSection section)
        {
            var connStrParts = section.GetValue<string>("ConnectionString").Split(';');
            var accountEndpoint = connStrParts[0].Substring(connStrParts[0].IndexOf('=') + 1);
            var accountKey = connStrParts[1].Substring(connStrParts[1].IndexOf('=') + 1);
            var options = new DbContextOptionsBuilder<CasbinDbContext<string>>()
              .UseCosmos(accountEndpoint, accountKey, databaseName: section.GetValue<string>("Database"))
              .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CosmosEventId.SyncNotSupported))
              .Options;
            services.AddSingleton(_ => new CasbinDbContext<string>(options, new CasbinEntityConfiguration()));
            services.AddSingleton(s => new CasbinCosmosAdapter(s.GetRequiredService<CasbinDbContext<string>>()));
            services.AddSingleton(s => new Enforcer(AuthzRepository.GetRbacWithDomainsModel(), s.GetRequiredService<CasbinCosmosAdapter>()));
            return services;
        }

        public static IServiceCollection RegisterRepositories(this IServiceCollection services)
        {
            services.AddScoped<IFamilyRepository, FamilyRepository>();
            services.AddScoped<IDishRepository, DishRepository>();
            services.AddScoped<IDishQueryRepository, DishRepository>();
            services.AddScoped<IDinnerRepository, DinnerRepository>();
            services.AddScoped<IFamilyQueryRepository, FamilyRepository>();
            services.AddSingleton<IAuthzRepository, AuthzRepository>();
            return services;
        }

        public static IServiceCollection RegisterEnrichment(this IServiceCollection services, IConfiguration configuration)
        {
            var apiKey = configuration.GetValue<string>("Anthropic:ApiKey")!;
            services.AddSingleton(_ => new AnthropicClient(new APIAuthentication(apiKey)));
            services.AddScoped<IDishEnrichmentProvider, AnthropicEnrichmentProvider>();
            return services;
        }

    }
}
