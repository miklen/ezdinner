using EzDinner.Core.DomainServices.DishRecommendations;
using EzDinner.Query.Core.DishRecommendationQueries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using EzDinner.Application.Commands.Families;
using EzDinner.Infrastructure.GroceryRecommendations;

namespace EzDinner.Infrastructure.DishRecommendations;

public static class DishRecommendationRegistration
{
    public static IServiceCollection RegisterDishRecommendations(this IServiceCollection services, IConfiguration configuration)
    {
        var limits = new DishRecommendationLimits();
        configuration.GetSection("DishRecommendations").Bind(limits);
        if (limits.MaximumCandidates < 1 || limits.MaximumEvidenceCharacters < 1 || limits.MaximumResults is < 1 or > 20 ||
            limits.ProviderTimeoutSeconds is < 1 or > 120 || limits.MaximumTurns < 1 || limits.MaximumConstraints < 1 ||
            limits.MaximumRequestCharacters < 1 || limits.MaximumExclusions < 1)
            throw new InvalidOperationException("INVALID_RECOMMENDATION_LIMITS");
        services.AddSingleton(limits);
        services.AddSingleton<IClock>(SystemClock.Instance);
        services.AddScoped<ResurfacingRule>();
        services.AddScoped<DishRecommendationService>();
        services.AddScoped<DishRecommendationContextAssembler>();
        services.AddScoped<DishRecommendationQuery>();
        services.AddSingleton<IDishRecommendationCompletionClient, AnthropicDishRecommendationCompletionClient>();
        services.AddScoped<IDishRecommendationLlmClient, AnthropicDishRecommendationClient>();
        var groceries = new GroceryRecommendationOptions();
        configuration.GetSection("GroceryRecommendations").Bind(groceries);
        if (groceries.MaximumPages is < 1 or > 50 || groceries.MaximumOffers is < 1 or > 5_000)
            throw new InvalidOperationException("INVALID_GROCERY_LIMITS");
        services.AddSingleton(groceries);
        services.AddHttpClient<PrejHttpClient>().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddSingleton<CosmosGroceryRecommendationCache>();
        services.AddSingleton<IGroceryRecommendationCache>(provider => provider.GetRequiredService<CosmosGroceryRecommendationCache>());
        services.AddSingleton<IGrocerySnapshotStore>(provider => provider.GetRequiredService<CosmosGroceryRecommendationCache>());
        services.AddSingleton<GrocerySnapshotRefresh>();
        services.AddSingleton<IGroceryStoreCatalog, PrejGroceryStoreCatalog>();
        services.AddSingleton<PrejGroceryOfferSource>();
        services.AddSingleton<IGroceryOfferReader, CachedGroceryOfferReader>();
        services.AddSingleton<DishIngredientProfileInference>();
        services.AddSingleton<IDishIngredientProfileReader, CachedDishIngredientProfileReader>();
        services.AddScoped<GroceryRecommendationEnrichment>();
        services.AddScoped<SetGroceryStoresCommand>();
        return services;
    }
}
