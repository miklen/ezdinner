using EzDinner.Core.Aggregates.WishlistAggregate;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EzDinner.Application.Commands.Wishlist
{
    public enum RemoveUpvoteResult
    {
        Removed,
        NotFound,
        NotVoted,
        Forbidden,
    }

    public class RemoveUpvoteCommand
    {
        private readonly IWishlistRepository _wishlistRepo;

        public RemoveUpvoteCommand(IWishlistRepository wishlistRepo)
        {
            _wishlistRepo = wishlistRepo;
        }

        public async Task<RemoveUpvoteResult> HandleAsync(Guid familyId, Guid wishId, Guid userId)
        {
            var wishes = await _wishlistRepo.GetActiveAsync(familyId);
            var wish = wishes.FirstOrDefault(w => w.Id == wishId);
            if (wish is null) return RemoveUpvoteResult.NotFound;

            try
            {
                wish.RemoveUpvote(userId);
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUESTER_CANNOT_REMOVE_VOTE")
            {
                return RemoveUpvoteResult.Forbidden;
            }
            catch (InvalidOperationException ex) when (ex.Message == "NOT_VOTED")
            {
                return RemoveUpvoteResult.NotVoted;
            }

            await _wishlistRepo.UpdateAsync(wish);
            return RemoveUpvoteResult.Removed;
        }
    }
}
