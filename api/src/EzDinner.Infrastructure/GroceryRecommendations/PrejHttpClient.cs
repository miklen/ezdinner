using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace EzDinner.Infrastructure.GroceryRecommendations;

public sealed class PrejHttpClient(HttpClient client, GroceryRecommendationOptions options,
    ILogger<PrejHttpClient>? logger = null)
{
    public async Task<JsonDocument> GetAsync(string relativePath, CancellationToken cancellationToken)
    {
        if (!relativePath.StartsWith("/v1/", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("PREJ_NOT_CONFIGURED");
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://api.prej.app" + relativePath));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            logger?.LogWarning("Prej HTTP failure at {Endpoint}: status {StatusCode}; retry-after: {RetryAfter}; daily limit: {DailyLimit}; daily remaining: {DailyRemaining}",
                request.RequestUri!.AbsolutePath, (int)response.StatusCode,
                Header(response.Headers, "Retry-After"), Header(response.Headers, "X-RateLimit-Limit-Day"),
                Header(response.Headers, "X-RateLimit-Remaining-Day"));
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > 2_000_000) throw new InvalidOperationException("PREJ_RESPONSE_TOO_LARGE");
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        while (true)
        {
            var count = await stream.ReadAsync(chunk, cancellationToken);
            if (count == 0) break;
            if (buffer.Length + count > 2_000_000) throw new InvalidOperationException("PREJ_RESPONSE_TOO_LARGE");
            buffer.Write(chunk, 0, count);
        }
        return JsonDocument.Parse(buffer.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
    }

    private static string? Header(HttpResponseHeaders headers, string name) =>
        headers.TryGetValues(name, out var values) ? string.Join(",", values) : null;
}
