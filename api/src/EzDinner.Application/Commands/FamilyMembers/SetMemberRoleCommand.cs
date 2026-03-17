using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.FamilyAggregate;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EzDinner.Application.Commands.FamilyMembers
{
    public class SetMemberRoleCommand
    {
        private readonly IFamilyRepository _familyRepository;
        private readonly IAuthzService _authz;

        public SetMemberRoleCommand(IFamilyRepository familyRepository, IAuthzService authz)
        {
            _familyRepository = familyRepository;
            _authz = authz;
        }

        public async Task Handle(Guid familyId, Guid memberId, bool isOwner)
        {
            var family = await _familyRepository.GetFamily(familyId)
                ?? throw new InvalidOperationException("FAMILY_NOT_FOUND");

            var member = family.FamilyMembers.FirstOrDefault(m => m.Id == memberId)
                ?? throw new InvalidOperationException("MEMBER_NOT_FOUND");

            var previousRole = member.IsOwner ? Roles.Owner : Roles.FamilyMember;
            var newRole = isOwner ? Roles.Owner : Roles.FamilyMember;

            family.SetMemberRole(memberId, isOwner);
            await _familyRepository.SaveAsync(family);

            await _authz.RemoveRoleFromUserAsync(memberId, previousRole, familyId);
            await _authz.AssignRoleToUserAsync(memberId, newRole, familyId);
            await _authz.ReloadPoliciesAsync();
        }
    }
}
