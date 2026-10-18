using System.Text.RegularExpressions;
using EzDinner.Core.DomainServices.DishRecommendations;
using EzDinner.Query.Core.DishRecommendationQueries;

namespace EzDinner.Infrastructure.DishRecommendations;

public static class DishIngredientSourceValidator
{
    private static readonly string[] AnimalIngredients = ["minced-meat", "minced-beef", "minced-pork", "poultry", "chicken", "turkey", "beef", "pork", "fish", "salmon", "cod", "prepared-meatballs", "sausages"];
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    public static bool ContradictsSources(string key, IReadOnlyList<DishRecommendationSource> sources)
    {
        var notes = sources.Where(source => source.Reference.EndsWith(":notes", StringComparison.Ordinal))
            .Select(source => source.Text).ToArray();
        if (Contradicts(key, notes)) return true;
        if (notes.Any(note => IngredientVocabularyFactory.SupportsExplicitClaim(key, note))) return false;
        return Contradicts(key, sources.Where(source => source.Reference.EndsWith(":recipe", StringComparison.Ordinal))
            .Select(source => source.Text));
    }

    private static bool Contradicts(string key, IEnumerable<string> sources)
    {
        var texts = sources.ToArray();
        if (AnimalIngredients.Contains(key) && texts.Any(note => Regex.IsMatch(note,
                @"\b(vegetarian|vegetarisk|vegetar|vegan|vegansk|meatless|meat.free|kødfri)\b", Options))) return true;
        return texts.Any(note => ExcludedClauses(note).Any(clause =>
            ExcludedKeys(key).Any(excluded => IngredientVocabularyFactory.SupportsExplicitClaim(excluded, clause)) ||
            (AnimalIngredients.Contains(key) && Regex.IsMatch(clause, @"\b(meat|kød)\b", Options))));
    }

    private static IEnumerable<string> ExcludedKeys(string key)
    {
        yield return key;
        if (key == "minced-beef") yield return "beef";
        if (key == "minced-pork") yield return "pork";
        if (key is "chicken" or "turkey") yield return "poultry";
        if (key is "salmon" or "cod") yield return "fish";
    }

    private static IEnumerable<string> ExcludedClauses(string notes)
    {
        foreach (Match match in Regex.Matches(notes,
            @"(?:\b(?:no|without|omit|avoid|uden|ingen|udelad|undgå)|instead\s+of|i\s+stedet\s+for)\s+([^.!?;\r\n]+)", Options))
            yield return Regex.Split(match.Groups[1].Value, @"\b(?:use|add|brug|tilføj|but|men)\b", Options)[0];
        foreach (Match match in Regex.Matches(notes,
            @"\b(?:replace|substitute|erstat)\s+(.+?)\s+(?:with|by|med)\b", Options))
            yield return match.Groups[1].Value;
    }
}
