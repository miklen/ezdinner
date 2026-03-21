using System;
using System.Threading;
using System.Threading.Tasks;
using EzDinner.Application.Commands.Dishes;
using EzDinner.Authorization.Core;
using EzDinner.Functions.Models.Command;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace EzDinner.Functions
{
    public class DishUpdateMetadata
    {
        private readonly ILogger<DishUpdateMetadata> _logger;
        private readonly UpdateDishMetadataCommandHandler _handler;
        private readonly IAuthzService _authz;

        public DishUpdateMetadata(ILogger<DishUpdateMetadata> logger, UpdateDishMetadataCommandHandler handler, IAuthzService authz)
        {
            _logger = logger;
            _handler = handler;
            _authz = authz;
        }

        [Function(nameof(DishUpdateMetadata))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "families/{familyId}/dishes/{dishId}/metadata")] HttpRequest req,
            string familyId,
            string dishId,
            CancellationToken ct)
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();

            if (!Guid.TryParse(familyId, out var parsedFamilyId) || !Guid.TryParse(dishId, out var parsedDishId))
                return new BadRequestObjectResult("Invalid family or dish ID.");

            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, familyId, Resources.Dish, Actions.Update)) return new UnauthorizedResult();

            var body = await req.GetBodyAs<UpdateDishMetadataCommandModel>();
            if (body is null) return new BadRequestObjectResult("Request body is required.");

            try
            {
                var command = new UpdateDishMetadataCommand(
                    parsedFamilyId,
                    parsedDishId,
                    body.Roles,
                    body.EffortLevel,
                    body.SeasonAffinity,
                    body.Cuisine);
                await _handler.Handle(command, ct);
                return new OkResult();
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
