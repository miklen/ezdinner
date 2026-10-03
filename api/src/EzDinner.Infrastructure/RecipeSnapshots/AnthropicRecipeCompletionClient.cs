using Anthropic.SDK;
using Anthropic.SDK.Messaging;

namespace EzDinner.Infrastructure.RecipeSnapshots;

public interface IRecipeCompletionClient
{
    Task<string?> CompleteAsync(string systemPrompt, string sourcePrompt, CancellationToken cancellationToken);
}

public sealed class AnthropicRecipeCompletionClient(AnthropicClient client) : IRecipeCompletionClient
{
    public async Task<string?> CompleteAsync(string systemPrompt, string sourcePrompt, CancellationToken cancellationToken)
    {
        var response = await client.Messages.GetClaudeMessageAsync(new MessageParameters
        {
            Model = "claude-haiku-4-5-20251001",
            MaxTokens = 4096,
            SystemMessage = systemPrompt,
            Messages = [new Message(RoleType.User, sourcePrompt)]
        }, cancellationToken);
        return response.FirstMessage?.Text;
    }
}
