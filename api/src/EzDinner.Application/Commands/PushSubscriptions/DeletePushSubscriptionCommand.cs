using EzDinner.Core.Aggregates.PushSubscriptionAggregate;
using System;
using System.Threading.Tasks;

namespace EzDinner.Application.Commands.PushSubscriptions
{
    public class DeletePushSubscriptionCommand
    {
        private readonly IPushSubscriptionRepository _repository;

        public DeletePushSubscriptionCommand(IPushSubscriptionRepository repository)
        {
            _repository = repository;
        }

        public Task Handle(Guid userId)
        {
            if (userId == Guid.Empty) throw new ArgumentException("MISSING_USERID");
            return _repository.DeleteByUserIdAsync(userId);
        }
    }
}
