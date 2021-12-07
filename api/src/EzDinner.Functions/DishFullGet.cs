using System;
using System.Linq;
using System.Threading.Tasks;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Functions.Models.Query;
using EzDinner.Query.Core.DishQueries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;

namespace EzDinner.Functions
{
    public class DishFullGet
    {
        private readonly ILogger<DishGet> _logger;
        private readonly IDishRepository _dishRepository;
        private readonly IDinnerRepository _dinnerRepository;
        private readonly IAuthzService _authz;

        public DishFullGet(ILogger<DishGet> logger, IDishRepository dishRepository, IDinnerRepository dinnerRepository, IAuthzService authz)
        {
            _logger = logger;
            _dishRepository = dishRepository;
            _dinnerRepository = dinnerRepository;
            _authz = authz;
        }
        
        [FunctionName(nameof(DishFullGet))]
        [RequiredScope("backendapi")]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dishes/{dishId}/full/family/{familyId}")] HttpRequest req,
            string dishId,
            string familyId
            )
        {
            var (authenticationStatus, authenticationResponse) = await req.HttpContext.AuthenticateAzureFunctionAsync();
            if (!authenticationStatus) return authenticationResponse;
            var dish = await _dishRepository.GetDishAsync(Guid.Parse(dishId));
            if (dish is null || !dish.FamilyId.Equals(Guid.Parse(familyId))) return new BadRequestObjectResult("DISH_NOT_FOUND");
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, dish.FamilyId, Resources.Dish, Actions.Read)) return new UnauthorizedResult();
            var dinners = await _dinnerRepository.GetAsync(dish.FamilyId, dish.Id).ToListAsync();
            return new OkObjectResult(DishDetails.CreateNew(dish, dinners));
        }
    }
}

