using System;
using System.Threading.Tasks;

namespace EzDinner.Query.Core.PushSubscriptionQueries
{
    public class GetPushSubscriptionQuery
    {
        private readonly IPushSubscriptionQueryRepository _repository;

        public GetPushSubscriptionQuery(IPushSubscriptionQueryRepository repository)
        {
            _repository = repository;
        }

        public Task<bool> HasActiveSubscriptionAsync(Guid userId)
        {
            return _repository.HasActiveSubscriptionAsync(userId);
        }
    }
}
