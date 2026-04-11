using System;
using System.Collections.Generic;

namespace EzDinner.Query.Core.WishlistQueries
{
    public class WishlistItemResult
    {
        public Guid WishId { get; set; }
        public Guid DishId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public string AddedByName { get; set; } = string.Empty;
        public Guid AddedById { get; set; }
        public int VoteCount { get; set; }
        public IReadOnlyList<Guid> VoterIds { get; set; } = Array.Empty<Guid>();
        public DateTimeOffset ExpiresAt { get; set; }
        public bool IsVotedByCurrentUser { get; set; }
    }
}
