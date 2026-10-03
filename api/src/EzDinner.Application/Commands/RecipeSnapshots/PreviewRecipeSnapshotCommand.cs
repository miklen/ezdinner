using EzDinner.Core.Aggregates.DishAggregate;

namespace EzDinner.Application.Commands.RecipeSnapshots;

public sealed class PreviewRecipeSnapshotCommand(IDishRepository repository, IRecipeSourceReader reader,
    IStructuredRecipeExtractor structured, IRecipeExtractionProvider provider)
{
    public async Task<RecipePreview> ExecuteAsync(Guid familyId, Guid dishId, CancellationToken cancellationToken = default)
    {
        var dish = await RecipeSnapshotDishLoader.LoadAsync(repository, familyId, dishId);
        if (dish.Url is null) throw new RecipeImportException("RECIPE_NO_URL");
        var sourceUrl = dish.Url.OriginalString;
        var source = await reader.ReadAsync(sourceUrl, cancellationToken);
        var recipe = structured.Extract(source.Html);
        string hash;
        if (recipe is not null)
        {
            hash = structured.Hash(structured.FormatMarkdown(recipe));
        }
        else
        {
            var text = structured.CleanSourceText(source.Html);
            hash = structured.Hash(text);
            try
            {
                recipe = await provider.ExtractAsync(new RecipeExtractionRequest(text), cancellationToken);
            }
            catch (Exception exception) when (exception is not (RecipeImportException or OperationCanceledException))
            {
                throw new RecipeImportException("RECIPE_EXTRACTION_FAILED");
            }
        }
        var content = structured.FormatMarkdown(recipe);
        var capturedAt = DateTimeOffset.UtcNow;
        _ = new RecipeSnapshotValueObject(content, sourceUrl, capturedAt, hash);
        return new RecipePreview(new(content, sourceUrl, capturedAt, hash), recipe.Warnings,
            dish.RecipeSnapshot?.SourceHash != hash || dish.RecipeSnapshot.SourceUrl != sourceUrl);
    }
}
