using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.WishlistAggregate;
using EzDinner.Query.Core.AiPlanningQueries;
using EzDinner.Query.Core.DishQueries;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EzDinner.Infrastructure
{
    public class AnthropicWeekPlannerService : IAiWeekPlannerService
    {
        private const string Model = "claude-haiku-4-5-20251001";
        private const int MaxTokens = 1024;

        private readonly AnthropicClient _client;
        private readonly IDishRepository _dishRepository;
        private readonly IDishQueryService _dishQueryService;
        private readonly IWishlistRepository _wishlistRepository;
        private readonly IDinnerRepository _dinnerRepository;
        private readonly ILogger<AnthropicWeekPlannerService> _logger;

        public AnthropicWeekPlannerService(
            AnthropicClient client,
            IDishRepository dishRepository,
            IDishQueryService dishQueryService,
            IWishlistRepository wishlistRepository,
            IDinnerRepository dinnerRepository,
            ILogger<AnthropicWeekPlannerService> logger)
        {
            _client = client;
            _dishRepository = dishRepository;
            _dishQueryService = dishQueryService;
            _wishlistRepository = wishlistRepository;
            _dinnerRepository = dinnerRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<AiWeekPlanSuggestionResult>> PlanWeekAsync(
            Guid familyId,
            string weekStart,
            string? context,
            IReadOnlyList<string>? excludedDishIds = null,
            CancellationToken ct = default)
        {
            var weekStartDate = LocalDatePattern.Iso.Parse(weekStart).GetValueOrThrow();
            var weekEndDate = weekStartDate.PlusDays(6);
            var today = LocalDate.FromDateTime(DateTime.UtcNow);
            var statsFrom = today.PlusDays(-365);

            // Load all active dishes
            var allDishes = (await _dishRepository.GetDishesAsync(familyId, includeArchived: false)).ToList();

            if (!allDishes.Any())
                return Array.Empty<AiWeekPlanSuggestionResult>();

            // Filter out dishes the user explicitly skipped in a previous round
            var excludedSet = BuildExcludedSet(excludedDishIds);
            var dishes = excludedSet.Count > 0
                ? allDishes.Where(d => !excludedSet.Contains(d.Id)).ToList()
                : allDishes;

            if (!dishes.Any())
                return Array.Empty<AiWeekPlanSuggestionResult>();

            // Load usage stats, wishlist, and current week dinners in parallel
            var statsTask = _dishQueryService.GetDishUsageStatsAsync(familyId, statsFrom, today);
            var wishesTask = _wishlistRepository.GetActiveAsync(familyId);
            var plannedDaysTask = LoadPlannedDaysAsync(familyId, weekStartDate, weekEndDate, allDishes);

            await Task.WhenAll(statsTask, wishesTask, plannedDaysTask);

            var stats = statsTask.Result;
            var wishMap = wishesTask.Result.ToDictionary(w => w.DishId, w => w.Votes.Count);
            var plannedDays = plannedDaysTask.Result;

            // All days already planned — nothing to suggest
            var unplannedCount = 7 - plannedDays.Count;
            if (unplannedCount == 0)
                return Array.Empty<AiWeekPlanSuggestionResult>();

            var catalog = BuildCatalog(dishes, stats, wishMap, weekStartDate);
            var userPrompt = BuildPrompt(catalog, weekStartDate, weekEndDate, context, plannedDays);

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
                _logger.LogWarning("Claude returned an empty response for family {FamilyId}", familyId);
                return Array.Empty<AiWeekPlanSuggestionResult>();
            }

            var suggestions = ParseResponse(responseText, dishes);

            // Guard: drop any dates the AI suggested despite them being already planned
            if (plannedDays.Count == 0)
                return suggestions;

            var plannedDates = new HashSet<string>(plannedDays.Keys.Select(d => LocalDatePattern.Iso.Format(d)));
            return suggestions.Where(s => !plannedDates.Contains(s.Date)).ToList();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static HashSet<Guid> BuildExcludedSet(IReadOnlyList<string>? excludedDishIds)
        {
            if (excludedDishIds == null || excludedDishIds.Count == 0)
                return new HashSet<Guid>();

            var result = new HashSet<Guid>();
            foreach (var id in excludedDishIds)
            {
                if (Guid.TryParse(id, out var guid))
                    result.Add(guid);
            }
            return result;
        }

        private async Task<Dictionary<LocalDate, List<string>>> LoadPlannedDaysAsync(
            Guid familyId,
            LocalDate weekStart,
            LocalDate weekEnd,
            IReadOnlyList<Dish> allDishes)
        {
            var nameMap = allDishes.ToDictionary(d => d.Id, d => d.Name);
            var result = new Dictionary<LocalDate, List<string>>();

            await foreach (var dinner in _dinnerRepository.GetAsync(familyId, weekStart, weekEnd))
            {
                if (!dinner.Menu.Any()) continue;
                var names = dinner.Menu
                    .Select(m => nameMap.TryGetValue(m.DishId, out var n) ? n : null)
                    .Where(n => n != null)
                    .ToList();
                if (names.Any())
                    result[dinner.Date] = names!;
            }

            return result;
        }

        private static string BuildCatalog(
            IReadOnlyList<Dish> dishes,
            Dictionary<Guid, DishStats> stats,
            Dictionary<Guid, int> wishMap,
            LocalDate weekStart)
        {
            var today = LocalDate.FromDateTime(DateTime.UtcNow);
            var sb = new StringBuilder();
            sb.AppendLine("id | name | effort | weeks_since_last | wish_votes");
            sb.AppendLine("---|------|--------|-----------------|----------");

            foreach (var dish in dishes)
            {
                var effort = dish.Metadata.EffortLevel?.ToString() ?? "unknown";
                var weeksSince = "never";
                if (stats.TryGetValue(dish.Id, out var s) && s.TimesUsed > 0)
                {
                    var daysDiff = Period.Between(s.LastUsed, today, PeriodUnits.Days).Days;
                    weeksSince = $"{Math.Round(daysDiff / 7.0, 1)}";
                }
                var votes = wishMap.TryGetValue(dish.Id, out var v) ? v : 0;
                sb.AppendLine($"{dish.Id} | {dish.Name} | {effort} | {weeksSince} | {votes}");
            }

            return sb.ToString();
        }

        private static string BuildPrompt(
            string catalog,
            LocalDate weekStart,
            LocalDate weekEnd,
            string? context,
            Dictionary<LocalDate, List<string>> plannedDays)
        {
            var allDays = Enumerable.Range(0, 7).Select(i => weekStart.PlusDays(i)).ToList();
            var unplannedDays = allDays.Where(d => !plannedDays.ContainsKey(d)).ToList();
            var datesLine = string.Join(", ", unplannedDays.Select(d => d.ToString()));

            var contextLine = string.IsNullOrWhiteSpace(context)
                ? ""
                : $"\nContext from the planner: {context}";

            var plannedSection = plannedDays.Count > 0
                ? "\nAlready planned — do NOT suggest these days:\n" +
                  string.Join("\n", plannedDays
                      .OrderBy(kv => kv.Key)
                      .Select(kv => $"- {kv.Key}: {string.Join(", ", kv.Value)}"))
                : "";

            var exampleRow = @"  {""date"":""2026-04-14"",""dishId"":""<guid>"",""dishName"":""<name>"",""reason"":""<brief reason>""}";
            return "You are a meal planning assistant. Return ONLY valid JSON arrays - no explanation, no markdown, no code fences.\n\n" +
                   $"Plan a week of family dinners for the week {weekStart} to {weekEnd}.\n" +
                   $"Days to plan: {datesLine}{contextLine}{plannedSection}\n\n" +
                   $"Dish catalog (use only dishIds from this list):\n{catalog}\n\n" +
                   "Rules:\n" +
                   "- Suggest one dish per day for the listed unplanned days only\n" +
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

        private IReadOnlyList<AiWeekPlanSuggestionResult> ParseResponse(
            string responseText,
            IReadOnlyList<Dish> dishes)
        {
            var validDishIds = new HashSet<Guid>(dishes.Select(d => d.Id));
            var dishNameMap = dishes.ToDictionary(d => d.Id, d => d.Name);

            try
            {
                var json = ExtractJson(responseText);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return Array.Empty<AiWeekPlanSuggestionResult>();

                var results = new List<AiWeekPlanSuggestionResult>();
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (!item.TryGetProperty("date", out var dateEl) || dateEl.ValueKind != JsonValueKind.String)
                        continue;

                    if (!item.TryGetProperty("dishId", out var dishIdEl) || dishIdEl.ValueKind != JsonValueKind.String)
                        continue;

                    if (!Guid.TryParse(dishIdEl.GetString(), out var dishId))
                        continue;

                    if (!validDishIds.Contains(dishId))
                        continue;

                    var reason = item.TryGetProperty("reason", out var reasonEl) && reasonEl.ValueKind == JsonValueKind.String
                        ? reasonEl.GetString() ?? string.Empty
                        : string.Empty;

                    results.Add(new AiWeekPlanSuggestionResult
                    {
                        Date = dateEl.GetString()!,
                        DishId = dishId,
                        DishName = dishNameMap.TryGetValue(dishId, out var name) ? name : string.Empty,
                        Reason = reason,
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse AI week plan response. Response: {Response}", responseText);
                return Array.Empty<AiWeekPlanSuggestionResult>();
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
