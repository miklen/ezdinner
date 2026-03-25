using NodaTime;
using System;

namespace EzDinner.Core.Aggregates.PushSubscriptionAggregate
{
    public class PushSubscription : AggregateRoot<Guid>
    {
        public Guid UserId { get; private set; }
        public Guid FamilyId { get; private set; }
        public string Endpoint { get; private set; }
        public string P256dh { get; private set; }
        public string Auth { get; private set; }
        public string Language { get; private set; }
        public Instant CreatedAt { get; private set; }

        /// <summary>
        /// Partition key is familyId to allow efficient batch reads during delivery.
        /// </summary>
        public override Guid PartitionKey => FamilyId;

        /// <summary>
        /// For serialization purpose only.
        /// </summary>
        public PushSubscription(Guid id, Guid userId, Guid familyId, string endpoint, string p256dh, string auth, string language, Instant createdAt)
            : base(id)
        {
            UserId = userId;
            FamilyId = familyId;
            Endpoint = endpoint;
            P256dh = p256dh;
            Auth = auth;
            Language = language;
            CreatedAt = createdAt;
        }

        public static PushSubscription CreateNew(Guid userId, Guid familyId, string endpoint, string p256dh, string auth, string language)
        {
            if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));
            if (familyId == Guid.Empty) throw new ArgumentException("FamilyId cannot be empty.", nameof(familyId));
            if (string.IsNullOrWhiteSpace(endpoint)) throw new ArgumentException("Endpoint cannot be empty.", nameof(endpoint));
            if (string.IsNullOrWhiteSpace(p256dh)) throw new ArgumentException("P256dh cannot be empty.", nameof(p256dh));
            if (string.IsNullOrWhiteSpace(auth)) throw new ArgumentException("Auth cannot be empty.", nameof(auth));

            return new PushSubscription(
                id: Guid.NewGuid(),
                userId,
                familyId,
                endpoint,
                p256dh,
                auth,
                language,
                createdAt: SystemClock.Instance.GetCurrentInstant());
        }
    }
}
