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

namespace EzDinner.Functions
{
    public class DishesGet
    {
        private readonly ILogger<DishCreate> _logger;
        private readonly IDishQueryRepository _dishRepository;
        private readonly IAuthzService _authz;

        public DishesGet(ILogger<DishCreate> logger, IDishQueryRepository dishRepository, IAuthzService authz)
        {
            _logger = logger;
            _dishRepository = dishRepository;
            _authz = authz;
        }
        
        [Function(nameof(DishesGet))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dishes/family/{familyId}")] HttpRequest req,
            string familyId
            )
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, familyId, Resources.Dish, Actions.Read)) return new UnauthorizedResult();

            _logger.LogInformation("GetDishes called for familyId " + familyId);
            var parsedId = Guid.Parse(familyId);
            var includeArchived = string.Equals(req.Query["includeArchived"], "true", StringComparison.OrdinalIgnoreCase);
            var dishes = await _dishRepository.GetDishesAsync(parsedId, includeArchived);

            return new OkObjectResult(dishes.Select(DishesQueryModel.FromDomain));
        }
    }
}

