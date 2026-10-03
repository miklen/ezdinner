using EzDinner.Application.Commands.Dishes;
using EzDinner.Core.Aggregates.DishAggregate;
using Microsoft.Extensions.Logging;

namespace EzDinner.Application.Commands.RecipeSnapshots;

public sealed class ConfirmRecipeSnapshotCommand(IDishRepository repository, EnrichDishCommandHandler enrichment,
    ILogger<ConfirmRecipeSnapshotCommand> logger)
{
    public async Task ExecuteAsync(Guid familyId, Guid dishId, RecipeCandidate candidate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var snapshot = new RecipeSnapshotValueObject(candidate.Content, candidate.SourceUrl, candidate.CapturedAt, candidate.SourceHash);
        if (candidate.CapturedAt > DateTimeOffset.UtcNow.AddMinutes(5))
            throw new RecipeImportException("RECIPE_INVALID_CAPTURE_TIME");
        var dish = await RecipeSnapshotDishLoader.LoadAsync(repository, familyId, dishId);
        dish.SetRecipeSnapshot(snapshot);
        await repository.SaveAsync(dish);
        try
        {
            await enrichment.Handle(new EnrichDishCommand(familyId, dishId), cancellationToken);
        }
        catch (Exception)
        {
            logger.LogWarning("Metadata enrichment failed after saving recipe snapshot for dish {DishId}", dishId);
        }
    }
}
