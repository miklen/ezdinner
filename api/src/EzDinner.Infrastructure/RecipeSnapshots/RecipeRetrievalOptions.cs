namespace EzDinner.Infrastructure.RecipeSnapshots;

public sealed class RecipeRetrievalOptions
{
    public int MaximumBytes { get; init; } = 1_000_000;
    public int MaximumRedirects { get; init; } = 3;
    public int TimeoutSeconds { get; init; } = 15;
    public int MaximumSourceCharacters { get; init; } = 24_000;

    public void Validate()
    {
        if (MaximumBytes is < 1 or > 5_000_000 || MaximumRedirects is < 0 or > 10 ||
            TimeoutSeconds is < 1 or > 60 || MaximumSourceCharacters is < 1 or > 100_000)
            throw new InvalidOperationException("RECIPE_INVALID_CONFIGURATION");
    }
}
