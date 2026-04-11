using System;
using System.Threading.Tasks;

namespace EzDinner.Core.Aggregates.WishlistAggregate
{
    public interface IWishStatsRepository
    {
        Task IncrementWishesAddedAsync(Guid familyId, Guid userId);
        Task IncrementWishesGrantedAsync(Guid familyId, Guid userId);
    }
}
