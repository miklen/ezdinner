using System;
using System.Threading.Tasks;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Functions.Models.Command;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace EzDinner.Functions
{
    public class DishUpdateName
    {
        private readonly ILogger<DishUpdateName> _logger;
        private readonly IDishRepository _dishRepository;
        private readonly IAuthzService _authz;

        public DishUpdateName(ILogger<DishUpdateName> logger, IDishRepository dishRepository, IAuthzService authz)
        {
            _logger = logger;
            _dishRepository = dishRepository;
            _authz = authz;
        }
        
        [Function(nameof(DishUpdateName))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "dishes/{dishId}/name")] HttpRequest req,
            string dishId
            )
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            var dish = await _dishRepository.GetDishAsync(Guid.Parse(dishId));
            if (dish is null) return new BadRequestObjectResult("DISH_NOT_FOUND");
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, dish.FamilyId, Resources.Dish, Actions.Update)) return new UnauthorizedResult();

            var newDishName = await req.GetBodyAs<UpdateDishNameCommandModel>();
            if (string.IsNullOrWhiteSpace(newDishName?.Name)) return new BadRequestObjectResult("MISSING_NAME");

            dish.Name = newDishName.Name;
            await _dishRepository.SaveAsync(dish);

            return new OkResult();
        }
    }
}

