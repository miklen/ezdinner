using EzDinner.Core.Aggregates.WishlistAggregate;
using NodaTime;
using System;
using System.Threading.Tasks;

namespace EzDinner.Application.Commands.Wishlist
{
    public class AddWishResult
    {
        public Guid WishId { get; }
        public bool AlreadyExists { get; }

        private AddWishResult(Guid wishId, bool alreadyExists)
        {
            WishId = wishId;
            AlreadyExists = alreadyExists;
        }

        public static AddWishResult Created(Guid wishId) => new(wishId, alreadyExists: false);
        public static AddWishResult Duplicate(Guid existingWishId) => new(existingWishId, alreadyExists: true);
    }

    public class AddWishCommand
    {
        private readonly IWishlistRepository _wishlistRepo;
        private readonly IWishStatsRepository _wishStatsRepo;
        private readonly IClock _clock;

        public AddWishCommand(IWishlistRepository wishlistRepo, IWishStatsRepository wishStatsRepo, IClock clock)
        {
            _wishlistRepo = wishlistRepo;
            _wishStatsRepo = wishStatsRepo;
            _clock = clock;
        }

        public async Task<AddWishResult> HandleAsync(Guid familyId, Guid dishId, string dishName, Guid requestedByUserId)
        {
            var existing = await _wishlistRepo.GetByDishAsync(familyId, dishId);
            if (existing is not null && !existing.IsExpired(_clock.GetCurrentInstant()))
                return AddWishResult.Duplicate(existing.Id);

            var wish = WishlistItem.CreateNew(familyId, dishId, dishName, requestedByUserId, _clock);
            await _wishlistRepo.AddAsync(wish);
            await _wishStatsRepo.IncrementWishesAddedAsync(familyId, requestedByUserId);

            return AddWishResult.Created(wish.Id);
        }
    }
}
