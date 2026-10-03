using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Moq;
using NodaTime;
using System.Net;
using Xunit;

namespace EzDinner.UnitTests.DishRecommendationTests;

public class ConditionalDinnerStorageTests
{
    [Fact]
    public async Task ConditionalCreationReturnsConflictWhenAnotherWriterCreatedTheSameDay()
    {
        var dinner = Dinner.CreateNew(Guid.NewGuid(), new LocalDate(2026, 10, 5));
        var container = new Mock<Container>();
        container.Setup(store => store.CreateItemAsync(dinner, It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), default))
            .ThrowsAsync(new CosmosException("Conflict", HttpStatusCode.Conflict, 0, "test", 0));
        var client = new Mock<CosmosClient>();
        client.Setup(cosmos => cosmos.GetContainer("test", DinnerRepository.CONTAINER)).Returns(container.Object);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { ["CosmosDb:Database"] = "test" }).Build();
        Assert.False(await new DinnerRepository(client.Object, configuration).CreateIfAbsentAsync(dinner, default));
        container.VerifyAll();
    }

    [Theory]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task ConditionalSaveReturnsConflictForChangedOrDeletedDocument(HttpStatusCode status)
    {
        var dinner = Dinner.CreateNew(Guid.NewGuid(), new LocalDate(2026, 10, 5));
        var container = new Mock<Container>();
        container.Setup(store => store.ReplaceItemAsync(dinner, dinner.Id.ToString(), It.IsAny<PartitionKey?>(), It.Is<ItemRequestOptions>(options => options.IfMatchEtag == "etag"), default))
            .ThrowsAsync(new CosmosException("Conflict", status, 0, "test", 0));
        var client = new Mock<CosmosClient>();
        client.Setup(cosmos => cosmos.GetContainer("test", DinnerRepository.CONTAINER)).Returns(container.Object);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { ["CosmosDb:Database"] = "test" }).Build();
        Assert.False(await new DinnerRepository(client.Object, configuration).SaveIfUnchangedAsync(dinner, "etag", default));
        container.VerifyAll();
    }

    [Fact]
    public async Task UnauthorizedUndoNeverTouchesStorage()
    {
        var repository = new Mock<IConditionalDinnerRepository>(MockBehavior.Strict);
        var authorization = new Mock<EzDinner.Authorization.Core.IAuthzService>();
        var function = new EzDinner.Functions.DinnerUndoMenuChange(new(repository.Object), authorization.Object);
        var request = new Microsoft.AspNetCore.Http.DefaultHttpContext().Request;
        Assert.IsType<Microsoft.AspNetCore.Mvc.UnauthorizedResult>(await function.Run(request, Guid.NewGuid().ToString(), "2026-10-05"));
        repository.VerifyNoOtherCalls();
    }
}
