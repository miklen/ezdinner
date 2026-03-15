using System;
using System.Threading.Tasks;
using EzDinner.Application.Commands.FamilyMembers;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.FamilyAggregate;
using EzDinner.Functions.Models.Command;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace EzDinner.Functions
{
    public class FamilyMergeMember
    {
        private readonly ILogger<FamilyMergeMember> _logger;
        private readonly IFamilyRepository _familyRepository;
        private readonly IAuthzService _authz;
        private readonly MergeNonAutonomousMemberCommand _mergeCommand;

        public FamilyMergeMember(
            ILogger<FamilyMergeMember> logger,
            IFamilyRepository familyRepository,
            IAuthzService authz,
            MergeNonAutonomousMemberCommand mergeCommand)
        {
            _logger = logger;
            _familyRepository = familyRepository;
            _authz = authz;
            _mergeCommand = mergeCommand;
        }

        /// <summary>
        /// Merge a non-autonomous family member into an autonomous account.
        /// Can be initiated by the family owner or the autonomous member themselves.
        /// </summary>
        [Function(nameof(FamilyMergeMember))]
        public async Task<IActionResult?> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "families/{familyId}/member/{nonAutonomousMemberId}/merge")] HttpRequest req,
            string familyId,
            string nonAutonomousMemberId)
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, familyId, Resources.Family, Actions.Read)) return new UnauthorizedResult();

            var command = await req.GetBodyAs<MergeFamilyMemberCommandModel>();
            if (string.IsNullOrEmpty(command?.AutonomousMemberId)) return new BadRequestObjectResult("MISSING_AUTONOMOUS_MEMBER_ID");

            if (!Guid.TryParse(command.AutonomousMemberId, out var autonomousGuid)) return new BadRequestObjectResult("INVALID_AUTONOMOUS_MEMBER_ID");
            if (!Guid.TryParse(familyId, out var familyGuid)) return new BadRequestObjectResult("INVALID_FAMILY_ID");
            if (!Guid.TryParse(nonAutonomousMemberId, out var nonAutonomousGuid)) return new BadRequestObjectResult("INVALID_NON_AUTONOMOUS_MEMBER_ID");

            var family = await _familyRepository.GetFamily(familyGuid);
            if (family is null) return new BadRequestObjectResult("FAMILY_NOT_FOUND");

            var callerId = Guid.Parse(req.HttpContext.User.GetNameIdentifierId()!);
            if (callerId != family.OwnerId && callerId != autonomousGuid) return new UnauthorizedResult();

            try
            {
                await _mergeCommand.Handle(familyGuid, nonAutonomousGuid, autonomousGuid);
                return new OkResult();
            }
            catch (InvalidOperationException e)
            {
                return new BadRequestObjectResult(e.Message);
            }
        }
    }
}
