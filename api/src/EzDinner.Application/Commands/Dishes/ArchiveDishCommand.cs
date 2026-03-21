using EzDinner.Core.Aggregates.DishAggregate;
using System;
using System.Threading.Tasks;

namespace EzDinner.Application.Commands.Dishes
{
    public class ArchiveDishCommand
    {
        private readonly IDishRepository _dishRepository;

        public ArchiveDishCommand(IDishRepository dishRepository)
        {
            _dishRepository = dishRepository;
        }

        public async Task Handle(Guid familyId, Guid dishId)
        {
            if (familyId == Guid.Empty) throw new ArgumentException("MISSING_FAMILYID");
            if (dishId == Guid.Empty) throw new ArgumentException("MISSING_DISHID");

            var dish = await _dishRepository.GetDishAsync(dishId);
            if (dish is null) throw new KeyNotFoundException($"Dish {dishId} not found.");
            if (dish.FamilyId != familyId) throw new UnauthorizedAccessException("DISH_NOT_IN_FAMILY");
            dish.Archive();
            await _dishRepository.SaveAsync(dish);
        }
    }
}
