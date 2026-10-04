using EzDinner.Core.Aggregates.RatingRemindersAggregate;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Net;
using ReminderState = EzDinner.Core.Aggregates.RatingRemindersAggregate.RatingReminders;

namespace EzDinner.Infrastructure.RatingReminders;

public sealed class RatingRemindersRepository : IRatingRemindersRepository
{
    public const string ContainerName = "RatingReminders";
    private readonly Container _container;

    public RatingRemindersRepository(CosmosClient client, IConfiguration configuration)
    {
        _container = client.GetContainer(configuration.GetValue<string>("CosmosDb:Database"), ContainerName);
    }

    public async Task<(ReminderState Reminders, string Revision)?> GetWithRevisionAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _container.ReadItemAsync<RatingRemindersDocument>(userId.ToString(), new PartitionKey(userId.ToString()), cancellationToken: cancellationToken);
            if (response.Resource.Id != userId) throw new InvalidOperationException("RATING_REMINDER_OWNER_MISMATCH");
            return (response.Resource.ToAggregate(), response.ETag);
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound) { return null; }
    }

    public async Task<bool> CreateIfAbsentAsync(ReminderState reminders, CancellationToken cancellationToken)
    {
        try
        {
            await _container.CreateItemAsync(RatingRemindersDocument.FromAggregate(reminders), new PartitionKey(reminders.Id.ToString()), cancellationToken: cancellationToken);
            return true;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.Conflict) { return false; }
    }

    public async Task<bool> SaveIfUnchangedAsync(ReminderState reminders, string revision, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(revision)) throw new ArgumentException("RATING_REMINDER_REVISION_REQUIRED", nameof(revision));
        try
        {
            await _container.ReplaceItemAsync(RatingRemindersDocument.FromAggregate(reminders), reminders.Id.ToString(),
                new PartitionKey(reminders.Id.ToString()), new ItemRequestOptions { IfMatchEtag = revision }, cancellationToken);
            return true;
        }
        catch (CosmosException exception) when (exception.StatusCode is HttpStatusCode.PreconditionFailed or HttpStatusCode.NotFound) { return false; }
    }
}
