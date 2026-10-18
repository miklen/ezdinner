namespace EzDinner.Core.DomainServices.DishRecommendations;

public static class IngredientVocabularyFactory
{
    public const string Version = "3";
    private static readonly IReadOnlyDictionary<string, string?> Parents = new Dictionary<string, string?>
    {
        ["minced-meat"] = null, ["minced-beef"] = "minced-meat", ["minced-pork"] = "minced-meat",
        ["poultry"] = null, ["chicken"] = "poultry", ["turkey"] = "poultry",
        ["beef"] = null, ["pork"] = null, ["fish"] = null, ["salmon"] = "fish", ["cod"] = "fish",
        ["lentils"] = null, ["beans"] = null, ["chickpeas"] = null, ["tofu"] = null,
        ["eggs"] = null, ["cheese"] = null, ["potatoes"] = null, ["rice"] = null,
        ["pasta"] = null, ["tomatoes"] = null, ["mushrooms"] = null,
        ["prepared-meatballs"] = null, ["sausages"] = null, ["salt"] = null
    };
    public static IReadOnlyCollection<string> Keys => Parents.Keys.ToArray();
    public static bool IsKnown(string key) => Parents.ContainsKey(key);
    public static bool IsIncidental(string key) => key == "salt";
    public static string OverlapGroup(string key) => Parents.TryGetValue(key, out var parent) ? parent ?? key : key;
    public static double Compatibility(string ingredient, string product)
    {
        if (ingredient == product) return 1;
        return Parents.TryGetValue(product, out var parent) && parent == ingredient ? 0.7 : 0;
    }
}
