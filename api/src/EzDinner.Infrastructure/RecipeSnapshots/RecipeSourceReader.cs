using System.Net;
using System.Text;
using EzDinner.Application.Commands.RecipeSnapshots;

namespace EzDinner.Infrastructure.RecipeSnapshots;

public sealed class RecipeSourceReader(HttpClient client, IRecipeHostResolver resolver, RecipeRetrievalOptions limits) : IRecipeSourceReader
{
    public async Task<RecipeSource> ReadAsync(string sourceUrl, CancellationToken cancellationToken)
    {
        limits.Validate();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(limits.TimeoutSeconds));
        try
        {
            return await RetrieveAsync(sourceUrl, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new RecipeImportException("RECIPE_FETCH_TIMEOUT");
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Net.Sockets.SocketException or IOException)
        {
            throw new RecipeImportException("RECIPE_FETCH_FAILED");
        }
    }

    private async Task<RecipeSource> RetrieveAsync(string sourceUrl, CancellationToken cancellationToken)
    {
        var current = ValidateUrl(sourceUrl);
        for (var redirects = 0; ; redirects++)
        {
            PublicRecipeHostResolver.EnsurePublic(await resolver.ResolveAsync(current.IdnHost, cancellationToken));
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            request.Headers.UserAgent.ParseAdd("EzDinner-RecipeCapture/1.0");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (IsRedirect(response.StatusCode))
            {
                if (redirects >= limits.MaximumRedirects)
                    throw new RecipeImportException("RECIPE_TOO_MANY_REDIRECTS");
                var location = response.Headers.Location;
                if (location is null) throw new RecipeImportException("RECIPE_FETCH_FAILED");
                current = ValidateUrl(new Uri(current, location).OriginalString);
                continue;
            }
            if (!response.IsSuccessStatusCode) throw new RecipeImportException("RECIPE_FETCH_FAILED");
            var mediaType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
            if (mediaType is not ("text/html" or "application/xhtml+xml" or "text/plain"))
                throw new RecipeImportException("RECIPE_UNSUPPORTED_CONTENT");
            if (response.Content.Headers.ContentLength > limits.MaximumBytes)
                throw new RecipeImportException("RECIPE_SOURCE_TOO_LARGE");
            return new RecipeSource(await ReadBoundedAsync(response.Content, cancellationToken));
        }
    }

    private async Task<string> ReadBoundedAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + count > limits.MaximumBytes)
                throw new RecipeImportException("RECIPE_SOURCE_TOO_LARGE");
            buffer.Write(chunk, 0, count);
        }
        var encoding = Encoding.UTF8;
        var charset = content.Headers.ContentType?.CharSet;
        if (!string.IsNullOrWhiteSpace(charset))
        {
            try { encoding = Encoding.GetEncoding(charset.Trim('"')); }
            catch (ArgumentException) { throw new RecipeImportException("RECIPE_UNSUPPORTED_CONTENT"); }
        }
        return encoding.GetString(buffer.ToArray());
    }

    private static Uri ValidateUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo) || string.IsNullOrWhiteSpace(uri.Host))
            throw new RecipeImportException("RECIPE_UNSAFE_URL");
        return uri;
    }

    private static bool IsRedirect(HttpStatusCode code) => (int)code is 301 or 302 or 303 or 307 or 308;
}
