using System;
using System.Threading.Tasks;
using EzDinner.Application.Commands.Dishes;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DishAggregate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace EzDinner.Functions
{
    public class DishArchive
    {
        private readonly ILogger<DishArchive> _logger;
        private readonly IDishRepository _dishRepository;
        private readonly IAuthzService _authz;

        public DishArchive(ILogger<DishArchive> logger, IDishRepository dishRepository, IAuthzService authz)
        {
            _logger = logger;
            _dishRepository = dishRepository;
            _authz = authz;
        }

        [Function(nameof(DishArchive))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "families/{familyId}/dishes/{dishId}/archive")] HttpRequest req,
            string familyId,
            string dishId)
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, familyId, Resources.Dish, Actions.Update)) return new UnauthorizedResult();

            try
            {
                var command = new ArchiveDishCommand(_dishRepository);
                await command.Handle(Guid.Parse(familyId), Guid.Parse(dishId));
                return new OkResult();
            }
            catch (InvalidOperationException e)
            {
                return new ConflictObjectResult(e.Message);
            }
            catch (KeyNotFoundException e)
            {
                return new NotFoundObjectResult(e.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return new UnauthorizedResult();
            }
            catch (Exception e) when (e is ArgumentException || e is ArgumentNullException)
            {
                return new BadRequestObjectResult(e.Message);
            }
        }
    }
}
