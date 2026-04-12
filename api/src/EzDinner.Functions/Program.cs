using EzDinner.Application.Commands.Dishes;
using EzDinner.Application.Commands.FamilyMembers;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Infrastructure;
using EzDinner.Query.Core.DishQueries;
using EzDinner.Query.Core.FamilyQueries;
using EzDinner.Core.DomainServices.DinnerSuggestions;
using EzDinner.Query.Core.SuggestionQueries;
using EzDinner.Query.Core.WishlistQueries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using System.Text.Json.Serialization;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddMvcCore().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = Microsoft.Identity.Web.Constants.Bearer;
                options.DefaultChallengeScheme = Microsoft.Identity.Web.Constants.Bearer;
            })
            .AddMicrosoftIdentityWebApi(context.Configuration.GetSection("AzureAdB2C"));
        services.AddAuthorization();

        // Inject UseAuthentication + UseAuthorization into the ASP.NET Core pipeline
        services.AddSingleton<IStartupFilter, AuthMiddlewareStartupFilter>();

        var plannerKey = context.Configuration["Suggestions:Planner"];

        services
            .AddAutoMapper(typeof(Program))
            .RegisterMsGraph(context.Configuration.GetSection("AzureAdB2C"))
            .RegisterCosmosDb(context.Configuration.GetSection("CosmosDb"))
            .RegisterCasbin(context.Configuration.GetSection("CosmosDb"))
            .RegisterRepositories()
            .RegisterWebPush(context.Configuration)
            .RegisterEnrichment(context.Configuration)
            .AddScoped<UpdateDishMetadataCommandHandler>()
            .AddScoped<EnrichDishCommandHandler>()
            .AddScoped<MergeNonAutonomousMemberCommand>()
            .AddScoped<SetMemberRoleCommand>()
            .AddScoped<IDinnerService, DinnerService>()
            .AddScoped<IDishQueryService, DishQueryService>()
            .AddScoped<IFamilyQueryService, FamilyQueryService>()
            .AddSingleton<IAuthzService, AuthzService>()
            .AddScoped<DinnerSuggestionEngineService>()
            .AddScoped<IScoringRule, OverdueScoringRule>()
            .AddScoped<IScoringRule, RatingScoringRule>()
            .AddScoped<IScoringRule, RecencyPenaltyRule>()
            .AddScoped<IScoringRule, LeftoverPatternRule>()
            .AddScoped<IScoringRule, SeasonalAffinityRule>()
            .AddScoped<IScoringRule, EffortMatchRule>()
            .AddScoped<IScoringRule, WishlistBoostRule>()
            .AddScoped<SuggestionContextAssembler>()
            .AddScoped<IDinnerSuggestionService, DinnerSuggestionService>()
            .AddScoped<GetWishlistQuery>();

        if (string.Equals(plannerKey, "Ai", System.StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IDinnerWeekPlanner, AiDinnerWeekPlanner>();
        }
        else
        {
            if (!string.IsNullOrEmpty(plannerKey) && !string.Equals(plannerKey, "RuleBased", System.StringComparison.OrdinalIgnoreCase))
            {
                var sp = services.BuildServiceProvider();
                sp.GetRequiredService<ILogger<Program>>().LogWarning(
                    "Unknown Suggestions:Planner value '{Value}'. Defaulting to RuleBased.", plannerKey);
            }
            services.AddScoped<IDinnerWeekPlanner, RuleBasedDinnerWeekPlanner>();
        }
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
