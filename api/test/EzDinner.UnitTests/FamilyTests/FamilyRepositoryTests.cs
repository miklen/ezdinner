using System.Net;
using EzDinner.Core.Aggregates.FamilyAggregate;
using EzDinner.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace EzDinner.UnitTests.FamilyTests;

public class FamilyRepositoryTests
{
    private readonly Mock<Container> _container = new();
    private readonly Family _family = Family.CreateNew(Guid.NewGuid(), "Family");
    private readonly FamilyRepository _repository;

    public FamilyRepositoryTests()
    {
        var client = new Mock<CosmosClient>();
        client.Setup(client => client.GetContainer("test", FamilyRepository.CONTAINER)).Returns(_container.Object);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { ["CosmosDb:Database"] = "test" }).Build();
        _repository = new FamilyRepository(client.Object, configuration);
    }

    [Fact]
    public async Task Membership_save_patches_only_owned_fields_and_never_upserts()
    {
        await _repository.SaveAsync(_family);
        _container.Verify(container => container.PatchItemAsync<Family>(_family.Id.ToString(), new PartitionKey(_family.Id.ToString()),
            It.Is<IReadOnlyList<PatchOperation>>(operations => operations.Select(operation => operation.Path).SequenceEqual(new[] { "/name", "/familyMembers", "/ownerId", "/updatedDate" })),
            It.IsAny<PatchItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _container.Verify(container => container.UpsertItemAsync(It.IsAny<Family>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Store_save_patches_only_selection_and_timestamp()
    {
        _family.ReplaceGroceryStores(new[] { "netto" }, new[] { "netto" });
        await _repository.SaveGroceryStoresAsync(_family);
        _container.Verify(container => container.PatchItemAsync<Family>(_family.Id.ToString(), new PartitionKey(_family.Id.ToString()),
            It.Is<IReadOnlyList<PatchOperation>>(operations => operations.Select(operation => operation.Path).SequenceEqual(new[] { "/groceryStores", "/updatedDate" })),
            It.IsAny<PatchItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Missing_family_is_created_with_initial_selection()
    {
        _family.ReplaceGroceryStores(new[] { "netto" }, new[] { "netto" });
        Patch().ThrowsAsync(CosmosFailure(HttpStatusCode.NotFound));
        await _repository.SaveAsync(_family);
        _container.Verify(container => container.CreateItemAsync(_family, new PartitionKey(_family.Id.ToString()), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Concurrent_creation_conflict_retries_patch_without_replacing_stores()
    {
        _container.SetupSequence(container => container.PatchItemAsync<Family>(It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<IReadOnlyList<PatchOperation>>(), It.IsAny<PatchItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(CosmosFailure(HttpStatusCode.NotFound)).ReturnsAsync(Mock.Of<ItemResponse<Family>>());
        _container.Setup(container => container.CreateItemAsync(_family, It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(CosmosFailure(HttpStatusCode.Conflict));
        await _repository.SaveAsync(_family);
        _container.Verify(container => container.PatchItemAsync<Family>(It.IsAny<string>(), It.IsAny<PartitionKey>(),
            It.Is<IReadOnlyList<PatchOperation>>(operations => operations.All(operation => operation.Path != "/groceryStores")), It.IsAny<PatchItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Store_save_never_recreates_a_deleted_family()
    {
        Patch().ThrowsAsync(CosmosFailure(HttpStatusCode.NotFound));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _repository.SaveGroceryStoresAsync(_family));
        _container.Verify(container => container.CreateItemAsync(It.IsAny<Family>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private Moq.Language.Flow.ISetup<Container, Task<ItemResponse<Family>>> Patch() => _container.Setup(container => container.PatchItemAsync<Family>(
        It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<IReadOnlyList<PatchOperation>>(), It.IsAny<PatchItemRequestOptions>(), It.IsAny<CancellationToken>()));
    private static CosmosException CosmosFailure(HttpStatusCode status) => new("test", status, 0, "test", 0);
}
