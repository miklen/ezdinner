using AngleSharp.Html.Parser;
using EzDinner.Application.Commands.RecipeSnapshots;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EzDinner.Infrastructure.RecipeSnapshots;

public sealed class StructuredRecipeExtractor : IStructuredRecipeExtractor
{
    public RecipeExtractionResult? Extract(string html)
    {
        using var document = new HtmlParser().ParseDocument(html);
        foreach (var script in document.QuerySelectorAll("script[type='application/ld+json']"))
        {
            try
            {
                using var json = JsonDocument.Parse(script.TextContent, new JsonDocumentOptions { MaxDepth = 32 });
                foreach (var recipe in FindRecipes(json.RootElement))
                {
                    var ingredients = ReadStrings(recipe, "recipeIngredient");
                    var instructions = ReadStrings(recipe, "recipeInstructions");
                    if (ingredients.Count == 0 || instructions.Count == 0) continue;
                    var title = ReadStrings(recipe, "name").FirstOrDefault();
                    return new RecipeExtractionResult(title, ingredients, instructions, Array.Empty<string>());
                }
            }
            catch (JsonException) { }
        }
        return null;
    }

    private static IEnumerable<JsonElement> FindRecipes(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
                foreach (var recipe in FindRecipes(child)) yield return recipe;
            yield break;
        }
        if (element.ValueKind != JsonValueKind.Object) yield break;
        if (element.TryGetProperty("@type", out var type) && HasRecipeType(type)) yield return element;
        foreach (var property in element.EnumerateObject())
            foreach (var recipe in FindRecipes(property.Value)) yield return recipe;
    }

    private static bool HasRecipeType(JsonElement type)
    {
        if (type.ValueKind == JsonValueKind.Array) return type.EnumerateArray().Any(HasRecipeType);
        if (type.ValueKind != JsonValueKind.String) return false;
        return type.GetString() is "Recipe" or "https://schema.org/Recipe" or "http://schema.org/Recipe";
    }

    private static IReadOnlyList<string> ReadStrings(JsonElement recipe, string name) =>
        recipe.TryGetProperty(name, out var value) ? FlattenText(value).ToArray() : Array.Empty<string>();

    private static IEnumerable<string> FlattenText(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            using var fragment = new HtmlParser().ParseDocument(element.GetString()!);
            var text = NormalizeText(fragment.Body?.TextContent ?? "");
            if (text.Length > 0) yield return text;
            yield break;
        }
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
                foreach (var text in FlattenText(child)) yield return text;
            yield break;
        }
        if (element.ValueKind != JsonValueKind.Object) yield break;
        if (element.TryGetProperty("itemListElement", out var steps))
        {
            foreach (var text in FlattenText(steps)) yield return text;
            yield break;
        }
        if (element.TryGetProperty("text", out var content))
            foreach (var text in FlattenText(content)) yield return text;
    }

    public string CleanSourceText(string html)
    {
        using var document = new HtmlParser().ParseDocument(html);
        foreach (var element in document.QuerySelectorAll("script,style,nav,footer,header,iframe,svg,noscript,form"))
            element.Remove();
        foreach (var element in document.QuerySelectorAll("p,li,h1,h2,h3,div,br"))
            element.Prepend(document.CreateTextNode(" "));
        return NormalizeText(document.Body?.TextContent ?? "");
    }

    public string FormatMarkdown(RecipeExtractionResult recipe)
    {
        if (recipe.Ingredients.Count == 0 || recipe.Instructions.Count == 0)
            throw new RecipeImportException("RECIPE_EXTRACTION_FAILED");
        var title = string.IsNullOrWhiteSpace(recipe.Title) ? "" : $"# {Escape(recipe.Title)}\n\n";
        var ingredients = string.Join("\n", recipe.Ingredients.Select(ingredient => $"- {Escape(ingredient)}"));
        var instructions = string.Join("\n", recipe.Instructions.Select((instruction, index) => $"{index + 1}. {Escape(instruction)}"));
        return $"{title}{ingredients}\n\n{instructions}";
    }

    private static string Escape(string text) => Regex.Replace(
        NormalizeText(text).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"),
        @"([\\`*_{}\[\]()#+.!|~-])", @"\$1", RegexOptions.None, TimeSpan.FromSeconds(1));

    public string Hash(string source) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(source.Normalize(NormalizationForm.FormC).Replace("\r\n", "\n").Trim())));

    public static string NormalizeText(string text) => Regex.Replace(text.Normalize(NormalizationForm.FormC), @"\s+", " ", RegexOptions.None, TimeSpan.FromSeconds(1)).Trim();
}
