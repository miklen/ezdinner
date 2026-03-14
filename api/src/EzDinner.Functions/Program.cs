using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Infrastructure;
using EzDinner.Query.Core.DishQueries;
using EzDinner.Query.Core.FamilyQueries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

        // Inject UseAuthentication + UseAuthorization into the ASP.NET Core pipeline
        services.AddSingleton<IStartupFilter, AuthMiddlewareStartupFilter>();

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

file sealed class AuthMiddlewareStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.UseAuthentication();
            app.UseAuthorization();
            next(app);
        };
}
