using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using EzDinner.Application.Commands.FamilyMembers;
using EzDinner.Authorization.Core;
using EzDinner.Functions.Models.Command;

namespace EzDinner.Functions
{
    public class FamilySetMemberRole
    {
        private readonly ILogger<FamilySetMemberRole> _logger;
        private readonly SetMemberRoleCommand _command;
        private readonly IAuthzService _authz;

        public FamilySetMemberRole(ILogger<FamilySetMemberRole> logger, SetMemberRoleCommand command, IAuthzService authz)
        {
            _logger = logger;
            _command = command;
            _authz = authz;
        }

        [Function(nameof(FamilySetMemberRole))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "families/{familyId}/member/{memberId}/role")] HttpRequest req,
            string familyId,
            string memberId)
        {
            if (req.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
            if (!_authz.Authorize(req.HttpContext.User.GetNameIdentifierId()!, familyId, Resources.Family, Actions.Update)) return new UnauthorizedResult();

            if (!Guid.TryParse(familyId, out var familyGuid)) return new BadRequestObjectResult("INVALID_FAMILYID");
            if (!Guid.TryParse(memberId, out var memberGuid)) return new BadRequestObjectResult("INVALID_MEMBERID");

            var body = await req.GetBodyAs<SetMemberRoleCommandModel>();

            try
            {
                await _command.Handle(familyGuid, memberGuid, body.IsOwner);
                return new OkResult();
            }
            catch (InvalidOperationException ex) when (ex.Message == "FAMILY_NOT_FOUND" || ex.Message == "MEMBER_NOT_FOUND")
            {
                return new NotFoundObjectResult(ex.Message);
            }
            catch (InvalidOperationException ex) when (
                ex.Message == "LAST_OWNER_CANNOT_BE_DEMOTED" ||
                ex.Message == "CANNOT_CHANGE_ROLE_OF_NON_AUTONOMOUS_MEMBER")
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
