namespace EzDinner.Infrastructure.DishRecommendations;

internal static class DishRecommendationJsonResponse
{
    public static string UnwrapFence(string response)
    {
        var trimmed = response.Trim();
        var firstLineEnd = trimmed.IndexOf('\n');
        if (firstLineEnd < 0 || !trimmed.EndsWith("\n```", StringComparison.Ordinal)) return response;
        var opening = trimmed[..firstLineEnd].TrimEnd('\r');
        if (opening is not ("```json" or "```")) return response;
        return trimmed[(firstLineEnd + 1)..^3].Trim();
    }
}
