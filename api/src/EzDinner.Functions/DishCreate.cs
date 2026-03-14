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
    public class DishCreate
    {
        private readonly ILogger<DishCreate> _logger;
        private readonly IDishRepository _dishRepository;
        private readonly IAuthzService _authz;

        public DishCreate(ILogger<DishCreate> logger, IDishRepository dishRepository, IAuthzService authz)
        {
            _logger = logger;
            _dishRepository = dishRepository;
            _authz = authz;
        }
        
        [Function(nameof(DishCreate))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "dishes")] HttpRequest req
            )
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            var newDish = await req.GetBodyAs<CreateDishCommandModel>();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, newDish.FamilyId, Resources.Dish, Actions.Create)) return new UnauthorizedResult();
           
            if (string.IsNullOrWhiteSpace(newDish.Name)) return new BadRequestObjectResult("MISSING_NAME");
            if (newDish.FamilyId == Guid.Empty) return new BadRequestObjectResult("MISSING_FAMILYID");

            var dish = Dish.CreateNew(newDish.FamilyId, newDish.Name);
            await _dishRepository.SaveAsync(dish);

            return new OkObjectResult(dish.Id);
        }
    }
}

