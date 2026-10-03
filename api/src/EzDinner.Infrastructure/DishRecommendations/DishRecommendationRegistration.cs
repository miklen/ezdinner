using EzDinner.Core.DomainServices.DishRecommendations;
using EzDinner.Query.Core.DishRecommendationQueries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

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
        services.AddScoped<IDishRecommendationCompletionClient, AnthropicDishRecommendationCompletionClient>();
        services.AddScoped<IDishRecommendationLlmClient, AnthropicDishRecommendationClient>();
        return services;
    }
}
