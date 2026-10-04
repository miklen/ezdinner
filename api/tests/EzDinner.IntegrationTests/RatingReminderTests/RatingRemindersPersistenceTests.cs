using EzDinner.Core.Aggregates.RatingRemindersAggregate;
using EzDinner.Infrastructure.RatingReminders;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;

namespace EzDinner.IntegrationTests.RatingReminderTests;

public class RatingRemindersPersistenceTests : IClassFixture<StartupFixture>
{
    private readonly IServiceProvider _provider;
    public RatingRemindersPersistenceTests(StartupFixture fixture) => _provider = fixture.Provider;

    [Fact]
    public async Task Persisted_reload_preserves_dismissal_and_rejects_competing_writes()
    {
        var client = _provider.GetRequiredService<CosmosClient>();
        var configuration = _provider.GetRequiredService<IConfiguration>();
        var database = client.GetDatabase(configuration["CosmosDb:Database"]);
        await database.CreateContainerIfNotExistsAsync(new ContainerProperties(RatingRemindersRepository.ContainerName, "/id"));
        var repository = _provider.GetRequiredService<IRatingRemindersRepository>();
        var userId = Guid.NewGuid();
        var state = RatingReminders.CreateNew(userId);
        var today = new LocalDate(2026, 10, 12);
        var occurrence = new RatingReminderOccurrenceValueObject(Guid.NewGuid(), Guid.NewGuid(), today.PlusDays(-1));
        Assert.Null(await repository.GetWithRevisionAsync(userId, default));
        Assert.True(await repository.CreateIfAbsentAsync(state, default));
        try
        {
            Assert.False(await repository.CreateIfAbsentAsync(state, default));
            var first = (await repository.GetWithRevisionAsync(userId, default)).Value;
            var competing = (await repository.GetWithRevisionAsync(userId, default)).Value;
            first.Reminders.DismissThroughOccurrence(occurrence, today);
            competing.Reminders.SetPushEnabled(true);
            var results = await Task.WhenAll(repository.SaveIfUnchangedAsync(first.Reminders, first.Revision, default),
                repository.SaveIfUnchangedAsync(competing.Reminders, competing.Revision, default));
            Assert.Single(results.Where(success => success));
            var retry = (await repository.GetWithRevisionAsync(userId, default)).Value;
            retry.Reminders.SetPushEnabled(true);
            retry.Reminders.DismissThroughOccurrence(occurrence, today);
            Assert.True(await repository.SaveIfUnchangedAsync(retry.Reminders, retry.Revision, default));
            var persisted = (await repository.GetWithRevisionAsync(userId, default)).Value.Reminders;
            Assert.True(persisted.PushEnabled);
            Assert.True(persisted.IsDismissed(occurrence));
        }
        finally
        {
            await database.GetContainer(RatingRemindersRepository.ContainerName).DeleteItemAsync<RatingRemindersDocument>(userId.ToString(), new PartitionKey(userId.ToString()));
        }
    }
}
