using EzDinner.Core.Aggregates.DishAggregate;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EzDinner.Application.Commands.Dishes
{
    public class UpdateDishMetadataCommandHandler
    {
        private readonly IDishRepository _dishRepository;

        public UpdateDishMetadataCommandHandler(IDishRepository dishRepository)
        {
            _dishRepository = dishRepository;
        }

        public async Task Handle(UpdateDishMetadataCommand command, CancellationToken ct = default)
        {
            if (command.FamilyId == Guid.Empty) throw new ArgumentException("MISSING_FAMILYID");
            if (command.DishId == Guid.Empty) throw new ArgumentException("MISSING_DISHID");
            if (command.Roles is not null && command.Roles.Count == 0)
                throw new ArgumentException("ROLES_MUST_NOT_BE_EMPTY");

            var dish = await _dishRepository.GetDishAsync(command.DishId);
            if (dish is null) throw new KeyNotFoundException($"Dish {command.DishId} not found.");
            if (dish.FamilyId != command.FamilyId) throw new UnauthorizedAccessException("DISH_NOT_IN_FAMILY");

            var incoming = DishMetadataValueObject.FromUserEdit(
                dish.Metadata,
                command.Roles,
                command.EffortLevel,
                command.SeasonAffinity,
                command.Cuisine);

            dish.UpdateMetadata(incoming);
            await _dishRepository.SaveAsync(dish);
        }
    }
}
