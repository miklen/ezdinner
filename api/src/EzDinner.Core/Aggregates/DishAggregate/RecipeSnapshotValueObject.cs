namespace EzDinner.Core.Aggregates.DishAggregate;

public sealed record RecipeSnapshotValueObject
{
    public const int MaximumContentLength = 32_000;
    public string Content { get; }
    public string SourceUrl { get; }
    public DateTimeOffset CapturedAt { get; }
    public string SourceHash { get; }

    public RecipeSnapshotValueObject(string content, string sourceUrl, DateTimeOffset capturedAt, string sourceHash)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > MaximumContentLength)
            throw new ArgumentException("RECIPE_INVALID_CONTENT", nameof(content));
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var source) ||
            (source.Scheme != Uri.UriSchemeHttp && source.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(source.UserInfo))
            throw new ArgumentException("RECIPE_INVALID_URL", nameof(sourceUrl));
        if (capturedAt == default)
            throw new ArgumentException("RECIPE_INVALID_CAPTURE_TIME", nameof(capturedAt));
        if (sourceHash is null || sourceHash.Length != 64 || !sourceHash.All(Uri.IsHexDigit))
            throw new ArgumentException("RECIPE_INVALID_HASH", nameof(sourceHash));
        Content = content;
        SourceUrl = sourceUrl;
        CapturedAt = capturedAt.ToUniversalTime();
        SourceHash = sourceHash.ToLowerInvariant();
    }
}
