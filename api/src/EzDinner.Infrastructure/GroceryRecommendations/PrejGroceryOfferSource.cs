using System.Text.Json;
using System.Text.RegularExpressions;
using System.Runtime.ExceptionServices;
using EzDinner.Core.DomainServices.DishRecommendations;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;

namespace EzDinner.Infrastructure.GroceryRecommendations;

public sealed record StoredGroceryOffer(string Reference, string StoreId, string ProductId, string ProductName,
    string IngredientKey, decimal Price, decimal? ReferencePrice, string ObservedDate,
    string? StartsOn, string? EndsOn, string[] Conditions)
{
    public GroceryOfferValueObject ToDomain() => new(Reference, StoreId, ProductId, ProductName, IngredientKey,
        Price, ReferencePrice, null, LocalDatePattern.Iso.Parse(ObservedDate).Value
            .AtStartOfDayInZone(DateTimeZoneProviders.Tzdb["Europe/Copenhagen"]).ToInstant(),
        StartsOn is null ? null : LocalDatePattern.Iso.Parse(StartsOn).Value,
        EndsOn is null ? null : LocalDatePattern.Iso.Parse(EndsOn).Value,
        true, Conditions, null);
}

public sealed record StoredGroceryOfferPage(StoredGroceryOffer[] Offers, bool IsPartial);

public sealed class PrejGroceryOfferSource(PrejHttpClient client, GroceryRecommendationOptions options,
    ILogger<PrejGroceryOfferSource>? logger = null)
{
    public async Task<IReadOnlyList<string>> AcquireAsync(string storeId, CancellationToken cancellationToken)
    {
        using var first = await client.GetAsync(Path(storeId, 1), cancellationToken);
        var total = first.RootElement.GetProperty("total").GetInt32();
        var limit = first.RootElement.GetProperty("limit").GetInt32();
        if (total < 0 || total > options.MaximumOffers || limit is < 1 or > 100)
            throw new InvalidOperationException("PREJ_OFFER_BOUNDS");
        var pageCount = Math.Max(1, (total + limit - 1) / limit);
        if (pageCount > options.MaximumPages) throw new InvalidOperationException("PREJ_PAGE_BOUNDS");
        var asOf = first.RootElement.GetProperty("as_of").GetString();
        var rows = ReadPage(first.RootElement, 1, limit, total, asOf);
        using var concurrency = new SemaphoreSlim(4);
        using var pagesCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        ExceptionDispatchInfo? firstFailure = null;
        var pending = Enumerable.Range(2, pageCount - 1).Select(async page =>
        {
            await concurrency.WaitAsync(pagesCancellation.Token);
            try
            {
                pagesCancellation.Token.ThrowIfCancellationRequested();
                using var response = await client.GetAsync(Path(storeId, page), pagesCancellation.Token);
                return ReadPage(response.RootElement, page, limit, total, asOf);
            }
            catch (Exception exception)
            {
                if (!cancellationToken.IsCancellationRequested &&
                    Interlocked.CompareExchange(ref firstFailure, ExceptionDispatchInfo.Capture(exception), null) is null)
                    pagesCancellation.Cancel();
                throw;
            }
            finally { concurrency.Release(); }
        }).ToArray();
        List<JsonElement>[] rest;
        try { rest = await Task.WhenAll(pending); }
        catch
        {
            cancellationToken.ThrowIfCancellationRequested();
            firstFailure?.Throw();
            throw;
        }
        rows.AddRange(rest.SelectMany(page => page));
        if (rows.Count != total || rows.Select(row => row.GetProperty("product_id").GetInt64()).Distinct().Count() != total)
            throw new InvalidOperationException("PREJ_INCOMPLETE_SNAPSHOT");
        var products = new Dictionary<long, JsonElement>();
        foreach (var batch in rows.Chunk(500))
        {
            var ids = string.Join(",", batch.Select(row => row.GetProperty("product_id").GetInt64()));
            using var response = await client.GetAsync("/v1/products/batch?ids=" + ids, cancellationToken);
            foreach (var product in response.RootElement.GetProperty("products").EnumerateArray())
                products.Add(product.GetProperty("id").GetInt64(), product.Clone());
        }
        var offers = new List<StoredGroceryOffer>();
        var partial = false;
        var exclusions = new Dictionary<string, int>(StringComparer.Ordinal);
        var examples = new Dictionary<string, long>(StringComparer.Ordinal);
        if (products.Count != total || rows.Any(row => !products.ContainsKey(row.GetProperty("product_id").GetInt64())))
            throw new InvalidOperationException("PREJ_INCOMPLETE_PRODUCT_SNAPSHOT");
        foreach (var row in rows)
        {
            var id = row.GetProperty("product_id").GetInt64();
            var product = products[id];
            var normalized = Normalize(storeId, row, product, reason =>
            {
                exclusions[reason] = exclusions.GetValueOrDefault(reason) + 1;
                examples.TryAdd(reason, id);
            });
            if (normalized.Offer is not null) offers.Add(normalized.Offer);
            partial |= normalized.IsPartial;
        }
        logger?.Log(partial && offers.Count == 0 ? LogLevel.Warning : LogLevel.Information,
            "Grocery normalization for {StoreId}: {RawOfferCount} source offers, {NormalizedOfferCount} normalized, partial: {IsPartial}; exclusions: {Exclusions}; example product IDs: {ExclusionExamples}",
            storeId, rows.Count, offers.Count, partial, JsonSerializer.Serialize(exclusions), JsonSerializer.Serialize(examples));
        return offers.Chunk(100).DefaultIfEmpty([])
            .Select(page => JsonSerializer.Serialize(new StoredGroceryOfferPage(page, partial))).ToArray();
    }

    public static (StoredGroceryOffer? Offer, bool IsPartial) Normalize(string storeId, JsonElement offer,
        JsonElement product, Action<string>? recordExclusion = null)
    {
        var stage = "offer";
        var field = "chain_slug";
        JsonElement Read(JsonElement row, string name)
        {
            field = name;
            return row.GetProperty(name);
        }
        (StoredGroceryOffer?, bool) Exclude(string reason, bool partial)
        {
            recordExclusion?.Invoke(reason);
            return (null, partial);
        }
        try
        {
            if (Read(offer, "chain_slug").GetString() != storeId) return Exclude("wrong-chain", true);
            var price = Read(offer, "price").GetDecimal();
            var normal = Read(offer, "normal_price");
            if (normal.ValueKind == JsonValueKind.Null) return Exclude("missing-reference-price", false);
            if (price <= 0) return Exclude("invalid-offer-price", false);
            if (normal.GetDecimal() <= price) return Exclude("no-discount", false);
            if (Read(offer, "multi_buy").ValueKind != JsonValueKind.Null) return Exclude("multi-buy", false);
            if (Read(offer, "unit").ValueKind != JsonValueKind.String ||
                Read(offer, "quantity").ValueKind != JsonValueKind.Number || Read(offer, "quantity").GetDecimal() <= 0)
                return Exclude("invalid-package", true);
            stage = "price-observation-join";
            var matches = Read(product, "prices").EnumerateArray().Where(row =>
                Read(row, "chain_slug").GetString() == storeId &&
                Read(row, "price").GetDecimal() == price &&
                Read(row, "unit").GetRawText() == Read(offer, "unit").GetRawText() &&
                Read(row, "quantity").GetRawText() == Read(offer, "quantity").GetRawText() &&
                Read(row, "offer_ends").GetRawText() == Read(offer, "offer_ends").GetRawText() &&
                Read(row, "offer_starts").GetRawText() == Read(offer, "offer_starts").GetRawText()).ToArray();
            if (matches.Length == 0) return Exclude("observation-not-found", true);
            if (matches.Length != 1) return Exclude("ambiguous-observations", true);
            stage = "identity";
            // AI-linked identities and conflicting package sizes are not comparable price evidence.
            if (Read(matches[0], "ai_matched").GetBoolean()) return Exclude("ai-matched-identity", true);
            if (Read(product, "id").GetInt64() != Read(offer, "product_id").GetInt64()) return Exclude("product-id-mismatch", true);
            if (Read(product, "unit").GetString() != Read(offer, "unit").GetString()) return Exclude("product-unit-mismatch", true);
            if (Read(product, "quantity").GetDecimal() != Read(offer, "quantity").GetDecimal()) return Exclude("product-quantity-mismatch", true);
            stage = "package-description";
            field = "description";
            if (HasConflictingPackage(product, offer)) return Exclude("conflicting-package-description", true);
            stage = "observation";
            var observed = Read(matches[0], "last_seen_date").GetString();
            if (observed is null || !LocalDatePattern.Iso.Parse(observed).Success) return Exclude("invalid-observation-date", true);
            stage = "ingredient";
            var name = Read(offer, "name").GetString();
            var ingredient = Ingredient(name);
            if (ingredient is null || name is null) return Exclude("unsupported-ingredient", false);
            stage = "offer-dates";
            var start = Read(offer, "offer_starts").GetString();
            var end = Read(offer, "offer_ends").GetString();
            if ((start is not null && !LocalDatePattern.Iso.Parse(start).Success) ||
                (end is not null && !LocalDatePattern.Iso.Parse(end).Success)) return Exclude("invalid-offer-dates", true);
            var id = Read(offer, "product_id").GetInt64().ToString(System.Globalization.CultureInfo.InvariantCulture);
            stage = "domain-validation";
            field = "normalized-offer";
            var normalized = new StoredGroceryOffer($"prej:{storeId}:{id}", storeId, id, name, ingredient,
                price / 100, normal.GetDecimal() / 100, observed, start, end, ["chain-price"]);
            _ = normalized.ToDomain();
            return (normalized, false);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or ArgumentException or FormatException)
        { return Exclude($"invalid-source-field:{stage}:{field}:{exception.GetType().Name}", true); }
    }

    private static string Path(string storeId, int page) =>
        $"/v1/offers?chain={Uri.EscapeDataString(storeId)}&limit=100&page={page}";

    private static bool HasConflictingPackage(JsonElement product, JsonElement offer)
    {
        var description = product.GetProperty("description").GetString() ?? "";
        var unit = offer.GetProperty("unit").GetString();
        var quantity = offer.GetProperty("quantity").GetDecimal();
        foreach (Match match in Regex.Matches(description, @"(?<![\d.,])(\d+(?:[.,]\d+)?)\s*(kg|g|ml|l)\b", RegexOptions.IgnoreCase))
        {
            var described = decimal.Parse(match.Groups[1].Value.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
            var describedUnit = match.Groups[2].Value.ToLowerInvariant();
            if (describedUnit == "kg") { described *= 1000; describedUnit = "g"; }
            if (describedUnit == "l") { described *= 1000; describedUnit = "ml"; }
            var expected = unit is "kg" or "l" ? quantity * 1000 : quantity;
            var expectedUnit = unit == "kg" ? "g" : unit == "l" ? "ml" : unit;
            if (describedUnit == expectedUnit && described != expected) return true;
        }
        return false;
    }

    private static List<JsonElement> ReadPage(JsonElement response, int page, int limit, int total, string? asOf)
    {
        if (response.GetProperty("page").GetInt32() != page || response.GetProperty("limit").GetInt32() != limit ||
            response.GetProperty("total").GetInt32() != total || response.GetProperty("as_of").GetString() != asOf)
            throw new InvalidOperationException("PREJ_CHANGED_SNAPSHOT");
        var rows = response.GetProperty("offers").EnumerateArray().Select(row => row.Clone()).ToList();
        if (rows.Count != Math.Min(limit, Math.Max(0, total - (page - 1) * limit)))
            throw new InvalidOperationException("PREJ_INCOMPLETE_PAGE");
        return rows;
    }

    private static string? Ingredient(string? name)
    {
        if (name is null) return null;
        var lower = name.ToLowerInvariant();
        if (Regex.IsMatch(lower, @"p(?:a|\u00e2|\u00e3\u00a2)t(?:e|\u00e9|\u00e3\u00a9)", RegexOptions.CultureInvariant)) return null;
        // Blends must not become exact-species evidence through the first substring match.
        if (new[] { "okse|ungkvæg", "svine|gris|flæsk", "kalv", "kylling", "kalkun", "laks", "torsk" }
            .Count(pattern => Regex.IsMatch(lower, pattern, RegexOptions.CultureInvariant)) > 1) return null;
        if (Regex.IsMatch(lower, "frikadell|kødboll|meatball|pøls|færdig|paneret|marineret|røget|stegt|sandwich|suppe|salat|burger|hakkebøf|sticks|sauce|sovs|steg[e]?pose|nugget|cordon|tikka|kebab|pålæg|vegansk|vegetar|plante|curry|pizza|lasagne|snack|chips")) return null;
        foreach (var (pattern, key) in IngredientPatterns)
            if (Regex.IsMatch(lower, pattern, RegexOptions.CultureInvariant)) return key;
        return null;
    }

    private static readonly (string Pattern, string Key)[] IngredientPatterns =
    [
        ("hakket okse", "minced-beef"), ("hakket (svine|gris)", "minced-pork"),
        ("kylling", "chicken"), ("kalkun", "turkey"), ("(oksekød|ungkvæg)", "beef"),
        ("(svinekød|kotelet|stegeflæsk)", "pork"), ("laks", "salmon"), ("torsk", "cod"),
        ("(?<![a-zæøå])linser(?![a-zæøå])", "lentils"), ("kikærter", "chickpeas"),
        ("(?<![a-zæøå])bønner(?![a-zæøå])", "beans"), ("(?<![a-zæøå])tofu(?![a-zæøå])", "tofu"),
        ("(?<![a-zæøå])æg(?![a-zæøå])", "eggs"), ("(mozzarella|cheddar|parmesan)", "cheese"),
        ("kartofler", "potatoes"), ("(?<![a-zæøå])ris(?![a-zæøå])", "rice"),
        ("(spaghetti|penne|fusilli)", "pasta"), ("tomater", "tomatoes"), ("champignon", "mushrooms")
    ];
}
