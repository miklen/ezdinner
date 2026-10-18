namespace EzDinner.Core.DomainServices.DishRecommendations;

public static class IngredientVocabularyFactory
{
    public const string Version = "2";
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
    private static readonly IReadOnlyDictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["hakket oksekød"] = "minced-beef", ["hakket svinekød"] = "minced-pork",
        ["hakket kød"] = "minced-meat", ["kylling"] = "chicken", ["laks"] = "salmon",
        ["torsk"] = "cod", ["linser"] = "lentils", ["kartofler"] = "potatoes",
        ["kikærter"] = "chickpeas", ["frikadeller"] = "prepared-meatballs",
        ["oksekød"] = "beef", ["svinekød"] = "pork", ["fjerkræ"] = "poultry",
        ["kalkun"] = "turkey", ["fisk"] = "fish",
        ["bønner"] = "beans", ["bønne"] = "beans", ["bean"] = "beans",
        ["kikært"] = "chickpeas", ["chickpea"] = "chickpeas", ["lentil"] = "lentils",
        ["æg"] = "eggs", ["egg"] = "eggs", ["ost"] = "cheese",
        ["feta"] = "cheese", ["mozzarella"] = "cheese", ["parmesan"] = "cheese", ["cheddar"] = "cheese",
        ["kartoffel"] = "potatoes", ["potato"] = "potatoes", ["ris"] = "rice",
        ["tomat"] = "tomatoes", ["tomater"] = "tomatoes", ["tomato"] = "tomatoes",
        ["cherrytomat"] = "tomatoes", ["cherrytomater"] = "tomatoes",
        ["svamp"] = "mushrooms", ["svampe"] = "mushrooms", ["mushroom"] = "mushrooms",
        ["champignon"] = "mushrooms", ["champignoner"] = "mushrooms",
        ["pølse"] = "sausages", ["pølser"] = "sausages", ["sausage"] = "sausages"
    };

    public static IReadOnlyCollection<string> Keys => Parents.Keys.ToArray();
    public static bool IsKnown(string key) => Parents.ContainsKey(key);
    public static string? Canonicalize(string label) => Parents.ContainsKey(label) ? label
        : Aliases.TryGetValue(label.Trim(), out var key) ? key : null;
    public static bool IsIncidental(string key) => key == "salt";
    public static bool SupportsExplicitClaim(string key, string excerpt)
    {
        var labels = Aliases.Where(alias => alias.Value == key).Select(alias => alias.Key).Append(key.Replace('-', ' '));
        return labels.Any(label => System.Text.RegularExpressions.Regex.IsMatch(excerpt,
            $@"(?<!\p{{L}}){System.Text.RegularExpressions.Regex.Escape(label)}(?!\p{{L}})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant));
    }
    public static string OverlapGroup(string key) => Parents.TryGetValue(key, out var parent) ? parent ?? key : key;
    public static double Compatibility(string ingredient, string product)
    {
        if (ingredient == product) return 1;
        return Parents.TryGetValue(product, out var parent) && parent == ingredient ? 0.7 : 0;
    }
}
