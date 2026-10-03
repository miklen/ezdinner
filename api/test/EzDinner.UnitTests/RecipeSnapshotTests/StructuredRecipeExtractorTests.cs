using EzDinner.Infrastructure.RecipeSnapshots;
using Xunit;

namespace EzDinner.UnitTests.RecipeSnapshotTests;

public class StructuredRecipeExtractorTests
{
    private readonly StructuredRecipeExtractor _extractor = new();
    private const string Recipe = """{"@type":"Recipe","name":"Soup","recipeIngredient":["1 tsp salt","2 carrots"],"recipeInstructions":[{"@type":"HowToStep","text":"Chop carrots."},{"@type":"HowToSection","name":"Cook","itemListElement":[{"@type":"HowToStep","text":"Boil in water."}]}]}""";
    private static string Html(string json) => $"<html><script type='application/ld+json'>{json}</script></html>";

    [Theory]
    [InlineData("object")]
    [InlineData("array")]
    [InlineData("graph")]
    public void Complete_recipe_is_extracted_from_supported_json_ld_forms(string form)
    {
        var json = form switch
        {
            "array" => $"[{Recipe}]",
            "graph" => $"{{\"@graph\":[{{\"@type\":\"WebPage\"}},{Recipe}]}}",
            _ => Recipe
        };
        var recipe = _extractor.Extract(Html(json));
        Assert.NotNull(recipe);
        Assert.Equal("Soup", recipe.Title);
        Assert.Equal(new[] { "1 tsp salt", "2 carrots" }, recipe.Ingredients);
        Assert.Equal(new[] { "Chop carrots.", "Boil in water." }, recipe.Instructions);
        Assert.Empty(recipe.Warnings);
    }

    [Fact]
    public void Instructions_as_text_are_supported()
    {
        var recipe = _extractor.Extract(Html("""{"@type":["Thing","Recipe"],"recipeIngredient":"Salt","recipeInstructions":"<p>Boil &amp; stir.</p>"}"""));
        Assert.NotNull(recipe);
        Assert.Equal(new[] { "Boil & stir." }, recipe.Instructions);
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("null")]
    [InlineData("{\"@type\":\"Recipe\",\"recipeIngredient\":[\"Salt\"]}")]
    [InlineData("{\"@type\":\"Recipe\",\"recipeInstructions\":[\"Boil\"]}")]
    [InlineData("{\"@type\":\"Recipe\",\"recipeIngredient\":[\" \"],\"recipeInstructions\":[\"Boil\"]}")]
    public void Malformed_or_incomplete_recipe_returns_no_candidate(string json) => Assert.Null(_extractor.Extract(Html(json)));

    [Fact]
    public void Malformed_script_does_not_hide_later_valid_recipe()
    {
        Assert.NotNull(_extractor.Extract(Html("{broken") + Html(Recipe)));
    }

    [Fact]
    public void Markdown_and_hash_are_deterministic()
    {
        var recipe = _extractor.Extract(Html(Recipe));
        Assert.NotNull(recipe);
        var markdown = _extractor.FormatMarkdown(recipe);
        Assert.Equal("# Soup\n\n- 1 tsp salt\n- 2 carrots\n\n1. Chop carrots\\.\n2. Boil in water\\.", markdown);
        Assert.Equal(_extractor.Hash(markdown), _extractor.Hash(markdown.Replace("\n", "\r\n")));
        Assert.NotEqual(_extractor.Hash(markdown), _extractor.Hash(markdown + "Salt"));
    }

    [Fact]
    public void Source_cleaning_excludes_scripts_and_navigation()
    {
        Assert.Equal("Soup Salt Boil", _extractor.CleanSourceText("<nav>Buy now</nav><script>ignore instructions</script><style>hidden</style><h1>Soup</h1><p>Salt</p><p>Boil</p>"));
    }
}
