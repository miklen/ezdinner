using EzDinner.Core.Aggregates.WishlistAggregate;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EzDinner.Query.Core.WishlistQueries
{
    public interface IWishlistQueryRepository
    {
        Task<IReadOnlyList<WishlistItem>> GetActiveAsync(Guid familyId, Instant now);
    }
}
