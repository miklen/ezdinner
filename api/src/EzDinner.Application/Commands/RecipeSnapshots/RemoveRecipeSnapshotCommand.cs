using EzDinner.Core.Aggregates.DishAggregate;

namespace EzDinner.Application.Commands.RecipeSnapshots;

public sealed class RemoveRecipeSnapshotCommand(IDishRepository repository)
{
    public async Task ExecuteAsync(Guid familyId, Guid dishId)
    {
        var dish = await RecipeSnapshotDishLoader.LoadAsync(repository, familyId, dishId);
        dish.RemoveRecipeSnapshot();
        await repository.SaveAsync(dish);
    }
}
