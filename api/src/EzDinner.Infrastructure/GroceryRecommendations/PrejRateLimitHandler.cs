using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace EzDinner.Infrastructure.GroceryRecommendations;

public sealed class PrejRateLimitHandler(IPrejRateLimitStore store, GroceryRecommendationOptions options, IClock clock,
    PrejRateLimitMemory memory, ILogger<PrejRateLimitHandler>? logger = null)
    : DelegatingHandler
{
    private readonly string scope = "prej:quota:" + Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(options.ApiKey ?? "")));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ThrowIfLocallyBlocked();
        var blockedUntil = await store.ReadRateLimitAsync(scope, cancellationToken);
        if (blockedUntil > clock.GetCurrentInstant().ToDateTimeUtc())
        {
            memory.Extend(scope, blockedUntil.Value);
            throw new HttpRequestException($"PREJ_RATE_LIMITED_UNTIL:{blockedUntil:O}", null, HttpStatusCode.TooManyRequests);
        }
        ThrowIfLocallyBlocked();

        var response = await base.SendAsync(request, cancellationToken);
        try
        {
            var exhausted = response.Headers.TryGetValues("X-RateLimit-Remaining-Day", out var values) &&
                long.TryParse(values.SingleOrDefault(), out var remaining) && remaining == 0;
            if (response.StatusCode == HttpStatusCode.TooManyRequests || exhausted)
            {
                var now = clock.GetCurrentInstant().ToDateTimeUtc();
                var midnight = now.Date.AddDays(1);
                var retryAfter = RetryAfter(response.Headers, now) ?? midnight;
                if (exhausted && retryAfter < midnight) retryAfter = midnight;
                memory.Extend(scope, retryAfter);
                logger?.LogWarning("Prej requests deferred until {RetryAfterUtc}; status {StatusCode}, daily quota exhausted: {DailyExhausted}",
                    retryAfter, (int)response.StatusCode, exhausted);
                using var persistenceBudget = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                try { await store.ExtendRateLimitAsync(scope, retryAfter, persistenceBudget.Token); }
                catch (Exception exception)
                {
                    logger?.LogWarning(exception, "Could not persist Prej cooldown until {RetryAfterUtc}; the local block remains active and provider status {StatusCode} is preserved",
                        retryAfter, (int)response.StatusCode);
                }
            }
            return response;
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private void ThrowIfLocallyBlocked()
    {
        if (memory.IsBlocked(scope, clock.GetCurrentInstant().ToDateTimeUtc()))
            throw new HttpRequestException("PREJ_RATE_LIMITED", null, HttpStatusCode.TooManyRequests);
    }

    private static DateTime? RetryAfter(HttpResponseHeaders headers, DateTime now)
    {
        if (!headers.TryGetValues("Retry-After", out var values) ||
            !RetryConditionHeaderValue.TryParse(values.SingleOrDefault(), out var retry)) return null;
        var deadline = retry.Delta.HasValue ? now.Add(retry.Delta.Value) : retry.Date?.UtcDateTime;
        return deadline > now ? deadline : null;
    }
}
