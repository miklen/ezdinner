using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EzDinner.Authorization.Core
{
    public interface IAuthzService
    {
        /// <summary>
        /// Idempotent creates the Owner role and assigns permissions for a family.
        /// </summary>
        /// <param name="familyId"></param>
        /// <returns></returns>
        Task CreateOwnerRolePermissionsAsync(Guid familyId);
        /// <summary>
        /// Idempotent assigns a user to a role.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="role">A role defined in <see cref="Roles"/></param>
        /// <param name="familyId"></param>
        /// <returns></returns>
        Task AssignRoleToUserAsync(Guid userId, string role, Guid familyId);
        Task RemoveRoleFromUserAsync(Guid userId, string role, Guid familyId);
        /// <summary>
        /// Idempotent creates definition and asssigns permissions to the FamilyMember role
        /// </summary>
        /// <param name="familyId"></param>
        /// <returns></returns>
        Task CreateFamilyMemberRolePermissionsAsync(Guid familyId);
        /// <summary>
        /// Authorize a user's actions on a resource in the scope of a family.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="familyId"></param>
        /// <param name="resource">A resource from <see cref="Resources"/></param>
        /// <param name="action">An action <see cref="Actions>"/></param>
        /// <returns>True if the user has the required permissions. False if not.</returns>
        bool Authorize(Guid userId, Guid familyId, string resource, string action);
        /// <summary>
        /// Authorize a user's actions on a resource in the scope of a family.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="familyId"></param>
        /// <param name="resource">A resource from <see cref="Resources"/></param>
        /// <param name="action">An action <see cref="Actions>"/></param>
        /// <returns>True if the user has the required permissions. False if not.</returns>
        bool Authorize(string userId, Guid familyId, string resource, string action);
        /// <summary>
        /// Authorize a user's actions on a resource in the scope of a family.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="familyId"></param>
        /// <param name="resource">A resource from <see cref="Resources"/></param>
        /// <param name="action">An action <see cref="Actions>"/></param>
        /// <returns>True if the user has the required permissions. False if not.</returns>
        bool Authorize(string userId, string familyId, string resource, string action);
        /// <summary>
        /// Reloads the Casbin policy model from the database on this instance.
        /// Call after writing new policies to ensure the in-memory model reflects the latest state.
        /// </summary>
        Task ReloadPoliciesAsync();
    }
}
