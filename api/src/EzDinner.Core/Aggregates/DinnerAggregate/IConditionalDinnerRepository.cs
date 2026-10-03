using NodaTime;

namespace EzDinner.Core.Aggregates.DinnerAggregate;

public interface IConditionalDinnerRepository
{
    Task<(Dinner Dinner, string Revision)?> GetWithRevisionAsync(Guid familyId, LocalDate date, CancellationToken cancellationToken);
    Task<bool> SaveIfUnchangedAsync(Dinner dinner, string revision, CancellationToken cancellationToken);
    Task<bool> CreateIfAbsentAsync(Dinner dinner, CancellationToken cancellationToken);
}
