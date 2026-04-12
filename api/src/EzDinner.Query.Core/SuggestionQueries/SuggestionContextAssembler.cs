using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.WishlistAggregate;
using EzDinner.Core.DomainServices.DinnerSuggestions;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EzDinner.Query.Core.SuggestionQueries
{
    public class SuggestionContextAssembler
    {
        private readonly IDishRepository _dishRepository;
        private readonly IDinnerRepository _dinnerRepository;
        private readonly IWishlistRepository _wishlistRepository;

        public SuggestionContextAssembler(
            IDishRepository dishRepository,
            IDinnerRepository dinnerRepository,
            IWishlistRepository wishlistRepository)
        {
            _dishRepository = dishRepository;
            _dinnerRepository = dinnerRepository;
            _wishlistRepository = wishlistRepository;
        }

        public async Task<FamilySuggestionContext> AssembleAsync(
            Guid familyId,
            IReadOnlyList<Guid> excludedDishIds,
            IReadOnlyDictionary<LocalDate, EffortLevel>? effortPreferences = null)
        {
            var dishesTask = _dishRepository.GetDishesAsync(familyId, includeArchived: false);
            var wishlistTask = _wishlistRepository.GetActiveAsync(familyId);

            await Task.WhenAll(dishesTask, wishlistTask);

            var dishes = dishesTask.Result.ToList();

            var allDinners = new List<Dinner>();
            await foreach (var dinner in _dinnerRepository.GetAsync(familyId, LocalDate.MinIsoValue, LocalDate.MaxIsoValue))
                allDinners.Add(dinner);

            var now = SystemClock.Instance.GetCurrentInstant();
            var wishVotes = wishlistTask.Result
                .Where(w => !w.IsExpired(now))
                .ToDictionary(w => w.DishId, w => w.Votes.Count);

            return new FamilySuggestionContext(dishes, allDinners, wishVotes, excludedDishIds, effortPreferences);
        }
    }
}
