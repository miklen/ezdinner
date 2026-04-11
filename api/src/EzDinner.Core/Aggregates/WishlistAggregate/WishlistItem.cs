using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EzDinner.Core.Aggregates.WishlistAggregate
{
    /// <summary>
    /// A family wish for a specific dish. One entry per dish per family — deduplication enforced.
    /// Votes extend the expiry window; forgotten wishes auto-expire after 14 days of inactivity.
    /// </summary>
    public class WishlistItem : AggregateRoot<Guid>
    {
        private readonly List<Vote> _votes;

        public Guid FamilyId { get; }
        public Guid DishId { get; }
        public string DishName { get; }
        public Guid AddedBy { get; }
        public Instant AddedAt { get; }
        public Instant ExpiresAt { get; private set; }
        public IReadOnlyList<Vote> Votes => _votes;

        /// <summary>
        /// Partition key is familyId — all family wishes co-located in one partition.
        /// </summary>
        public override Guid PartitionKey => FamilyId;

        public bool IsExpired(Instant now) => ExpiresAt < now;

        public bool HasUserVoted(Guid userId) => _votes.Any(v => v.UserId == userId);

        /// <summary>
        /// For serialization purpose only.
        /// </summary>
        public WishlistItem(
            Guid id,
            Guid familyId,
            Guid dishId,
            string dishName,
            Guid addedBy,
            Instant addedAt,
            Instant expiresAt,
            IEnumerable<Vote>? votes = null)
            : base(id)
        {
            FamilyId = familyId;
            DishId = dishId;
            DishName = dishName;
            AddedBy = addedBy;
            AddedAt = addedAt;
            ExpiresAt = expiresAt;
            _votes = votes?.ToList() ?? new List<Vote>();
        }

        public static WishlistItem CreateNew(Guid familyId, Guid dishId, string dishName, Guid requestedByUserId, IClock clock)
        {
            if (familyId == Guid.Empty) throw new ArgumentException("FamilyId cannot be empty.", nameof(familyId));
            if (dishId == Guid.Empty) throw new ArgumentException("DishId cannot be empty.", nameof(dishId));
            if (string.IsNullOrWhiteSpace(dishName)) throw new ArgumentException("DishName cannot be empty.", nameof(dishName));
            if (requestedByUserId == Guid.Empty) throw new ArgumentException("RequestedByUserId cannot be empty.", nameof(requestedByUserId));

            var now = clock.GetCurrentInstant();
            var expiresAt = now + Duration.FromDays(14);
            var item = new WishlistItem(
                id: Guid.NewGuid(),
                familyId,
                dishId,
                dishName,
                addedBy: requestedByUserId,
                addedAt: now,
                expiresAt);

            // The creator's interest is the first vote
            item._votes.Add(new Vote(requestedByUserId, now));
            return item;
        }

        /// <summary>
        /// Records an upvote. Extends expiry to at least 14 days from now.
        /// Throws if the user has already voted.
        /// </summary>
        public void Upvote(Guid userId, IClock clock)
        {
            if (HasUserVoted(userId))
                throw new InvalidOperationException("ALREADY_VOTED");

            var voteTime = clock.GetCurrentInstant();
            _votes.Add(new Vote(userId, voteTime));

            var extendedExpiry = voteTime + Duration.FromDays(14);
            if (extendedExpiry > ExpiresAt)
                ExpiresAt = extendedExpiry;
        }
    }
}
