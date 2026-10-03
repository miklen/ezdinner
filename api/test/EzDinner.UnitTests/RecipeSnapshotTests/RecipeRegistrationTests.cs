using EzDinner.Application.Commands.Dishes;
using EzDinner.Application.Commands.RecipeSnapshots;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Infrastructure;
using EzDinner.Infrastructure.RecipeSnapshots;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace EzDinner.UnitTests.RecipeSnapshotTests;

public class RecipeRegistrationTests
{
    [Fact]
    public void Recipe_service_dependency_graph_resolves()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Anthropic:ApiKey"] = "test-only"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IDishRepository>(_ => Mock.Of<IDishRepository>());
        services.AddScoped<EnrichDishCommandHandler>();
        services.RegisterEnrichment(configuration).RegisterRecipeSnapshots(configuration);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        Assert.IsType<PreviewRecipeSnapshotCommand>(scope.ServiceProvider.GetRequiredService<PreviewRecipeSnapshotCommand>());
        Assert.IsType<ConfirmRecipeSnapshotCommand>(scope.ServiceProvider.GetRequiredService<ConfirmRecipeSnapshotCommand>());
        Assert.IsType<RemoveRecipeSnapshotCommand>(scope.ServiceProvider.GetRequiredService<RemoveRecipeSnapshotCommand>());
        Assert.IsType<RecipeSourceReader>(scope.ServiceProvider.GetRequiredService<IRecipeSourceReader>());
    }
}
