using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using EzDinner.Core.Aggregates.UserAggregate;
using EzDinner.Core.Aggregates.FamilyAggregate;
using EzDinner.Functions.Models.Command;
using EzDinner.Authorization.Core;

namespace EzDinner.Functions
{
    public class FamilyInviteMember
    {
        private readonly ILogger<FamilyInviteMember> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IFamilyRepository _familyRepository;
        private readonly IAuthzService _authz;

        public FamilyInviteMember(ILogger<FamilyInviteMember> logger, IUserRepository userRepository, IFamilyRepository familyRepository, IAuthzService authz)
        {
            _logger = logger;
            _userRepository = userRepository;
            _familyRepository = familyRepository;
            _authz = authz;
        }
        
        /// <summary>
        /// Invite user to become a family member in a family.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="familyId"></param>
        /// <returns></returns>
        [FunctionName(nameof(FamilyInviteMember))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "family/{familyId}/member")] HttpRequest req,
            string familyId)
        {

            var (authenticationStatus, authenticationResponse) = await req.HttpContext.AuthenticateAzureFunctionAsync();
            if (!authenticationStatus) return authenticationResponse;
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, familyId, Resources.Family, Actions.Update)) return new UnauthorizedResult();

            // TODO: Refactor to FamilyService - responsibilty is to handle cross-aggregate logic
            var familyGuid = Guid.Parse(familyId);
            if (familyGuid.Equals(Guid.Empty)) return new BadRequestObjectResult("MISSING_FAMILYID");
            
            var command = await req.GetBodyAs<InviteFamilyMemberCommandModel>();
            if (string.IsNullOrEmpty(command.Email)) return new BadRequestObjectResult("MISSING_EMAIL");

            var user = await _userRepository.GetUser(command.Email);
            if (user is null) return new NoContentResult();

            var family = await _familyRepository.GetFamily(familyGuid);
            if (family is null) return new BadRequestObjectResult("NOT_FOUND_FAMILY");

            family.InviteFamilyMember(user.Id);
            await _familyRepository.SaveAsync(family);
            return new OkResult();
        }
    }
}
