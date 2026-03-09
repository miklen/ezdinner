using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Infrastructure;
using EzDinner.Query.Core.DishQueries;
using EzDinner.Query.Core.FamilyQueries;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddAuthentication()
            .AddMicrosoftIdentityWebApi(context.Configuration.GetSection("AzureAdB2C"));
        services.AddAuthorization();

        services
            .AddAutoMapper(typeof(Program))
            .RegisterMsGraph(context.Configuration.GetSection("AzureAdB2C"))
            .RegisterCosmosDb(context.Configuration.GetSection("CosmosDb"))
            .RegisterCasbin(context.Configuration.GetSection("CosmosDb"))
            .RegisterRepositories()
            .AddScoped<IDinnerService, DinnerService>()
            .AddScoped<IDishQueryService, DishQueryService>()
            .AddScoped<IFamilyQueryService, FamilyQueryService>()
            .AddSingleton<IAuthzService, AuthzService>();
    })
    .Build();

host.Run();
