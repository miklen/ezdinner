namespace EzDinner.Infrastructure.GroceryRecommendations;

public sealed class GroceryRecommendationOptions
{
    public bool Enabled { get; set; }
    public bool CatalogRefreshEnabled { get; set; }
    public string? ApiKey { get; set; }
    public int MaximumPages { get; set; } = 50;
    public int MaximumOffers { get; set; } = 5_000;

    public bool CanAcquire => Enabled && !string.IsNullOrWhiteSpace(ApiKey);
}
