using System;
using System.Threading;
using System.Threading.Tasks;
using EzDinner.Application.Commands.Dishes;
using EzDinner.Authorization.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace EzDinner.Functions
{
    public class DishEnrich
    {
        private readonly ILogger<DishEnrich> _logger;
        private readonly EnrichDishCommandHandler _handler;
        private readonly IAuthzService _authz;

        public DishEnrich(ILogger<DishEnrich> logger, EnrichDishCommandHandler handler, IAuthzService authz)
        {
            _logger = logger;
            _handler = handler;
            _authz = authz;
        }


        [Function(nameof(DishEnrich))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "families/{familyId}/dishes/{dishId}/enrich")] HttpRequest req,
            string familyId,
            string dishId,
            CancellationToken ct)
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();

            if (!Guid.TryParse(familyId, out var parsedFamilyId) || !Guid.TryParse(dishId, out var parsedDishId))
                return new BadRequestObjectResult("Invalid family or dish ID.");

            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, familyId, Resources.Dish, Actions.Update)) return new UnauthorizedResult();

            try
            {
                var command = new EnrichDishCommand(parsedFamilyId, parsedDishId);
                await _handler.Handle(command, ct);
                return new OkResult();
            }
            catch (InvalidOperationException e)
            {
                _logger.LogError(e, "Enrichment failed for dish '{DishId}' in family '{FamilyId}'", dishId, familyId);
                return new ObjectResult(e.Message) { StatusCode = StatusCodes.Status422UnprocessableEntity };
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
