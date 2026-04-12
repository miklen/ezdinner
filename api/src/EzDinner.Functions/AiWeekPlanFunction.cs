using EzDinner.Query.Core.AiPlanningQueries;
using EzDinner.Authorization.Core;
using EzDinner.Functions.Models.Command;
using EzDinner.Functions.Models.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EzDinner.Functions
{
    public class AiWeekPlanFunction
    {
        private readonly ILogger<AiWeekPlanFunction> _logger;
        private readonly IAiWeekPlannerService _plannerService;
        private readonly IAuthzService _authz;

        public AiWeekPlanFunction(
            ILogger<AiWeekPlanFunction> logger,
            IAiWeekPlannerService plannerService,
            IAuthzService authz)
        {
            _logger = logger;
            _plannerService = plannerService;
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

            _logger.LogInformation(
                "AiWeekPlan requested for family={FamilyId}, weekStart={WeekStart}",
                familyId, request.WeekStart);

            var suggestions = await _plannerService.PlanWeekAsync(
                parsedFamilyId,
                request.WeekStart,
                request.Context,
                request.ExcludedDishIds,
                req.HttpContext.RequestAborted);

            var result = suggestions.Select(s => new AiWeekPlanSuggestion
            {
                Date = s.Date,
                DishId = s.DishId.ToString(),
                DishName = s.DishName,
                Reason = s.Reason,
            }).ToList();

            return new OkObjectResult(result);
        }
    }
}
