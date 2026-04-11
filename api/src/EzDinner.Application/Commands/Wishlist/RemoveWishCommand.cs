using EzDinner.Core.Aggregates.FamilyAggregate;
using EzDinner.Core.Aggregates.WishlistAggregate;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EzDinner.Application.Commands.Wishlist
{
    public enum RemoveWishResult
    {
        Removed,
        NotFound,
        Forbidden,
    }

    public class RemoveWishCommand
    {
        private readonly IWishlistRepository _wishlistRepo;
        private readonly IFamilyRepository _familyRepo;

        public RemoveWishCommand(IWishlistRepository wishlistRepo, IFamilyRepository familyRepo)
        {
            _wishlistRepo = wishlistRepo;
            _familyRepo = familyRepo;
        }

        public async Task<RemoveWishResult> HandleAsync(Guid familyId, Guid wishId, Guid requestingUserId)
        {
            var wishes = await _wishlistRepo.GetActiveAsync(familyId);
            var wish = wishes.FirstOrDefault(w => w.Id == wishId);
            if (wish is null) return RemoveWishResult.NotFound;

            // Requester may remove their own wish; family owners may remove any wish
            if (wish.AddedBy != requestingUserId)
            {
                var family = await _familyRepo.GetFamily(familyId);
                var isOwner = family?.FamilyMembers.Any(m => m.IsOwner && m.Id == requestingUserId) ?? false;
                if (!isOwner) return RemoveWishResult.Forbidden;
            }

            await _wishlistRepo.DeleteAsync(wish);
            return RemoveWishResult.Removed;
        }
    }
}
