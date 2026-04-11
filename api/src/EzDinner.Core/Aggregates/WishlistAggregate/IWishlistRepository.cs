using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EzDinner.Core.Aggregates.WishlistAggregate
{
    public interface IWishlistRepository
    {
        Task<IReadOnlyList<WishlistItem>> GetActiveAsync(Guid familyId);
        Task<WishlistItem?> GetByDishAsync(Guid familyId, Guid dishId);
        Task AddAsync(WishlistItem item);
        Task UpdateAsync(WishlistItem item);
        Task DeleteAsync(WishlistItem item);
    }
}
