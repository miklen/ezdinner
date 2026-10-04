using EzDinner.Core.Aggregates.RatingRemindersAggregate;
using EzDinner.Infrastructure.Models.Json;
using EzDinner.Infrastructure.RatingReminders;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using System.Net;
using Xunit;

namespace EzDinner.UnitTests.RatingReminderTests;

public class RatingRemindersRepositoryTests
{
    private readonly Mock<Container> _container = new();
    private readonly Guid _userId = Guid.NewGuid();

    private RatingRemindersRepository Repository()
    {
        var client = new Mock<CosmosClient>();
        client.Setup(value => value.GetContainer("test", RatingRemindersRepository.ContainerName)).Returns(_container.Object);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { ["CosmosDb:Database"] = "test" }).Build();
        return new(client.Object, configuration);
    }

    [Fact]
    public async Task Missing_state_returns_null_without_creating_a_document()
    {
        _container.Setup(value => value.ReadItemAsync<RatingRemindersDocument>(_userId.ToString(), new PartitionKey(_userId.ToString()), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Missing", HttpStatusCode.NotFound, 0, "", 0));
        Assert.Null(await Repository().GetWithRevisionAsync(_userId, default));
        _container.Verify(value => value.ReadItemAsync<RatingRemindersDocument>(_userId.ToString(), new PartitionKey(_userId.ToString()), null, It.IsAny<CancellationToken>()), Times.Once);
        _container.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Competing_create_returns_false()
    {
        _container.Setup(value => value.CreateItemAsync(It.IsAny<RatingRemindersDocument>(), new PartitionKey(_userId.ToString()), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Exists", HttpStatusCode.Conflict, 0, "", 0));
        Assert.False(await Repository().CreateIfAbsentAsync(RatingReminders.CreateNew(_userId), default));
    }

    [Theory]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task Conflicting_replace_returns_false_and_uses_the_loaded_revision(HttpStatusCode status)
    {
        _container.Setup(value => value.ReplaceItemAsync(It.IsAny<RatingRemindersDocument>(), _userId.ToString(), new PartitionKey(_userId.ToString()),
            It.Is<ItemRequestOptions>(options => options.IfMatchEtag == "revision"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Conflict", status, 0, "", 0));
        Assert.False(await Repository().SaveIfUnchangedAsync(RatingReminders.CreateNew(_userId), "revision", default));
        _container.Verify(value => value.ReplaceItemAsync(It.IsAny<RatingRemindersDocument>(), _userId.ToString(), new PartitionKey(_userId.ToString()),
            It.Is<ItemRequestOptions>(options => options.IfMatchEtag == "revision"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Empty_revision_is_rejected_before_storage_access()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Repository().SaveIfUnchangedAsync(RatingReminders.CreateNew(_userId), "", default));
        _container.VerifyNoOtherCalls();
    }

    [Fact]
    public void Cosmos_serialization_round_trips_identity_dates_and_suppression()
    {
        var today = new LocalDate(2026, 10, 12);
        var occurrence = new RatingReminderOccurrenceValueObject(Guid.NewGuid(), Guid.NewGuid(), today.PlusDays(-1));
        var state = RatingReminders.CreateNew(_userId);
        state.SetPushEnabled(true);
        state.TryRecordPushAttempt(occurrence, today);
        state.DismissThroughOccurrence(occurrence, today);
        var settings = new JsonSerializerSettings { ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() } }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        var serializer = new CosmosJsonNetSerializer(settings);
        using var stream = serializer.ToStream(RatingRemindersDocument.FromAggregate(state));
        var restored = serializer.FromStream<RatingRemindersDocument>(stream).ToAggregate();
        Assert.Equal(_userId, restored.Id);
        Assert.True(restored.PushEnabled);
        Assert.True(restored.IsDismissed(occurrence));
        Assert.True(restored.HasAttempted(occurrence));
        Assert.Equal(today, restored.LatestPushAttemptDay);
        Assert.Equal(today, Assert.Single(restored.PushAttempts).AttemptedOn);
    }
}
