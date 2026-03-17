using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EzDinner.Authorization.Core
{
    public interface IAuthzRepository
    {
        bool UserHasRole(Guid userId, Guid familyId, string role);
        Task AssignRoleToUser(Guid userId, Guid familyId, string role);
        Task RemoveRoleFromUserAsync(Guid userId, Guid familyId, string role);
        Task AssignPolicyToRole(string role, Guid familyId, string resource, string action);
        bool RoleHasPolicy(string role, Guid familyId, string resource, string action);

        bool VerifyUserPermission(string userId, string familyId, string resource, string action);
        Task ReloadPoliciesAsync();
    }
}
