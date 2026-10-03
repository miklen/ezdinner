using System.Net;
using System.Text;
using EzDinner.Application.Commands.RecipeSnapshots;
using EzDinner.Infrastructure.RecipeSnapshots;
using Xunit;

namespace EzDinner.UnitTests.RecipeSnapshotTests;

public class RecipeSourceReaderTests
{
    private sealed class Resolver : IRecipeHostResolver
    {
        public Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken) =>
            Task.FromResult(new[] { IPAddress.Parse(host == "private.example" ? "10.0.0.1" : "93.184.216.34") });
    }

    private sealed class Transport(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return send(request, cancellationToken);
        }
    }

    private static HttpResponseMessage Page(string text = "recipe", string type = "text/html") =>
        new(HttpStatusCode.OK) { Content = new StringContent(text, Encoding.UTF8, type) };

    [Theory]
    [InlineData("text/html")]
    [InlineData("Text/HTML")]
    public async Task Public_html_is_returned(string mediaType)
    {
        using var transport = new Transport((_, _) => Task.FromResult(Page("<html>Soup</html>", mediaType)));
        using var client = new HttpClient(transport);
        var reader = new RecipeSourceReader(client, new Resolver(), new());
        Assert.Equal("<html>Soup</html>", (await reader.ReadAsync("https://example.com/soup", default)).Html);
        Assert.Equal(1, transport.Calls);
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/recipe")]
    [InlineData("https://user:secret@example.com/")]
    [InlineData("relative/path")]
    [InlineData("https://private.example/")]
    public async Task Unsafe_url_is_rejected_before_request(string url)
    {
        using var transport = new Transport((_, _) => Task.FromResult(Page()));
        using var client = new HttpClient(transport);
        var reader = new RecipeSourceReader(client, new Resolver(), new());
        var error = await Assert.ThrowsAsync<RecipeImportException>(() => reader.ReadAsync(url, default));
        Assert.Equal("RECIPE_UNSAFE_URL", error.Code);
        Assert.Equal(0, transport.Calls);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.1")]
    [InlineData("169.254.169.254")]
    [InlineData("172.16.0.1")]
    [InlineData("192.168.0.1")]
    [InlineData("100.64.0.1")]
    [InlineData("0.0.0.0")]
    [InlineData("224.0.0.1")]
    [InlineData("198.18.0.1")]
    [InlineData("192.0.2.1")]
    [InlineData("203.0.113.1")]
    [InlineData("::1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("fe80::1")]
    [InlineData("fc00::1")]
    [InlineData("2001:db8::1")]
    [InlineData("2002:7f00:1::")]
    public void Non_public_addresses_are_rejected(string address) =>
        Assert.False(PublicRecipeHostResolver.IsPublic(IPAddress.Parse(address)));

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("93.184.216.34")]
    [InlineData("2606:4700:4700::1111")]
    [InlineData("::ffff:8.8.8.8")]
    public void Public_addresses_are_allowed(string address) =>
        Assert.True(PublicRecipeHostResolver.IsPublic(IPAddress.Parse(address)));

    [Fact]
    public async Task Redirect_to_private_destination_is_rejected_without_following()
    {
        using var transport = new Transport((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Headers = { Location = new Uri("https://private.example/secret") }
        }));
        using var client = new HttpClient(transport);
        var reader = new RecipeSourceReader(client, new Resolver(), new());
        var error = await Assert.ThrowsAsync<RecipeImportException>(() => reader.ReadAsync("https://example.com", default));
        Assert.Equal("RECIPE_UNSAFE_URL", error.Code);
        Assert.Equal(1, transport.Calls);
    }

    [Fact]
    public async Task Relative_redirect_to_public_page_is_followed()
    {
        using var transport = new Transport((request, _) => Task.FromResult(request.RequestUri.AbsolutePath == "/"
            ? new HttpResponseMessage(HttpStatusCode.Redirect) { Headers = { Location = new Uri("/soup", UriKind.Relative) } }
            : Page("Soup")));
        using var client = new HttpClient(transport);
        var reader = new RecipeSourceReader(client, new Resolver(), new());
        Assert.Equal("Soup", (await reader.ReadAsync("https://example.com", default)).Html);
        Assert.Equal(2, transport.Calls);
    }

    [Fact]
    public async Task Redirect_loop_stops_at_configured_limit()
    {
        using var transport = new Transport((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Headers = { Location = new Uri("https://example.com/again") }
        }));
        using var client = new HttpClient(transport);
        var reader = new RecipeSourceReader(client, new Resolver(), new() { MaximumRedirects = 1 });
        var error = await Assert.ThrowsAsync<RecipeImportException>(() => reader.ReadAsync("https://example.com", default));
        Assert.Equal("RECIPE_TOO_MANY_REDIRECTS", error.Code);
        Assert.Equal(2, transport.Calls);
    }

    [Fact]
    public async Task Oversized_response_is_rejected()
    {
        using var transport = new Transport((_, _) => Task.FromResult(Page(new string('x', 100))));
        using var client = new HttpClient(transport);
        var reader = new RecipeSourceReader(client, new Resolver(), new() { MaximumBytes = 10 });
        var error = await Assert.ThrowsAsync<RecipeImportException>(() => reader.ReadAsync("https://example.com", default));
        Assert.Equal("RECIPE_SOURCE_TOO_LARGE", error.Code);
    }

    [Fact]
    public async Task Oversized_stream_without_content_length_is_rejected()
    {
        using var transport = new Transport((_, _) =>
        {
            var response = Page(new string('x', 100));
            response.Content.Headers.ContentLength = null;
            return Task.FromResult(response);
        });
        using var client = new HttpClient(transport);
        var reader = new RecipeSourceReader(client, new Resolver(), new() { MaximumBytes = 10 });
        var error = await Assert.ThrowsAsync<RecipeImportException>(() => reader.ReadAsync("https://example.com", default));
        Assert.Equal("RECIPE_SOURCE_TOO_LARGE", error.Code);
    }

    [Fact]
    public async Task Unsupported_content_type_is_rejected()
    {
        using var transport = new Transport((_, _) => Task.FromResult(Page("binary", "image/png")));
        using var client = new HttpClient(transport);
        var reader = new RecipeSourceReader(client, new Resolver(), new());
        var error = await Assert.ThrowsAsync<RecipeImportException>(() => reader.ReadAsync("https://example.com", default));
        Assert.Equal("RECIPE_UNSUPPORTED_CONTENT", error.Code);
    }

    [Fact]
    public async Task Timeout_returns_stable_failure()
    {
        using var transport = new Transport(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return Page();
        });
        using var client = new HttpClient(transport);
        var reader = new RecipeSourceReader(client, new Resolver(), new() { TimeoutSeconds = 1 });
        var error = await Assert.ThrowsAsync<RecipeImportException>(() => reader.ReadAsync("https://example.com", default));
        Assert.Equal("RECIPE_FETCH_TIMEOUT", error.Code);
    }

    [Fact]
    public async Task Remote_failure_body_is_not_exposed()
    {
        using var transport = new Transport((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("private remote details")
        }));
        using var client = new HttpClient(transport);
        var reader = new RecipeSourceReader(client, new Resolver(), new());
        var error = await Assert.ThrowsAsync<RecipeImportException>(() => reader.ReadAsync("https://example.com", default));
        Assert.Equal("RECIPE_FETCH_FAILED", error.Message);
    }
}
