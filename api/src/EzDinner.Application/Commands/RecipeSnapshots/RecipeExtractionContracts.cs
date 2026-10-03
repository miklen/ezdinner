namespace EzDinner.Application.Commands.RecipeSnapshots;

public sealed record RecipeExtractionRequest(string SourceText);
public sealed record RecipeExtractionResult(string? Title, IReadOnlyList<string> Ingredients, IReadOnlyList<string> Instructions, IReadOnlyList<string> Warnings);
public sealed record RecipeSource(string Html);
public sealed record RecipeCandidate(string Content, string SourceUrl, DateTimeOffset CapturedAt, string SourceHash);
public sealed record RecipePreview(RecipeCandidate Candidate, IReadOnlyList<string> Warnings, bool SourceChanged);

public interface IRecipeSourceReader
{
    Task<RecipeSource> ReadAsync(string sourceUrl, CancellationToken cancellationToken);
}

public interface IRecipeExtractionProvider
{
    Task<RecipeExtractionResult> ExtractAsync(RecipeExtractionRequest request, CancellationToken cancellationToken);
}

public interface IStructuredRecipeExtractor
{
    RecipeExtractionResult? Extract(string html);
    string CleanSourceText(string html);
    string FormatMarkdown(RecipeExtractionResult recipe);
    string Hash(string source);
}

public sealed class RecipeImportException : Exception
{
    public string Code { get; }
    public RecipeImportException(string code) : base(code) => Code = code;
}
