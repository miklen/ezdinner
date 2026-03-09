using System;
using System.Linq;
using System.Threading.Tasks;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.FamilyAggregate;
using EzDinner.Functions.Models.Command;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace EzDinner.Functions
{
    public class DishUpdateRating
    {
        private readonly ILogger<DishUpdateName> _logger;
        private readonly IDishRepository _dishRepository;
        private readonly IAuthzService _authz;
        private readonly IFamilyRepository _familyRepository;

        public DishUpdateRating(ILogger<DishUpdateName> logger, IDishRepository dishRepository, IAuthzService authz, IFamilyRepository familyRepository)
        {
            _logger = logger;
            _dishRepository = dishRepository;
            _authz = authz;
            _familyRepository = familyRepository;
        }
        
        [Function(nameof(DishUpdateRating))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "dishes/{dishId}/rating")] HttpRequest req,
            string dishId
            )
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            var dish = await _dishRepository.GetDishAsync(Guid.Parse(dishId));
            if (dish is null) return new BadRequestObjectResult("DISH_NOT_FOUND");
            var userId = Guid.Parse(req.HttpContext.User.GetNameIdentifierId()!);
            if (!_authz.Authorize(userId, dish.FamilyId, Resources.Dish, Actions.Update)) return new UnauthorizedResult();
            
            var dishRating = await req.GetBodyAs<UpdateDishRatingCommandModel>();
            if (dishRating is null || dishRating.FamilyMemberId is null) return new BadRequestObjectResult("MISSING_VALUES");
            var family = await _familyRepository.GetFamily(dish.FamilyId);
            var familyMemberId = Guid.Parse(dishRating.FamilyMemberId);
            var familyMember = family!.FamilyMembers.FirstOrDefault(w => w.Id == familyMemberId);
            if (familyMember is null) return new BadRequestObjectResult("FAMILYMEMBER_NOT_FOUND_IN_FAMILY");
            
            // verify that you're rating as yourself or on behalf of a user that doesn't have autonomy - rating on behalf of other autonomus users is not allowed
            if (!familyMember.Id.Equals(userId) && familyMember.HasAutonomy) return new BadRequestObjectResult("NOT_ALLOWED");

            dish.SetRating(familyMemberId, dishRating.GetRatingInDomainFormat());
            await _dishRepository.SaveAsync(dish);

            return new OkResult();
        }
    }
}

