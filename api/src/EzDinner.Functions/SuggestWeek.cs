using AutoMapper;
using EzDinner.Authorization.Core;
using EzDinner.Functions.Models.Query;
using EzDinner.Query.Core.SuggestionQueries;
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
    public class SuggestWeek
    {
        private readonly ILogger<SuggestWeek> _logger;
        private readonly IDinnerSuggestionService _suggestionService;
        private readonly IMapper _mapper;
        private readonly IAuthzService _authz;

        public SuggestWeek(
            ILogger<SuggestWeek> logger,
            IDinnerSuggestionService suggestionService,
            IMapper mapper,
            IAuthzService authz)
        {
            _logger = logger;
            _suggestionService = suggestionService;
            _mapper = mapper;
            _authz = authz;
        }

        [Function(nameof(SuggestWeek))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get",
                Route = "suggest/family/{familyId}/week/{weekStart}")] HttpRequest req,
            string familyId,
            string weekStart)
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, familyId, Resources.Dinner, Actions.Read))
                return new UnauthorizedResult();

            var parsedFamilyId = Guid.Parse(familyId);
            var parsedWeekStart = LocalDatePattern.Iso.Parse(weekStart).GetValueOrThrow();
            var excludedDishIds = ParseExcludedDishIds(req.Query["exclude"]);

            _logger.LogInformation("SuggestWeek called for familyId={FamilyId}, weekStart={WeekStart}", familyId, weekStart);

            var suggestions = await _suggestionService.SuggestWeek(parsedFamilyId, parsedWeekStart, excludedDishIds);

            return new OkObjectResult(suggestions.Select(_mapper.Map<WeekSuggestionItemQueryModel>).ToList());
        }

        private static IReadOnlyList<Guid> ParseExcludedDishIds(Microsoft.Extensions.Primitives.StringValues exclude)
        {
            if (string.IsNullOrEmpty(exclude))
                return Array.Empty<Guid>();

            return exclude.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Guid.TryParse(s.Trim(), out var g) ? (Guid?)g : null)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToList();
        }
    }
}
