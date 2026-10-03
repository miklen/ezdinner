using EzDinner.Application.Commands.Dishes;
using EzDinner.Application.Commands.RecipeSnapshots;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Infrastructure.RecipeSnapshots;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EzDinner.UnitTests.RecipeSnapshotTests;

public class RecipeSnapshotCommandTests
{
    private readonly Dish _dish = Dish.CreateNew(Guid.NewGuid(), "Soup");
    private readonly Mock<IDishRepository> _repository = new();
    private readonly Mock<IRecipeSourceReader> _reader = new();
    private readonly Mock<IRecipeExtractionProvider> _provider = new();
    private readonly Mock<IDishEnrichmentProvider> _enrichment = new();
    private const string Notes = "  Æg 🥚\r\n\nLess salt\t";
    private const string Source = "https://example.com/soup";
    private const string StructuredHtml = """<script type="application/ld+json">{"@type":"Recipe","recipeIngredient":["Salt"],"recipeInstructions":["Boil"]}</script>""";

    public RecipeSnapshotCommandTests()
    {
        _dish.SetUrl(Source);
        _dish.SetNotes(Notes);
        _repository.Setup(repository => repository.GetDishAsync(_dish.Id)).ReturnsAsync(_dish);
        _reader.Setup(reader => reader.ReadAsync(Source, It.IsAny<CancellationToken>())).ReturnsAsync(new RecipeSource(StructuredHtml));
        _enrichment.Setup(provider => provider.EnrichAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync(new DishEnrichmentResult(new[] { DishRole.Main }, EffortLevel.Quick, SeasonAffinity.AllYear, "Danish"));
    }

    private PreviewRecipeSnapshotCommand Preview() => new(_repository.Object, _reader.Object, new StructuredRecipeExtractor(), _provider.Object);
    private ConfirmRecipeSnapshotCommand Confirm() => new(_repository.Object, new(_repository.Object, _enrichment.Object), NullLogger<ConfirmRecipeSnapshotCommand>.Instance);
    private RecipeCandidate Candidate() => new("- Salt\n\n1. Boil", Source, DateTimeOffset.UtcNow, new string('a', 64));

    [Fact]
    public async Task Structured_preview_skips_provider_and_never_saves()
    {
        var preview = await Preview().ExecuteAsync(_dish.FamilyId, _dish.Id);
        Assert.Equal("- Salt\n\n1. Boil", preview.Candidate.Content);
        Assert.Equal(Source, preview.Candidate.SourceUrl);
        Assert.True(preview.SourceChanged);
        Assert.Null(_dish.RecipeSnapshot);
        Assert.Equal(Notes, _dish.Notes);
        _repository.Verify(repository => repository.SaveAsync(It.IsAny<Dish>()), Times.Never);
        _provider.Verify(provider => provider.ExtractAsync(It.IsAny<RecipeExtractionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Preview_without_url_returns_actionable_failure_without_saving()
    {
        _dish.SetUrl(null);
        var error = await Assert.ThrowsAsync<RecipeImportException>(() => Preview().ExecuteAsync(_dish.FamilyId, _dish.Id));
        Assert.Equal("RECIPE_NO_URL", error.Code);
        _repository.Verify(repository => repository.SaveAsync(It.IsAny<Dish>()), Times.Never);
    }

    [Theory]
    [InlineData("RECIPE_FETCH_FAILED")]
    [InlineData("RECIPE_UNSAFE_URL")]
    public async Task Fetch_failure_preserves_dish(string code)
    {
        _reader.Setup(reader => reader.ReadAsync(Source, It.IsAny<CancellationToken>())).ThrowsAsync(new RecipeImportException(code));
        var error = await Assert.ThrowsAsync<RecipeImportException>(() => Preview().ExecuteAsync(_dish.FamilyId, _dish.Id));
        Assert.Equal(code, error.Code);
        Assert.Equal(Notes, _dish.Notes);
        _repository.Verify(repository => repository.SaveAsync(It.IsAny<Dish>()), Times.Never);
    }

    [Fact]
    public async Task Incomplete_structured_recipe_uses_provider_without_saving()
    {
        _reader.Setup(reader => reader.ReadAsync(Source, It.IsAny<CancellationToken>())).ReturnsAsync(new RecipeSource("<p>Salt Boil</p>"));
        _provider.Setup(provider => provider.ExtractAsync(It.Is<RecipeExtractionRequest>(request => request.SourceText == "Salt Boil"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecipeExtractionResult(null, new[] { "Salt" }, new[] { "Boil" }, new[] { "RECIPE_LLM_EXTRACTED" }));
        var preview = await Preview().ExecuteAsync(_dish.FamilyId, _dish.Id);
        Assert.Equal("- Salt\n\n1. Boil", preview.Candidate.Content);
        Assert.Equal(new[] { "RECIPE_LLM_EXTRACTED" }, preview.Warnings);
        _repository.Verify(repository => repository.SaveAsync(It.IsAny<Dish>()), Times.Never);
    }

    [Fact]
    public async Task Extraction_failure_preserves_dish()
    {
        _reader.Setup(reader => reader.ReadAsync(Source, It.IsAny<CancellationToken>())).ReturnsAsync(new RecipeSource("No recipe"));
        _provider.Setup(provider => provider.ExtractAsync(It.IsAny<RecipeExtractionRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(new RecipeImportException("RECIPE_EXTRACTION_FAILED"));
        var error = await Assert.ThrowsAsync<RecipeImportException>(() => Preview().ExecuteAsync(_dish.FamilyId, _dish.Id));
        Assert.Equal("RECIPE_EXTRACTION_FAILED", error.Code);
        Assert.Equal(Notes, _dish.Notes);
        _repository.Verify(repository => repository.SaveAsync(It.IsAny<Dish>()), Times.Never);
    }

    [Fact]
    public async Task Matching_source_hash_is_reported_unchanged_without_saving()
    {
        var candidate = (await Preview().ExecuteAsync(_dish.FamilyId, _dish.Id)).Candidate;
        _dish.SetRecipeSnapshot(new(candidate.Content, candidate.SourceUrl, candidate.CapturedAt, candidate.SourceHash));
        var preview = await Preview().ExecuteAsync(_dish.FamilyId, _dish.Id);
        Assert.False(preview.SourceChanged);
        _repository.Verify(repository => repository.SaveAsync(It.IsAny<Dish>()), Times.Never);
    }

    [Fact]
    public async Task Confirmation_saves_snapshot_before_enrichment()
    {
        _repository.Setup(repository => repository.SaveAsync(_dish)).Returns(Task.CompletedTask).Callback(() => Assert.NotNull(_dish.RecipeSnapshot));
        await Confirm().ExecuteAsync(_dish.FamilyId, _dish.Id, Candidate());
        Assert.Equal(Candidate().Content, _dish.RecipeSnapshot.Content);
        Assert.Equal(Notes, _dish.Notes);
        Assert.Equal(Source, _dish.Url.OriginalString);
        Assert.Equal("Danish", _dish.Metadata.Cuisine);
        _enrichment.Verify(provider => provider.EnrichAsync("Soup", Notes, It.IsAny<CancellationToken>(), Candidate().Content), Times.Once);
    }

    [Fact]
    public async Task Stale_confirmation_changes_nothing()
    {
        var existing = Candidate();
        _dish.SetRecipeSnapshot(new(existing.Content, existing.SourceUrl, existing.CapturedAt, existing.SourceHash));
        _dish.SetUrl("https://example.com/other");
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Confirm().ExecuteAsync(_dish.FamilyId, _dish.Id, Candidate()));
        Assert.Equal("RECIPE_SOURCE_CHANGED", error.Message);
        Assert.Equal(existing.Content, _dish.RecipeSnapshot.Content);
        Assert.Equal(Notes, _dish.Notes);
        _repository.Verify(repository => repository.SaveAsync(It.IsAny<Dish>()), Times.Never);
    }

    [Fact]
    public async Task Invalid_candidate_changes_nothing()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Confirm().ExecuteAsync(_dish.FamilyId, _dish.Id, Candidate() with { Content = "" }));
        Assert.Null(_dish.RecipeSnapshot);
        _repository.Verify(repository => repository.SaveAsync(It.IsAny<Dish>()), Times.Never);
    }

    [Fact]
    public async Task Enrichment_failure_does_not_undo_snapshot_save()
    {
        _enrichment.Setup(provider => provider.EnrichAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("provider unavailable"));
        await Confirm().ExecuteAsync(_dish.FamilyId, _dish.Id, Candidate());
        Assert.Equal(Candidate().Content, _dish.RecipeSnapshot.Content);
        Assert.Equal(Notes, _dish.Notes);
        _repository.Verify(repository => repository.SaveAsync(_dish), Times.Once);
    }

    [Fact]
    public async Task Removal_preserves_source_and_exact_notes()
    {
        var candidate = Candidate();
        _dish.SetRecipeSnapshot(new(candidate.Content, candidate.SourceUrl, candidate.CapturedAt, candidate.SourceHash));
        await new RemoveRecipeSnapshotCommand(_repository.Object).ExecuteAsync(_dish.FamilyId, _dish.Id);
        Assert.Null(_dish.RecipeSnapshot);
        Assert.Equal(Notes, _dish.Notes);
        Assert.Equal(Source, _dish.Url.OriginalString);
        _repository.Verify(repository => repository.SaveAsync(_dish), Times.Once);
    }

    [Theory]
    [InlineData("preview")]
    [InlineData("confirm")]
    [InlineData("remove")]
    public async Task Cross_family_operations_are_rejected_without_saving(string operation)
    {
        var family = Guid.NewGuid();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            if (operation == "preview") await Preview().ExecuteAsync(family, _dish.Id);
            if (operation == "confirm") await Confirm().ExecuteAsync(family, _dish.Id, Candidate());
            if (operation == "remove") await new RemoveRecipeSnapshotCommand(_repository.Object).ExecuteAsync(family, _dish.Id);
        });
        _repository.Verify(repository => repository.SaveAsync(It.IsAny<Dish>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Enrichment_preserves_confirmed_fields_with_or_without_snapshot(bool snapshot)
    {
        _dish.UpdateMetadata(new(new[] { DishRole.Side }, true, EffortLevel.Elaborate, true, SeasonAffinity.Winter, true, "Italian", true));
        if (snapshot)
        {
            var candidate = Candidate();
            _dish.SetRecipeSnapshot(new(candidate.Content, candidate.SourceUrl, candidate.CapturedAt, candidate.SourceHash));
        }
        await new EnrichDishCommandHandler(_repository.Object, _enrichment.Object).Handle(new(_dish.FamilyId, _dish.Id));
        Assert.Equal(new[] { DishRole.Side }, _dish.Metadata.Roles);
        Assert.Equal(EffortLevel.Elaborate, _dish.Metadata.EffortLevel);
        Assert.Equal(SeasonAffinity.Winter, _dish.Metadata.SeasonAffinity);
        Assert.Equal("Italian", _dish.Metadata.Cuisine);
        _enrichment.Verify(provider => provider.EnrichAsync("Soup", Notes, It.IsAny<CancellationToken>(), snapshot ? Candidate().Content : null), Times.Once);
    }
}
