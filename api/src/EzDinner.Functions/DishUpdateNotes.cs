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
    public class DishUpdateNotes
    {
        private readonly ILogger<DishUpdateName> _logger;
        private readonly IDishRepository _dishRepository;
        private readonly IAuthzService _authz;

        public DishUpdateNotes(ILogger<DishUpdateName> logger, IDishRepository dishRepository, IAuthzService authz)
        {
            _logger = logger;
            _dishRepository = dishRepository;
            _authz = authz;
        }
        
        [Function(nameof(DishUpdateNotes))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "dishes/{dishId}/notes")] HttpRequest req,
            string dishId
            )
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            var dish = await _dishRepository.GetDishAsync(Guid.Parse(dishId));
            if (dish is null) return new BadRequestObjectResult("DISH_NOT_FOUND");
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, dish.FamilyId, Resources.Dish, Actions.Update)) return new UnauthorizedResult();

            var dishNotes = await req.GetBodyAs<UpdateDishNotesCommandModel>();

            dish.SetUrl(dishNotes.Url);
            dish.SetNotes(dishNotes.Notes);
            await _dishRepository.SaveAsync(dish);

            return new OkResult();
        }
    }
}

