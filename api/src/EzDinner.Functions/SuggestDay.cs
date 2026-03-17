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
    public class SuggestDay
    {
        private readonly ILogger<SuggestDay> _logger;
        private readonly IDinnerSuggestionService _suggestionService;
        private readonly IMapper _mapper;
        private readonly IAuthzService _authz;

        public SuggestDay(
            ILogger<SuggestDay> logger,
            IDinnerSuggestionService suggestionService,
            IMapper mapper,
            IAuthzService authz)
        {
            _logger = logger;
            _suggestionService = suggestionService;
            _mapper = mapper;
            _authz = authz;
        }

        [Function(nameof(SuggestDay))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get",
                Route = "suggest/family/{familyId}/day/{date}")] HttpRequest req,
            string familyId,
            string date)
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, familyId, Resources.Dinner, Actions.Read))
                return new UnauthorizedResult();

            var parsedFamilyId = Guid.Parse(familyId);
            var parsedDate = LocalDatePattern.Iso.Parse(date).GetValueOrThrow();
            var excludedDishIds = ParseExcludedDishIds(req.Query["exclude"]);

            _logger.LogInformation("SuggestDay called for familyId={FamilyId}, date={Date}", familyId, date);

            var suggestion = await _suggestionService.SuggestDay(parsedFamilyId, parsedDate, excludedDishIds);

            if (suggestion is null)
                return new OkObjectResult((SuggestionQueryModel?)null);

            return new OkObjectResult(_mapper.Map<SuggestionQueryModel>(suggestion));
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
