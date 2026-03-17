using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace EzDinner.Authorization.Core
{
    /// <summary>
    /// Casbin implementation of IPermissionService. Implements an RBAC with domain solution
    /// which is defined in the `rbac_with_domains.conf` file and loaded into the Enforcer.
    /// 
    /// The model allows wildcard matches in place of object and actions. In our usage
    /// object represents a resource in the system where CRUD actions can be granted access to.
    /// 
    /// Policy examples to show how the RBAC with domains model grants access:
    /// 
    /// Role definitions: (role, familyId, resource, action)
    /// p, admin, domain1, data1, read
    /// p, admin, domain1, data1, write
    /// p, admin, domain2, data2, read
    /// p, admin, domain2, data2, write
    /// p, owner, domain2, *, *
    /// p, all_data2, domain2, data2, *
    /// p, all_read, domain2, *, read
    ///
    /// User assignment to roles: (userId, role, familyId)
    /// g, alice, admin, domain1
    /// g, bob, admin, domain2
    /// g, mikkel, owner, domain2
    /// g, foo, all_data2, domain2
    /// g, bar, all_read, domain2
    /// 
    /// Enforcements:
    /// alice, domain1, data2, read = false
    /// alice, domain2, data3, read = false
    /// mikkel, domain2, data2, write = true
    /// mikkel, domain2, data3, write = true
    /// mikkel, domain1, data1, write = false
    /// bob, domain2, data3, write = false
    /// bob, domain2, data2, write = true
    /// foo, domain2, data2, read = true
    /// foo, domain2, data2, write = true
    /// foo, domain2, data1, read = false
    /// foo, domain1, data2, write = false
    /// bar, domain2, data1, read = true
    /// bar, domain2, data1, write = false
    /// bar, domain2, data2, read = true
    /// </summary>
    public class AuthzService : IAuthzService
    {
        private readonly IAuthzRepository _authzRepository;

        public AuthzService(IAuthzRepository authzRepository)
        {
            _authzRepository = authzRepository;
        }

        public Task AssignRoleToUserAsync(Guid userId, string role, Guid familyId)
        {
            if (!_authzRepository.UserHasRole(userId, familyId, role))
            {
                return _authzRepository.AssignRoleToUser(userId, familyId, role);
            }
            return Task.CompletedTask;
        }

        public Task RemoveRoleFromUserAsync(Guid userId, string role, Guid familyId)
        {
            return _authzRepository.RemoveRoleFromUserAsync(userId, familyId, role);
        }

        public Task CreateOwnerRolePermissionsAsync(Guid familyId)
        {
            if (!_authzRepository.RoleHasPolicy(Roles.Owner, familyId, Resources.All, Actions.All))
            {
                return _authzRepository.AssignPolicyToRole(Roles.Owner, familyId, Resources.All, Actions.All);
            }
            return Task.CompletedTask;
        }

        public async Task CreateFamilyMemberRolePermissionsAsync(Guid familyId)
        {
            // TODO: Refactor to satisfy open/close principle. More policies will be added over time.
            if (!_authzRepository.RoleHasPolicy(Roles.FamilyMember, familyId, Resources.Dinner, Actions.All))
            {
                await _authzRepository.AssignPolicyToRole(Roles.FamilyMember, familyId, Resources.Dinner, Actions.All);
            }

            if (!_authzRepository.RoleHasPolicy(Roles.FamilyMember, familyId, Resources.Dish, Actions.All))
            {
                await _authzRepository.AssignPolicyToRole(Roles.FamilyMember, familyId, Resources.Dish, Actions.All);
            }

            if (!_authzRepository.RoleHasPolicy(Roles.FamilyMember, familyId, Resources.Family, Actions.Read))
            {
                await _authzRepository.AssignPolicyToRole(Roles.FamilyMember, familyId, Resources.Family, Actions.Read);
            }
        }

        public bool Authorize(Guid userId, Guid familyId, string resource, string action)
        {
            return Authorize(userId.ToString(), familyId.ToString(), resource, action);
        }

        public bool Authorize(string userId, Guid familyId, string resource, string action)
        {
            return Authorize(userId, familyId.ToString(), resource, action);
        }

        public bool Authorize(string userId, string familyId, string resource, string action)
        {
            return _authzRepository.VerifyUserPermission(userId, familyId, resource, action);
        }

        public Task ReloadPoliciesAsync()
        {
            return _authzRepository.ReloadPoliciesAsync();
        }
    }
}
