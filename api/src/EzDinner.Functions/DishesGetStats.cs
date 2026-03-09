using System;
using System.Linq;
using System.Threading.Tasks;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Functions.Models.Query;
using EzDinner.Query.Core.DishQueries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using NodaTime;

namespace EzDinner.Functions
{
    public class DishesGetStats
    {
        private readonly ILogger<DishesGetStats> _logger;
        private readonly IDishQueryService _dishQueryService;
        private readonly IAuthzService _authz;

        public DishesGetStats(ILogger<DishesGetStats> logger, IDishQueryService dishQueryService, IAuthzService authz)
        {
            _logger = logger;
            _dishQueryService = dishQueryService;
            _authz = authz;
        }
        
        [Function(nameof(DishesGetStats))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dishes/stats/family/{familyId}")] HttpRequest req,
            string familyId
            )
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId() ?? "", familyId, Resources.Dish, Actions.Read)) return new UnauthorizedResult();

            _logger.LogInformation("GetDishes called for familyId " + familyId);
            var parsedId = Guid.Parse(familyId);
            var dishes = await _dishQueryService.GetDishUsageStatsAsync(parsedId, LocalDate.MinIsoValue, LocalDate.MaxIsoValue);
            return new OkObjectResult(dishes);
        }
    }
}

