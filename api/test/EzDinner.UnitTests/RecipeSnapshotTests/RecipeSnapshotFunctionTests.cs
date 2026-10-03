using System.Security.Claims;
using System.Text;
using System.Text.Json;
using EzDinner.Application.Commands.Dishes;
using EzDinner.Application.Commands.RecipeSnapshots;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Functions;
using EzDinner.Infrastructure.RecipeSnapshots;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EzDinner.UnitTests.RecipeSnapshotTests;

public class RecipeSnapshotFunctionTests
{
    private readonly Dish _dish = Dish.CreateNew(Guid.NewGuid(), "Soup");
    private readonly Mock<IDishRepository> _repository = new();
    private readonly Mock<IAuthzService> _authorization = new();
    private readonly Mock<IRecipeSourceReader> _reader = new();
    private readonly Guid _user = Guid.NewGuid();

    public RecipeSnapshotFunctionTests()
    {
        _dish.SetUrl("https://example.com/soup");
        _dish.SetNotes("  My notes\r\n");
        _repository.Setup(repository => repository.GetDishAsync(_dish.Id)).ReturnsAsync(_dish);
        _authorization.Setup(authorization => authorization.Authorize(_user.ToString(), _dish.FamilyId, Resources.Dish, Actions.Update)).Returns(true);
        _reader.Setup(reader => reader.ReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new RecipeSource("""<script type="application/ld+json">{"@type":"Recipe","recipeIngredient":["Salt"],"recipeInstructions":["Boil"]}</script>"""));
    }

    private DishRecipeSnapshots Functions() => new(_authorization.Object,
        new(_repository.Object, _reader.Object, new StructuredRecipeExtractor(), Mock.Of<IRecipeExtractionProvider>()),
        new(_repository.Object, new(_repository.Object, Mock.Of<IDishEnrichmentProvider>()), NullLogger<ConfirmRecipeSnapshotCommand>.Instance),
        new(_repository.Object));

    private HttpRequest Request(bool authenticated = true, string body = "")
    {
        var context = new DefaultHttpContext();
        if (authenticated) context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", _user.ToString()),
            new Claim(ClaimTypes.NameIdentifier, _user.ToString())
        }, "test"));
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        return context.Request;
    }

    private Task<IActionResult> Execute(string operation, HttpRequest request, string family = null, string dish = null) => operation switch
    {
        "preview" => Functions().PreviewAsync(request, dish ?? _dish.Id.ToString(), family ?? _dish.FamilyId.ToString()),
        "confirm" => Functions().ConfirmAsync(request, dish ?? _dish.Id.ToString(), family ?? _dish.FamilyId.ToString()),
        _ => Functions().RemoveAsync(request, dish ?? _dish.Id.ToString(), family ?? _dish.FamilyId.ToString())
    };

    [Theory]
    [InlineData("preview")]
    [InlineData("confirm")]
    [InlineData("remove")]
    public async Task Unauthenticated_request_is_rejected_before_loading(string operation)
    {
        Assert.IsType<UnauthorizedResult>(await Execute(operation, Request(false)));
        _repository.Verify(repository => repository.GetDishAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Theory]
    [InlineData("preview")]
    [InlineData("confirm")]
    [InlineData("remove")]
    public async Task Missing_update_permission_is_rejected_without_saving(string operation)
    {
        _authorization.Setup(authorization => authorization.Authorize(It.IsAny<string>(), It.IsAny<Guid>(), Resources.Dish, Actions.Update)).Returns(false);
        Assert.IsType<UnauthorizedResult>(await Execute(operation, Request()));
        _repository.Verify(repository => repository.SaveAsync(It.IsAny<Dish>()), Times.Never);
    }

    [Theory]
    [InlineData("preview")]
    [InlineData("confirm")]
    [InlineData("remove")]
    public async Task Invalid_route_ids_return_validation_error(string operation)
    {
        var result = Assert.IsType<BadRequestObjectResult>(await Execute(operation, Request(), dish: "invalid"));
        Assert.Equal("RECIPE_INVALID_REQUEST", result.Value);
    }

    [Fact]
    public async Task Preview_returns_candidate_without_saving()
    {
        var result = Assert.IsType<OkObjectResult>(await Execute("preview", Request()));
        var preview = Assert.IsType<RecipePreview>(result.Value);
        Assert.Equal("- Salt\n\n1. Boil", preview.Candidate.Content);
        _repository.Verify(repository => repository.SaveAsync(It.IsAny<Dish>()), Times.Never);
    }

    [Fact]
    public async Task Confirmation_returns_no_content_and_preserves_notes()
    {
        var candidate = new RecipeCandidate("- Salt\n\n1. Boil", _dish.Url.OriginalString, DateTimeOffset.UtcNow, new string('a', 64));
        Assert.IsType<NoContentResult>(await Execute("confirm", Request(body: JsonSerializer.Serialize(candidate))));
        Assert.Equal(candidate.Content, _dish.RecipeSnapshot.Content);
        Assert.Equal("  My notes\r\n", _dish.Notes);
        _repository.Verify(repository => repository.SaveAsync(_dish), Times.Once);
    }

    [Fact]
    public async Task Removal_returns_no_content_and_preserves_notes()
    {
        _dish.SetRecipeSnapshot(new("recipe", _dish.Url.OriginalString, DateTimeOffset.UtcNow, new string('a', 64)));
        Assert.IsType<NoContentResult>(await Execute("remove", Request()));
        Assert.Null(_dish.RecipeSnapshot);
        Assert.Equal("  My notes\r\n", _dish.Notes);
    }

    [Fact]
    public async Task Stale_candidate_returns_conflict_without_saving()
    {
        var candidate = new RecipeCandidate("recipe", "https://example.com/old", DateTimeOffset.UtcNow, new string('a', 64));
        var result = Assert.IsType<ConflictObjectResult>(await Execute("confirm", Request(body: JsonSerializer.Serialize(candidate))));
        Assert.Equal("RECIPE_SOURCE_CHANGED", result.Value);
        _repository.Verify(repository => repository.SaveAsync(It.IsAny<Dish>()), Times.Never);
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task Invalid_candidate_returns_bad_request_without_saving(string body)
    {
        Assert.IsType<BadRequestObjectResult>(await Execute("confirm", Request(body: body)));
        _repository.Verify(repository => repository.SaveAsync(It.IsAny<Dish>()), Times.Never);
    }

    [Fact]
    public async Task Dish_from_different_family_is_rejected_even_when_route_family_is_authorized()
    {
        var otherFamily = Guid.NewGuid();
        _authorization.Setup(authorization => authorization.Authorize(_user.ToString(), otherFamily, Resources.Dish, Actions.Update)).Returns(true);
        Assert.IsType<UnauthorizedResult>(await Execute("preview", Request(), family: otherFamily.ToString()));
        _repository.Verify(repository => repository.SaveAsync(It.IsAny<Dish>()), Times.Never);
    }
}
