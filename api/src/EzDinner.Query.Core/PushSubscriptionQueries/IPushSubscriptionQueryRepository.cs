using System;
using System.Threading.Tasks;

namespace EzDinner.Query.Core.PushSubscriptionQueries
{
    public interface IPushSubscriptionQueryRepository
    {
        Task<bool> HasActiveSubscriptionAsync(Guid userId);
    }
}
