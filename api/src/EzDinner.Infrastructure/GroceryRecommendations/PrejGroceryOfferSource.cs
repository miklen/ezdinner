using System.Text.Json;
using System.Text.RegularExpressions;
using EzDinner.Core.DomainServices.DishRecommendations;
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

public sealed class PrejGroceryOfferSource(PrejHttpClient client, GroceryRecommendationOptions options)
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
        var rest = await Task.WhenAll(Enumerable.Range(2, pageCount - 1).Select(async page =>
        {
            await concurrency.WaitAsync(cancellationToken);
            try
            {
                using var response = await client.GetAsync(Path(storeId, page), cancellationToken);
                return ReadPage(response.RootElement, page, limit, total, asOf);
            }
            finally { concurrency.Release(); }
        }));
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
        if (products.Count != total || rows.Any(row => !products.ContainsKey(row.GetProperty("product_id").GetInt64())))
            throw new InvalidOperationException("PREJ_INCOMPLETE_PRODUCT_SNAPSHOT");
        foreach (var row in rows)
        {
            var id = row.GetProperty("product_id").GetInt64();
            var product = products[id];
            var normalized = Normalize(storeId, row, product);
            if (normalized.Offer is not null) offers.Add(normalized.Offer);
            partial |= normalized.IsPartial;
        }
        return offers.Chunk(100).DefaultIfEmpty([])
            .Select(page => JsonSerializer.Serialize(new StoredGroceryOfferPage(page, partial))).ToArray();
    }

    public static (StoredGroceryOffer? Offer, bool IsPartial) Normalize(string storeId, JsonElement offer, JsonElement product)
    {
        try
        {
            if (offer.GetProperty("chain_slug").GetString() != storeId) return (null, true);
            var price = offer.GetProperty("price").GetDecimal();
            var normal = offer.GetProperty("normal_price");
            if (normal.ValueKind == JsonValueKind.Null || price <= 0 || normal.GetDecimal() <= price) return (null, false);
            if (offer.GetProperty("multi_buy").ValueKind != JsonValueKind.Null) return (null, false);
            if (offer.GetProperty("unit").ValueKind != JsonValueKind.String ||
                offer.GetProperty("quantity").ValueKind != JsonValueKind.Number || offer.GetProperty("quantity").GetDecimal() <= 0)
                return (null, true);
            var matches = product.GetProperty("prices").EnumerateArray().Where(row =>
                row.GetProperty("chain_slug").GetString() == storeId &&
                row.GetProperty("price").GetDecimal() == price &&
                row.GetProperty("unit").GetRawText() == offer.GetProperty("unit").GetRawText() &&
                row.GetProperty("quantity").GetRawText() == offer.GetProperty("quantity").GetRawText() &&
                row.GetProperty("offer_ends").GetRawText() == offer.GetProperty("offer_ends").GetRawText() &&
                row.GetProperty("offer_starts").GetRawText() == offer.GetProperty("offer_starts").GetRawText()).ToArray();
            if (matches.Length != 1) return (null, true);
            // AI-linked identities and conflicting package sizes are not comparable price evidence.
            if (matches[0].GetProperty("ai_matched").GetBoolean() ||
                product.GetProperty("id").GetInt64() != offer.GetProperty("product_id").GetInt64() ||
                product.GetProperty("unit").GetString() != offer.GetProperty("unit").GetString() ||
                product.GetProperty("quantity").GetDecimal() != offer.GetProperty("quantity").GetDecimal() ||
                HasConflictingPackage(product, offer)) return (null, true);
            var observed = matches[0].GetProperty("last_seen_date").GetString();
            if (observed is null || !LocalDatePattern.Iso.Parse(observed).Success) return (null, true);
            var name = offer.GetProperty("name").GetString();
            var ingredient = Ingredient(name);
            if (ingredient is null || name is null) return (null, false);
            var start = offer.GetProperty("offer_starts").GetString();
            var end = offer.GetProperty("offer_ends").GetString();
            if ((start is not null && !LocalDatePattern.Iso.Parse(start).Success) ||
                (end is not null && !LocalDatePattern.Iso.Parse(end).Success)) return (null, true);
            var id = offer.GetProperty("product_id").GetInt64().ToString(System.Globalization.CultureInfo.InvariantCulture);
            var normalized = new StoredGroceryOffer($"prej:{storeId}:{id}", storeId, id, name, ingredient,
                price / 100, normal.GetDecimal() / 100, observed, start, end, ["chain-price"]);
            _ = normalized.ToDomain();
            return (normalized, false);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or ArgumentException or FormatException)
        { return (null, true); }
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
