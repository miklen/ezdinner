using EzDinner.Query.Core.FamilyQueries;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EzDinner.Query.Core.WishlistQueries
{
    public class GetWishlistQuery
    {
        private readonly IWishlistQueryRepository _wishlistRepo;
        private readonly IFamilyQueryService _familyQueryService;

        public GetWishlistQuery(IWishlistQueryRepository wishlistRepo, IFamilyQueryService familyQueryService)
        {
            _wishlistRepo = wishlistRepo;
            _familyQueryService = familyQueryService;
        }

        public async Task<IReadOnlyList<WishlistItemResult>> ExecuteAsync(Guid familyId, Guid currentUserId)
        {
            var now = SystemClock.Instance.GetCurrentInstant();
            var allItems = await _wishlistRepo.GetActiveAsync(familyId, now);

            // Filter out expired items (lazy evaluation on read)
            var activeItems = allItems.Where(w => !w.IsExpired(now)).ToList();

            var familyDetails = await _familyQueryService.GetFamilyDetailsAsync(familyId);
            var memberNameMap = familyDetails?.FamilyMembers?
                .Where(m => m.Name != null)
                .ToDictionary(m => m.Id, m => m.Name ?? string.Empty)
                ?? new Dictionary<Guid, string>();

            return activeItems
                .OrderByDescending(w => w.Votes.Count)
                .ThenBy(w => w.AddedAt)
                .Select(w => new WishlistItemResult
                {
                    WishId = w.Id,
                    DishId = w.DishId,
                    DishName = w.DishName,
                    AddedById = w.AddedBy,
                    AddedByName = memberNameMap.TryGetValue(w.AddedBy, out var name) ? name : string.Empty,
                    VoteCount = w.Votes.Count,
                    VoterIds = w.Votes.Select(v => v.UserId).ToList(),
                    ExpiresAt = w.ExpiresAt.ToDateTimeOffset(),
                    IsVotedByCurrentUser = w.Votes.Any(v => v.UserId == currentUserId),
                })
                .ToList();
        }
    }
}
