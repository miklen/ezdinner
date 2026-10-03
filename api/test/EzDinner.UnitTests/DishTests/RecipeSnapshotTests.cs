using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Query.Core.DishQueries;
using Newtonsoft.Json;
using Xunit;

namespace EzDinner.UnitTests.DishTests;

public class RecipeSnapshotTests
{
    private const string SourceUrl = "https://example.com/recipe?lang=da";
    private const string Notes = "  Køb æg 🥚\r\n\n- Less salt\t\n";
    private static readonly DateTimeOffset CapturedAt = DateTimeOffset.Parse("2026-10-03T10:00:00Z");

    private static Dish DishWithNotes()
    {
        var dish = Dish.CreateNew(Guid.NewGuid(), "Soup");
        dish.SetUrl(SourceUrl);
        dish.SetNotes(Notes);
        return dish;
    }

    private static RecipeSnapshotValueObject Snapshot(string content = "# Soup\n\n- Salt\n\n1. Boil") =>
        new(content, SourceUrl, CapturedAt, new string('a', 64));

    [Fact]
    public void Capture_preserves_exact_notes_and_url()
    {
        var dish = DishWithNotes();
        var snapshot = Snapshot();
        dish.SetRecipeSnapshot(snapshot);
        Assert.Equal(snapshot, dish.RecipeSnapshot);
        Assert.Equal(Notes, dish.Notes);
        Assert.Equal(SourceUrl, dish.Url.OriginalString);
    }

    [Fact]
    public void Replacement_preserves_exact_notes_and_url()
    {
        var dish = DishWithNotes();
        dish.SetRecipeSnapshot(Snapshot());
        var replacement = Snapshot("# Soup\n\n- Pepper\n\n1. Stir");
        dish.SetRecipeSnapshot(replacement);
        Assert.Equal(replacement, dish.RecipeSnapshot);
        Assert.Equal(Notes, dish.Notes);
        Assert.Equal(SourceUrl, dish.Url.OriginalString);
    }

    [Fact]
    public void Removal_preserves_exact_notes_and_url()
    {
        var dish = DishWithNotes();
        dish.SetRecipeSnapshot(Snapshot());
        dish.RemoveRecipeSnapshot();
        Assert.Null(dish.RecipeSnapshot);
        Assert.Equal(Notes, dish.Notes);
        Assert.Equal(SourceUrl, dish.Url.OriginalString);
    }

    [Theory]
    [InlineData("https://example.com/other")]
    [InlineData(null)]
    public void Url_change_keeps_snapshot_and_rejects_stale_replacement(string url)
    {
        var dish = DishWithNotes();
        var existing = Snapshot();
        dish.SetRecipeSnapshot(existing);
        dish.SetUrl(url);
        var error = Assert.Throws<InvalidOperationException>(() => dish.SetRecipeSnapshot(Snapshot("replacement")));
        Assert.Equal("RECIPE_SOURCE_CHANGED", error.Message);
        Assert.Equal(existing, dish.RecipeSnapshot);
        Assert.Equal(Notes, dish.Notes);
        Assert.Equal(url, dish.Url?.OriginalString);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \r\n")]
    [InlineData(null)]
    public void Empty_content_is_rejected(string content) =>
        Assert.Throws<ArgumentException>(() => Snapshot(content));

    [Fact]
    public void Oversized_content_is_rejected() =>
        Assert.Throws<ArgumentException>(() => Snapshot(new string('x', RecipeSnapshotValueObject.MaximumContentLength + 1)));

    [Theory]
    [InlineData("ftp://example.com/recipe")]
    [InlineData("/relative")]
    [InlineData("https://user:secret@example.com/recipe")]
    [InlineData(null)]
    public void Invalid_provenance_is_rejected(string url) =>
        Assert.Throws<ArgumentException>(() => new RecipeSnapshotValueObject("recipe", url, CapturedAt, new string('a', 64)));

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData(null)]
    public void Invalid_hash_is_rejected(string hash) =>
        Assert.Throws<ArgumentException>(() => new RecipeSnapshotValueObject("recipe", SourceUrl, CapturedAt, hash));

    [Fact]
    public void Missing_capture_time_is_rejected() =>
        Assert.Throws<ArgumentException>(() => new RecipeSnapshotValueObject("recipe", SourceUrl, default, new string('a', 64)));

    [Fact]
    public void Legacy_document_without_snapshot_preserves_notes_and_url_when_projected()
    {
        var legacy = JsonConvert.SerializeObject(new
        {
            id = Guid.NewGuid(), familyId = Guid.NewGuid(), name = "Soup", url = SourceUrl,
            notes = Notes, tags = Array.Empty<object>(), ratings = Array.Empty<object>(), deleted = false
        });
        var dish = JsonConvert.DeserializeObject<Dish>(legacy);
        Assert.NotNull(dish);
        var details = DishDetails.CreateNew(dish, Array.Empty<Dinner>());
        Assert.Null(dish.RecipeSnapshot);
        Assert.Null(details.RecipeSnapshot);
        Assert.Equal(Notes, details.Notes);
        Assert.Equal(SourceUrl, details.Url);
        dish.SetName("Updated soup");
        Assert.Equal(Notes, dish.Notes);
    }

    [Fact]
    public void Projection_preserves_original_source_url_for_snapshot_comparison()
    {
        const string originalUrl = "https://EXAMPLE.com:443/recipe?q=%41";
        var dish = DishWithNotes();
        dish.SetUrl(originalUrl);
        dish.SetRecipeSnapshot(new("recipe", originalUrl, CapturedAt, new string('a', 64)));

        var details = DishDetails.CreateNew(dish, Array.Empty<Dinner>());

        Assert.Equal(originalUrl, details.Url);
        Assert.Equal(details.RecipeSnapshot.SourceUrl, details.Url);
    }

    [Fact]
    public void Snapshot_document_round_trip_preserves_snapshot_and_notes()
    {
        var dish = DishWithNotes();
        dish.SetRecipeSnapshot(Snapshot());
        var restored = JsonConvert.DeserializeObject<Dish>(JsonConvert.SerializeObject(dish));
        Assert.NotNull(restored);
        var details = DishDetails.CreateNew(restored, Array.Empty<Dinner>());
        Assert.Equal(dish.RecipeSnapshot, details.RecipeSnapshot);
        Assert.Equal(Notes, details.Notes);
        Assert.Equal(SourceUrl, details.Url);
    }
}
