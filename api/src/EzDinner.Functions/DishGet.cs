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
    public class DishGet
    {
        private readonly ILogger<DishGet> _logger;
        private readonly IDishRepository _dishRepository;
        private readonly IAuthzService _authz;

        public DishGet(ILogger<DishGet> logger, IDishRepository dishRepository, IAuthzService authz)
        {
            _logger = logger;
            _dishRepository = dishRepository;
            _authz = authz;
        }
        
        [Function(nameof(DishGet))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dishes/{dishId}")] HttpRequest req,
            string dishId
            )
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            var dish = await _dishRepository.GetDishAsync(Guid.Parse(dishId));
            if (dish is null) return new BadRequestObjectResult("DISH_NOT_FOUND");
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, dish.FamilyId, Resources.Dish, Actions.Read)) return new UnauthorizedResult();
            return new OkObjectResult(DishesQueryModel.FromDomain(dish));
        }
    }
}

