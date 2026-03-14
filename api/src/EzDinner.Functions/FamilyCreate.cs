using System;
using System.IO;
using System.Threading.Tasks;
using EzDinner.Core.Aggregates.FamilyAggregate;
using EzDinner.Functions.Models.Command;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace EzDinner.Functions
{
    public class FamilyCreate
    {
        private readonly ILogger<FamilyCreate> _logger;
        private readonly IFamilyRepository _familyRepository;

        public FamilyCreate(ILogger<FamilyCreate> logger, IFamilyRepository familyRepository)
        {
            _logger = logger;
            _familyRepository = familyRepository;
        }

        [Function(nameof(FamilyCreate))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "families")] HttpRequest req
            )
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();

            var newFamily = await req.GetBodyAs<CreateFamilyCommandModel>();
            if (string.IsNullOrWhiteSpace(newFamily.Name)) return new BadRequestObjectResult("Name cannot be null or empty");

            await _familyRepository.SaveAsync(Family.CreateNew(Guid.Parse(req.HttpContext.User.GetNameIdentifierId() ?? ""), newFamily.Name));

            return new OkResult();
        }
    }
}

