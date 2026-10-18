using EzDinner.Infrastructure.GroceryRecommendations;
using Microsoft.Azure.Cosmos;
using Moq;
using Xunit;

namespace EzDinner.UnitTests.DishRecommendationTests;

public class GroceryRecommendationCacheMigrationTests
{
    private readonly Mock<Database> _database = new();
    private readonly Mock<Container> _container = new();

    [Fact]
    public async Task Creates_missing_container_with_scope_partition_and_item_expiration_enabled()
    {
        ConfigureContainer(-1);

        await GroceryRecommendationCacheMigration.EnsureAsync(_database.Object);

        _database.Verify(database => database.CreateContainerIfNotExistsAsync(
            It.Is<ContainerProperties>(properties =>
                properties.Id == CosmosGroceryRecommendationCache.ContainerName &&
                properties.PartitionKeyPath == "/scopeKey" && properties.DefaultTimeToLive == -1),
            It.IsAny<int?>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _container.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(86400)]
    public async Task Updates_existing_container_ttl_and_leaves_other_properties_intact(int? ttl)
    {
        var properties = ConfigureContainer(ttl);
        properties.IndexingPolicy.Automatic = false;

        await GroceryRecommendationCacheMigration.EnsureAsync(_database.Object);
        await GroceryRecommendationCacheMigration.EnsureAsync(_database.Object);

        _container.Verify(container => container.ReplaceContainerAsync(
            It.Is<ContainerProperties>(updated => ReferenceEquals(updated, properties) &&
                updated.DefaultTimeToLive == -1 && !updated.IndexingPolicy.Automatic),
            It.Is<ContainerRequestOptions>(options => options.IfMatchEtag == "container-etag"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Rejects_incompatible_partition_without_replacing_or_deleting_container()
    {
        ConfigureContainer(null, "/familyId");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            GroceryRecommendationCacheMigration.EnsureAsync(_database.Object));

        Assert.Contains("partition key /scopeKey", exception.Message);
        _container.VerifyNoOtherCalls();
    }

    private ContainerProperties ConfigureContainer(int? ttl, string partitionKey = "/scopeKey")
    {
        var properties = new ContainerProperties(CosmosGroceryRecommendationCache.ContainerName, partitionKey)
        { DefaultTimeToLive = ttl };
        var response = new Mock<ContainerResponse>();
        response.SetupGet(result => result.Resource).Returns(properties);
        response.SetupGet(result => result.Container).Returns(_container.Object);
        response.SetupGet(result => result.ETag).Returns("container-etag");
        _database.Setup(database => database.CreateContainerIfNotExistsAsync(
                It.IsAny<ContainerProperties>(), It.IsAny<int?>(), It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
        return properties;
    }
}
