using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using EzDinner.Application.Commands.Dishes;
using EzDinner.Core.Aggregates.DishAggregate;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EzDinner.Infrastructure
{
    public class AnthropicEnrichmentProvider : IDishEnrichmentProvider
    {
        private const string Model = "claude-haiku-4-5-20251001";
        private const int MaxTokens = 512;

        private readonly AnthropicClient _client;
        private readonly ILogger<AnthropicEnrichmentProvider> _logger;

        public AnthropicEnrichmentProvider(AnthropicClient client, ILogger<AnthropicEnrichmentProvider> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<DishEnrichmentResult> EnrichAsync(string dishName, string? notes, CancellationToken ct)
        {
            var prompt = BuildPrompt(dishName, notes);
            var parameters = new MessageParameters
            {
                Model = Model,
                MaxTokens = MaxTokens,
                Messages = new List<Message>
                {
                    new Message(RoleType.User, prompt)
                }
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters, ct);
            var responseText = response.FirstMessage?.Text;

            if (string.IsNullOrWhiteSpace(responseText))
                throw new InvalidOperationException($"Claude returned an empty response for dish '{dishName}'.");

            return ParseResponse(responseText, dishName);
        }

        private static string BuildPrompt(string dishName, string? notes)
        {
            var notesLine = string.IsNullOrWhiteSpace(notes) ? "" : $"\nNotes: {notes}";
            return $@"You are analyzing a dish for a family dinner planning app. The app is used by Danish families, so dish names may be in Danish.

Dish name: {dishName}{notesLine}

Classify this dish and return ONLY a valid JSON object with these fields:
- ""roles"": array of applicable roles from [""Main"", ""Side"", ""Dessert"", ""Other""] — a dish can have multiple roles
- ""effortLevel"": one of ""Quick"", ""Medium"", ""Elaborate"", or null if unknown
- ""seasonAffinity"": one of ""Summer"", ""Winter"", ""Spring"", ""Autumn"", ""AllYear"", or null if unknown
- ""cuisine"": cuisine type as a short string (e.g. ""Danish"", ""Italian""), or null if unknown

Return ONLY the JSON object, no explanation.";
        }

        private static string ExtractJson(string responseText)
        {
            var text = responseText.Trim();
            if (!text.StartsWith("```"))
                return text;

            var firstNewline = text.IndexOf('\n');
            if (firstNewline >= 0)
                text = text[(firstNewline + 1)..];

            var lastFence = text.LastIndexOf("```");
            if (lastFence >= 0)
                text = text[..lastFence];

            return text.Trim();
        }

        private DishEnrichmentResult ParseResponse(string responseText, string dishName)
        {
            try
            {
                using var doc = JsonDocument.Parse(ExtractJson(responseText));
                var root = doc.RootElement;

                var roles = ParseRoles(root);
                var effortLevel = ParseEnum<EffortLevel>(root, "effortLevel");
                var seasonAffinity = ParseEnum<SeasonAffinity>(root, "seasonAffinity");
                var cuisine = root.TryGetProperty("cuisine", out var cuisineEl) && cuisineEl.ValueKind == JsonValueKind.String
                    ? cuisineEl.GetString()
                    : null;

                return new DishEnrichmentResult(roles, effortLevel, seasonAffinity, cuisine);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse enrichment response for dish '{DishName}'. Response: {Response}", dishName, responseText);
                throw new InvalidOperationException("ENRICHMENT_PARSE_FAILED", ex);
            }
        }

        private static IReadOnlyList<DishRole>? ParseRoles(JsonElement root)
        {
            if (!root.TryGetProperty("roles", out var rolesEl) || rolesEl.ValueKind != JsonValueKind.Array)
                return null;

            var roles = new List<DishRole>();
            foreach (var item in rolesEl.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String &&
                    Enum.TryParse<DishRole>(item.GetString(), ignoreCase: true, out var role))
                {
                    roles.Add(role);
                }
            }

            return roles.Count > 0 ? roles : null;
        }

        private static T? ParseEnum<T>(JsonElement root, string propertyName) where T : struct, Enum
        {
            if (!root.TryGetProperty(propertyName, out var el) || el.ValueKind != JsonValueKind.String)
                return null;

            return Enum.TryParse<T>(el.GetString(), ignoreCase: true, out var value) ? value : null;
        }
    }
}
