using EzDinner.Core.Aggregates.PushSubscriptionAggregate;
using System;
using System.Threading.Tasks;

namespace EzDinner.Application.Commands.PushSubscriptions
{
    public class SavePushSubscriptionCommand
    {
        private readonly IPushSubscriptionRepository _repository;

        public SavePushSubscriptionCommand(IPushSubscriptionRepository repository)
        {
            _repository = repository;
        }

        public async Task Handle(Guid userId, Guid familyId, string endpoint, string p256dh, string auth, string language)
        {
            // Replace any existing subscription for this user before saving the new one
            await _repository.DeleteByUserIdAsync(userId);

            var subscription = PushSubscription.CreateNew(userId, familyId, endpoint, p256dh, auth, language);
            await _repository.SaveAsync(subscription);
        }
    }
}
