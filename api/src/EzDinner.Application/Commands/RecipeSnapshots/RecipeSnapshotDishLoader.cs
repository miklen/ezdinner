using EzDinner.Core.Aggregates.DishAggregate;

namespace EzDinner.Application.Commands.RecipeSnapshots;

internal static class RecipeSnapshotDishLoader
{
    public static async Task<Dish> LoadAsync(IDishRepository repository, Guid familyId, Guid dishId)
    {
        if (familyId == Guid.Empty || dishId == Guid.Empty) throw new RecipeImportException("RECIPE_INVALID_REQUEST");
        var dish = await repository.GetDishAsync(dishId);
        if (dish is null || dish.Deleted) throw new KeyNotFoundException("DISH_NOT_FOUND");
        if (dish.FamilyId != familyId) throw new UnauthorizedAccessException("DISH_NOT_IN_FAMILY");
        return dish;
    }
}
