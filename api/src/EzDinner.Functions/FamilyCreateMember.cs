using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using EzDinner.Core.Aggregates.UserAggregate;
using EzDinner.Core.Aggregates.FamilyAggregate;
using EzDinner.Functions.Models.Command;
using EzDinner.Authorization.Core;

namespace EzDinner.Functions
{
    public class FamilyCreateMember
    {
        private readonly ILogger<FamilyCreateMember> _logger;
        private readonly IFamilyRepository _familyRepository;
        private readonly IAuthzService _authz;

        public FamilyCreateMember(ILogger<FamilyCreateMember> logger, IFamilyRepository familyRepository, IAuthzService authz)
        {
            _logger = logger;
            _familyRepository = familyRepository;
            _authz = authz;
        }
        
        /// <summary>
        /// Invite user to become a family member in a family.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="familyId"></param>
        /// <returns></returns>
        [Function(nameof(FamilyCreateMember))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "families/{familyId}/member/noautonomy")] HttpRequest req,
            string familyId)
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, familyId, Resources.Family, Actions.Update)) return new UnauthorizedResult();

            var familyGuid = Guid.Parse(familyId);
            if (familyGuid.Equals(Guid.Empty)) return new BadRequestObjectResult("MISSING_FAMILYID");
            
            var command = await req.GetBodyAs<CreateFamilyMemberCommandModel>();
            if (string.IsNullOrEmpty(command.Name)) return new BadRequestObjectResult("MISSING_NAME");

            var family = await _familyRepository.GetFamily(familyGuid);
            if (family is null) return new BadRequestObjectResult("NOT_FOUND_FAMILY");

            family.CreateFamilyMember(command.Name);
            await _familyRepository.SaveAsync(family);
            return new OkResult();
        }
    }
}
