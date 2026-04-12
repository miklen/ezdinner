using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using EzDinner.Query.Core.SuggestionQueries;
using Microsoft.Extensions.Logging;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EzDinner.Infrastructure
{
    public class AnthropicWeekPlanClient : ILlmWeekPlanClient
    {
        private const string Model = "claude-haiku-4-5-20251001";
        private const int MaxTokens = 1024;

        private readonly AnthropicClient _client;
        private readonly ILogger<AnthropicWeekPlanClient> _logger;

        public AnthropicWeekPlanClient(AnthropicClient client, ILogger<AnthropicWeekPlanClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<IReadOnlyList<LlmDayPlanResult>> PlanWeekAsync(
            string dishCatalogMarkdown,
            LocalDate weekStart,
            IReadOnlyList<LocalDate> unplannedDates,
            string? userContext,
            CancellationToken ct = default)
        {
            var weekEnd = weekStart.PlusDays(6);
            var userPrompt = BuildPrompt(dishCatalogMarkdown, weekStart, weekEnd, unplannedDates, userContext);

            var parameters = new MessageParameters
            {
                Model = Model,
                MaxTokens = MaxTokens,
                Messages = new List<Message>
                {
                    new Message(RoleType.User, userPrompt)
                }
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters, ct);
            var responseText = response.FirstMessage?.Text;

            if (string.IsNullOrWhiteSpace(responseText))
            {
                _logger.LogWarning("Claude returned an empty response");
                return Array.Empty<LlmDayPlanResult>();
            }

            return ParseResponse(responseText);
        }

        private static string BuildPrompt(
            string catalog,
            LocalDate weekStart,
            LocalDate weekEnd,
            IReadOnlyList<LocalDate> unplannedDates,
            string? context)
        {
            var datesLine = string.Join(", ", unplannedDates.Select(d => d.ToString()));

            var contextLine = string.IsNullOrWhiteSpace(context)
                ? ""
                : $"\nContext from the planner: {context}";

            var exampleRow = @"  {""date"":""2026-04-14"",""dishId"":""<guid>"",""dishName"":""<name>"",""reason"":""<brief reason>""}";
            return "You are a meal planning assistant. Return ONLY valid JSON arrays - no explanation, no markdown, no code fences.\n\n" +
                   $"Plan a week of family dinners for the week {weekStart} to {weekEnd}.\n" +
                   $"Days to plan: {datesLine}{contextLine}\n\n" +
                   $"Dish catalog (use only dishIds from this list):\n{catalog}\n\n" +
                   "Rules:\n" +
                   "- Suggest one dish per day for the listed days only\n" +
                   "- Prefer dishes with higher weeks_since_last (longer since last served)\n" +
                   "- Prioritize dishes with wish_votes > 0\n" +
                   "- Match effort level to day of week if context hints at busy days (Quick on busy days, Elaborate on weekends)\n" +
                   "- Do NOT repeat the same dish twice in the week\n" +
                   "- Return ONLY a JSON array - no other text\n\n" +
                   "Return format:\n" +
                   "[\n" +
                   exampleRow + ",\n" +
                   "  ...\n" +
                   "]";
        }

        private IReadOnlyList<LlmDayPlanResult> ParseResponse(string responseText)
        {
            try
            {
                var json = ExtractJson(responseText);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return Array.Empty<LlmDayPlanResult>();

                var results = new List<LlmDayPlanResult>();
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (!item.TryGetProperty("date", out var dateEl) || dateEl.ValueKind != JsonValueKind.String)
                        continue;

                    if (!item.TryGetProperty("dishId", out var dishIdEl) || dishIdEl.ValueKind != JsonValueKind.String)
                        continue;

                    if (!Guid.TryParse(dishIdEl.GetString(), out var dishId))
                        continue;

                    var dishName = item.TryGetProperty("dishName", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                        ? nameEl.GetString() ?? string.Empty
                        : string.Empty;

                    var reason = item.TryGetProperty("reason", out var reasonEl) && reasonEl.ValueKind == JsonValueKind.String
                        ? reasonEl.GetString() ?? string.Empty
                        : string.Empty;

                    results.Add(new LlmDayPlanResult
                    {
                        Date = dateEl.GetString()!,
                        DishId = dishId,
                        DishName = dishName,
                        Reason = reason,
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse AI week plan response. Response: {Response}", responseText);
                return Array.Empty<LlmDayPlanResult>();
            }
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
    }
}
