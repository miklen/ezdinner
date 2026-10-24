namespace EzDinner.Infrastructure.GroceryRecommendations;

public interface IPrejRateLimitStore
{
    Task<DateTime?> ReadRateLimitAsync(string scope, CancellationToken cancellationToken);
    Task ExtendRateLimitAsync(string scope, DateTime retryAfter, CancellationToken cancellationToken);
}
