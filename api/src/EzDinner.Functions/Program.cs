using EzDinner.Application.Commands.Dishes;
using EzDinner.Application.Commands.FamilyMembers;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Infrastructure;
using EzDinner.Infrastructure.RecipeSnapshots;
using EzDinner.Infrastructure.DishRecommendations;
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
            .AddSingleton<NodaTime.IClock>(NodaTime.SystemClock.Instance)
            .AddScoped<EzDinner.Core.DomainServices.RatingReminders.RatingReminderSelectionService>()
            .AddScoped<EzDinner.Query.Core.RatingReminderQueries.GetRatingRemindersQuery>()
            .AddScoped<EzDinner.Query.Core.RatingReminderQueries.GetRatingReminderPreferenceQuery>()
            .AddScoped<EzDinner.Application.Commands.RatingReminders.RatingReminderWrites>()
            .AddScoped<EzDinner.Application.Commands.RatingReminders.DismissRatingReminderCommand>()
            .AddScoped<EzDinner.Application.Commands.RatingReminders.SetRatingReminderPreferenceCommand>()
            .AddScoped<EzDinner.Functions.RatingReminderAccess>()
            .AddScoped<EzDinner.Application.Commands.RatingReminders.RatingReminderRecipientAccess>()
            .AddScoped<EzDinner.Application.Commands.RatingReminders.SendRatingRemindersCommand>()
            .AddScoped<EzDinner.Application.Commands.RatingReminders.IRatingReminderTransport, EzDinner.Infrastructure.RatingReminders.WebPushRatingReminderTransport>()
            .AddAutoMapper(typeof(Program))
            .RegisterMsGraph(context.Configuration.GetSection("AzureAdB2C"))
            .RegisterCosmosDb(context.Configuration.GetSection("CosmosDb"))
            .RegisterCasbin(context.Configuration.GetSection("CosmosDb"))
            .RegisterRepositories()
            .RegisterWebPush(context.Configuration)
            .RegisterEnrichment(context.Configuration)
            .RegisterRecipeSnapshots(context.Configuration)
            .RegisterDishRecommendations(context.Configuration)
            .AddScoped<EzDinner.Application.Commands.Dinners.UndoDinnerMenuChangeCommand>()
            .AddScoped<EzDinner.Application.Commands.Dinners.ChangeDinnerMenuCommand>()
            .AddScoped<EzDinner.Application.Commands.Dinners.ChangeDinnerCommand>()
            .AddScoped<EzDinner.Functions.ConditionalDinnerMenuChangeHttp>()
            .AddScoped(provider => new EzDinner.Application.Commands.Dinners.AddDishToDinnerCommand(
                provider.GetRequiredService<EzDinner.Application.Commands.Dinners.ChangeDinnerCommand>(),
                provider.GetRequiredService<EzDinner.Core.Aggregates.WishlistAggregate.IWishlistRepository>(),
                provider.GetRequiredService<EzDinner.Core.Aggregates.WishlistAggregate.IWishStatsRepository>(),
                provider.GetRequiredService<EzDinner.Core.Aggregates.PushSubscriptionAggregate.IPushSubscriptionRepository>(),
                provider.GetRequiredService<WebPush.WebPushClient>(),
                provider.GetRequiredService<ILogger<EzDinner.Application.Commands.Dinners.AddDishToDinnerCommand>>(),
                provider.GetRequiredService<EzDinner.Application.Commands.Dinners.ChangeDinnerMenuCommand>()))
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
