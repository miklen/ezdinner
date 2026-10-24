using System.Net;
using EzDinner.Infrastructure.GroceryRecommendations;
using NodaTime;
using NodaTime.Testing;
using Xunit;

namespace EzDinner.UnitTests.DishRecommendationTests;

public class PrejRateLimitTests
{
    private readonly FakeClock clock = new(Instant.FromUtc(2026, 10, 24, 18, 0));
    private readonly MemoryRateLimits store = new();
    private readonly PrejRateLimitMemory memory = new();

    [Fact]
    public async Task Observed_429_blocks_outbound_work_while_persistence_is_still_pending()
    {
        store.WriteStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        store.AllowWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        using var client = Client(() => ++calls == 1 ? Limited("120", "10") : new(HttpStatusCode.OK));
        var first = client.GetAsync("https://api.prej.app/v1/offers?page=2");
        await store.WriteStarted.Task;
        try
        {
            await Assert.ThrowsAnyAsync<HttpRequestException>(() => client.GetAsync("https://api.prej.app/v1/offers?page=6"));
            Assert.Equal(1, calls);
        }
        finally
        {
            store.AllowWrite.TrySetResult();
            using var response = await first;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Persistence_failure_preserves_the_provider_response_and_blocks_local_retries(bool timedOut)
    {
        store.WriteFailure = timedOut ? new OperationCanceledException("storage timeout") : new IOException("storage unavailable");
        var calls = 0;
        using var client = Client(() => { calls++; return Limited("120", "10"); });
        using var response = await client.GetAsync("https://api.prej.app/v1/offers");
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        await Assert.ThrowsAnyAsync<HttpRequestException>(() => client.GetAsync("https://api.prej.app/v1/offers"));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Daily_exhaustion_blocks_other_stores_and_recreated_clients_until_utc_reset()
    {
        var calls = 0;
        using (var client = Client(() => { calls++; return Limited("60", "0"); }))
            using (await client.GetAsync("https://api.prej.app/v1/offers?chain=nemlig")) { }
        using var restarted = Client(() => { calls++; return new(HttpStatusCode.OK); }, cooldownMemory: new());
        clock.Advance(Duration.FromMinutes(6));
        await Assert.ThrowsAnyAsync<HttpRequestException>(() => restarted.GetAsync("https://api.prej.app/v1/offers?chain=rema1000"));
        Assert.Equal(1, calls);
        Assert.Equal(Instant.FromUtc(2026, 10, 25, 0, 0).ToDateTimeUtc(), Assert.Single(store.Blocks).Value);
        Assert.DoesNotContain("test-key", Assert.Single(store.Blocks).Key);
        clock.Reset(Instant.FromUtc(2026, 10, 25, 0, 0));
        using var success = await restarted.GetAsync("https://api.prej.app/v1/offers?chain=rema1000");
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        Assert.Equal(2, calls);
    }

    [Theory]
    [InlineData("120")]
    [InlineData("Sat, 24 Oct 2026 18:02:00 GMT")]
    public async Task Burst_limit_honors_seconds_or_http_date_without_waiting_until_tomorrow(string retryAfter)
    {
        var calls = 0;
        using var client = Client(() => ++calls == 1 ? Limited(retryAfter, "10") : new(HttpStatusCode.OK));
        using (await client.GetAsync("https://api.prej.app/v1/chains")) { }
        clock.Advance(Duration.FromSeconds(119));
        await Assert.ThrowsAnyAsync<HttpRequestException>(() => client.GetAsync("https://api.prej.app/v1/offers"));
        Assert.Equal(1, calls);
        clock.Advance(Duration.FromSeconds(1));
        using var response = await client.GetAsync("https://api.prej.app/v1/offers");
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Last_successful_response_is_returned_but_prevents_further_calls_that_day()
    {
        var calls = 0;
        using var client = Client(() =>
        {
            calls++;
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("useful last response") };
            response.Headers.TryAddWithoutValidation("X-RateLimit-Remaining-Day", "0");
            return response;
        });
        using var response = await client.GetAsync("https://api.prej.app/v1/offers");
        Assert.Equal("useful last response", await response.Content.ReadAsStringAsync());
        await Assert.ThrowsAnyAsync<HttpRequestException>(() => client.GetAsync("https://api.prej.app/v1/products/batch"));
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    public async Task Unspecified_429_reset_conservatively_blocks_until_next_utc_day(string retryAfter)
    {
        using var client = Client(() => Limited(retryAfter, null));
        using (await client.GetAsync("https://api.prej.app/v1/offers")) { }
        Assert.Equal(Instant.FromUtc(2026, 10, 25, 0, 0).ToDateTimeUtc(), Assert.Single(store.Blocks).Value);
    }

    [Fact]
    public async Task Different_api_key_does_not_inherit_the_exhausted_keys_block()
    {
        using var exhausted = Client(() => Limited("60", "0"));
        using (await exhausted.GetAsync("https://api.prej.app/v1/offers")) { }
        using var other = Client(() => new(HttpStatusCode.OK), "other-key");
        using var response = await other.GetAsync("https://api.prej.app/v1/offers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Caller_cancellation_after_response_does_not_lose_the_persisted_quota_block()
    {
        using var cancellation = new CancellationTokenSource();
        using var client = Client(() => { cancellation.Cancel(); return Limited("120", "0"); });
        try { using var response = await client.GetAsync("https://api.prej.app/v1/offers", cancellation.Token); }
        catch (OperationCanceledException) { }
        Assert.Equal(Instant.FromUtc(2026, 10, 25, 0, 0).ToDateTimeUtc(), Assert.Single(store.Blocks).Value);
    }

    private HttpClient Client(Func<HttpResponseMessage> response, string key = "test-key", PrejRateLimitMemory cooldownMemory = null) => new(
        new PrejRateLimitHandler(store, new() { ApiKey = key }, clock, cooldownMemory ?? memory) { InnerHandler = new ResponseTransport(response) });

    private static HttpResponseMessage Limited(string retryAfter, string remaining)
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        if (retryAfter is not null) response.Headers.TryAddWithoutValidation("Retry-After", retryAfter);
        if (remaining is not null) response.Headers.TryAddWithoutValidation("X-RateLimit-Remaining-Day", remaining);
        return response;
    }

    private sealed class ResponseTransport(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond());
    }

    private sealed class MemoryRateLimits : IPrejRateLimitStore
    {
        public Dictionary<string, DateTime> Blocks { get; } = [];
        public TaskCompletionSource WriteStarted;
        public TaskCompletionSource AllowWrite;
        public Exception WriteFailure;
        public Task<DateTime?> ReadRateLimitAsync(string scope, CancellationToken cancellationToken)
            => Task.FromResult(Blocks.TryGetValue(scope, out var value) ? (DateTime?)value : null);
        public async Task ExtendRateLimitAsync(string scope, DateTime retryAfter, CancellationToken cancellationToken)
        {
            WriteStarted?.TrySetResult();
            if (AllowWrite is not null) await AllowWrite.Task.WaitAsync(cancellationToken);
            if (WriteFailure is not null) throw WriteFailure;
            cancellationToken.ThrowIfCancellationRequested();
            if (!Blocks.TryGetValue(scope, out var previous) || previous < retryAfter) Blocks[scope] = retryAfter;
        }
    }
}
