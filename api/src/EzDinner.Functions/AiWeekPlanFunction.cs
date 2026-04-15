using EzDinner.Query.Core.SuggestionQueries;
using EzDinner.Authorization.Core;
using EzDinner.Functions.Models.Command;
using EzDinner.Functions.Models.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using NodaTime;
using NodaTime.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EzDinner.Functions
{
    public class AiWeekPlanFunction
    {
        private readonly ILogger<AiWeekPlanFunction> _logger;
        private readonly IDinnerSuggestionService _suggestionService;
        private readonly IAuthzService _authz;

        public AiWeekPlanFunction(
            ILogger<AiWeekPlanFunction> logger,
            IDinnerSuggestionService suggestionService,
            IAuthzService authz)
        {
            _logger = logger;
            _suggestionService = suggestionService;
            _authz = authz;
        }

        [Function(nameof(AiWeekPlanFunction))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post",
                Route = "families/{familyId}/suggest/ai-week")] HttpRequest req,
            string familyId)
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, familyId, Resources.Dinner, Actions.Read))
                return new UnauthorizedResult();

            AiWeekPlanRequest? request;
            try
            {
                request = await System.Text.Json.JsonSerializer.DeserializeAsync<AiWeekPlanRequest>(
                    req.Body,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize AiWeekPlanRequest");
                return new BadRequestResult();
            }

            if (request?.WeekStart is null)
                return new BadRequestObjectResult("weekStart is required");

            var parsedFamilyId = Guid.Parse(familyId);
            var weekStart = LocalDatePattern.Iso.Parse(request.WeekStart).GetValueOrThrow();

            var excludedDishIds = ParseExcludedDishIds(request.ExcludedDishIds);

            const int MaxContextLength = 200;
            var userContext = string.IsNullOrWhiteSpace(request.Context)
                ? null
                : request.Context.Length > MaxContextLength
                    ? request.Context[..MaxContextLength]
                    : request.Context;

            _logger.LogInformation(
                "AiWeekPlan requested for family={FamilyId}, weekStart={WeekStart}, hasContext={HasContext}",
                familyId, request.WeekStart, userContext is not null);

            var suggestions = await _suggestionService.SuggestWeek(parsedFamilyId, weekStart, excludedDishIds, userContext: userContext);

            var result = suggestions
                .Where(s => s.Suggestion is not null)
                .Select(s => new AiWeekPlanSuggestion
                {
                    Date = LocalDatePattern.Iso.Format(s.Date),
                    DishId = s.Suggestion!.DishId.ToString(),
                    DishName = s.Suggestion.DishName,
                    Reason = s.Suggestion.Reasons.FirstOrDefault() ?? string.Empty,
                }).ToList();

            return new OkObjectResult(result);
        }

        private static IReadOnlyList<Guid> ParseExcludedDishIds(string[]? ids)
        {
            if (ids == null || ids.Length == 0)
                return Array.Empty<Guid>();

            var result = new List<Guid>();
            foreach (var id in ids)
            {
                if (Guid.TryParse(id, out var guid))
                    result.Add(guid);
            }
            return result;
        }
    }
}
