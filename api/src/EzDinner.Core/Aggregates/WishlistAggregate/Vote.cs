using NodaTime;
using System;

namespace EzDinner.Core.Aggregates.WishlistAggregate
{
    /// <summary>
    /// Records a single upvote on a wish item. Immutable — defined by its values, not its identity.
    /// </summary>
    public class Vote : IEquatable<Vote>
    {
        public Guid UserId { get; }
        public Instant VotedAt { get; }

        public Vote(Guid userId, Instant votedAt)
        {
            UserId = userId;
            VotedAt = votedAt;
        }

        public bool Equals(Vote? other) =>
            other is not null && UserId == other.UserId && VotedAt == other.VotedAt;

        public override bool Equals(object? obj) => Equals(obj as Vote);

        public override int GetHashCode() => HashCode.Combine(UserId, VotedAt);
    }
}
