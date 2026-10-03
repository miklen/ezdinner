using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.DomainServices.DishRecommendations;
using EzDinner.Infrastructure.DishRecommendations;
using EzDinner.Query.Core.DishRecommendationQueries;
using Moq;
using NodaTime;
using System.Text.Json;
using Xunit;

namespace EzDinner.UnitTests.DishRecommendationTests;

public class RecommendationProviderTests
{
    [Theory]
    [InlineData("Works with potatoes", "Possible pairing", "en")]
    [InlineData("No preparation work", "Family uses frozen vegetables", "en")]
    [InlineData("Passer til kartofler", "Muligt tilbehør", "da")]
    public async Task ControlledResponsesPreserveLocalizedEvidenceAndCumulativeIntent(string turn, string reason, string locale)
    {
        var dishId = Guid.NewGuid();
        var completion = new Mock<IDishRecommendationCompletionClient>();
        string capturedPrompt = null;
        string capturedEvidence = null;
        var expected = new RecommendationProviderResult(RecommendationOutcome.Matches,
            [new(dishId, 1, [new(reason, RecommendationEvidenceKind.Inference, [$"{dishId}:notes"])], [])], turn, null);
        completion.Setup(client => client.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((prompt, evidence, _) => { capturedPrompt = prompt; capturedEvidence = evidence; })
            .ReturnsAsync(JsonSerializer.Serialize(expected, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            }));
        var monday = new LocalDate(2026, 10, 5);
        var candidate = new DishRecommendationCandidateValueObject(dishId, "Roast", null, [DishRole.Main], false, 0, new([], monday), []);
        var context = new DishRecommendationContext([new(dishId, "Roast", [DishRole.Main],
            [new($"{dishId}:notes", "Family uses frozen vegetables"), new($"{dishId}:recipe", "Peel and chop vegetables")], candidate)], []);
        var request = new DishRecommendationRequest(monday, null, RecommendationMode.Request, locale,
            [turn, "No fish"], ["No fish"], new HashSet<Guid>(), DishRole.Main);
        var result = await new AnthropicDishRecommendationClient(completion.Object).RecommendAsync(request, context, default);
        Assert.Equal(reason, Assert.Single(Assert.Single(result.Matches).Explanations).Text);
        Assert.Contains("pairing request", capturedPrompt);
        Assert.Contains("Quick metadata alone cannot prove no preparation", capturedPrompt);
        Assert.Contains("Do not introduce variety", capturedPrompt);
        using var payload = JsonDocument.Parse(capturedEvidence);
        Assert.Equal(locale, payload.RootElement.GetProperty("locale").GetString());
        Assert.Equal("No fish", payload.RootElement.GetProperty("activeConstraints")[0].GetString());
        Assert.Equal("Family uses frozen vegetables", payload.RootElement.GetProperty("dishes")[0].GetProperty("sources")[0].GetProperty("text").GetString());
    }

    [Theory]
    [InlineData("```json\n", "\n```")]
    [InlineData("```\n", "\n```")]
    [InlineData(" \r\n```json\r\n", "\r\n``` \r\n")]
    public async Task SingleJsonCodeFencePreservesStrictResultContract(string opening, string closing)
    {
        var completion = new Mock<IDishRecommendationCompletionClient>();
        completion.Setup(client => client.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(opening + "{\"outcome\":\"NoMatch\",\"matches\":[],\"contextSummary\":\"Potatoes\",\"message\":null}" + closing);
        var request = new DishRecommendationRequest(new(2026, 10, 5), null, RecommendationMode.Request, "en", ["Potatoes"], [], new HashSet<Guid>());
        var result = await new AnthropicDishRecommendationClient(completion.Object).RecommendAsync(request, new([], []), default);
        Assert.Equal(RecommendationOutcome.NoMatch, result.Outcome);
        Assert.Empty(result.Matches);
        Assert.Equal("Potatoes", result.ContextSummary);
    }

    [Theory]
    [InlineData("{}")] 
    [InlineData("not json")]
    [InlineData("```json\n{}\n```")]
    [InlineData("Here is JSON:\n```json\n{}\n```")]
    [InlineData("```json\n{}\n```\nExtra text")]
    [InlineData("{\"outcome\":12,\"matches\":[],\"contextSummary\":\"\",\"message\":null}")]
    public async Task MalformedOutputRemainsProviderFailure(string response)
    {
        var completion = new Mock<IDishRecommendationCompletionClient>();
        completion.Setup(client => client.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);
        var request = new DishRecommendationRequest(new(2026, 10, 5), null, RecommendationMode.Request, "en", ["Potatoes"], [], new HashSet<Guid>());
        await Assert.ThrowsAsync<DishRecommendationProviderException>(() => new AnthropicDishRecommendationClient(completion.Object).RecommendAsync(request, new([], []), default));
    }
}
