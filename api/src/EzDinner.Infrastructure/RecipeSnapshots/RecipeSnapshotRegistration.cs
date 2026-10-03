using System.Net;
using EzDinner.Application.Commands.RecipeSnapshots;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EzDinner.Infrastructure.RecipeSnapshots;

public static class RecipeSnapshotRegistration
{
    public static IServiceCollection RegisterRecipeSnapshots(this IServiceCollection services, IConfiguration configuration)
    {
        var limits = new RecipeRetrievalOptions
        {
            MaximumBytes = configuration.GetValue("RecipeSnapshots:MaximumBytes", 1_000_000),
            MaximumRedirects = configuration.GetValue("RecipeSnapshots:MaximumRedirects", 3),
            TimeoutSeconds = configuration.GetValue("RecipeSnapshots:TimeoutSeconds", 15),
            MaximumSourceCharacters = configuration.GetValue("RecipeSnapshots:MaximumSourceCharacters", 24_000)
        };
        limits.Validate();
        services.AddSingleton(limits);
        services.AddSingleton<IRecipeHostResolver, PublicRecipeHostResolver>();
        services.AddSingleton<IStructuredRecipeExtractor, StructuredRecipeExtractor>();
        services.AddScoped<IRecipeCompletionClient, AnthropicRecipeCompletionClient>();
        services.AddScoped<IRecipeExtractionProvider, AnthropicRecipeExtractionProvider>();
        services.AddHttpClient<IRecipeSourceReader, RecipeSourceReader>(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseProxy = false,
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                ConnectCallback = PublicRecipeHostResolver.ConnectAsync,
                MaxResponseHeadersLength = 32,
                PooledConnectionLifetime = TimeSpan.Zero
            });
        services.AddScoped<PreviewRecipeSnapshotCommand>();
        services.AddScoped<ConfirmRecipeSnapshotCommand>();
        services.AddScoped<RemoveRecipeSnapshotCommand>();
        return services;
    }
}
