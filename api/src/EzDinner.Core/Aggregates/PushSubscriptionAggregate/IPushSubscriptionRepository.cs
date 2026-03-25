using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EzDinner.Core.Aggregates.PushSubscriptionAggregate
{
    public interface IPushSubscriptionRepository
    {
        Task SaveAsync(PushSubscription subscription);
        Task DeleteByUserIdAsync(Guid userId);
        Task DeleteAsync(PushSubscription subscription);
        Task<IEnumerable<PushSubscription>> GetByFamilyIdAsync(Guid familyId);
        Task<PushSubscription?> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<Guid>> GetAllFamilyIdsAsync();
    }
}
