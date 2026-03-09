using Casbin;
using Casbin.Persist.Adapter.EFCore;
using EzDinner.Authorization.Core;
using EzDinner.Infrastructure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace EzDinner.IntegrationTests.AuthorizationTests
{
    public class CasbinTests : IClassFixture<StartupFixture>
    {
        private IServiceProvider _provider;
        private Enforcer _enforcer;
        private IAuthzService _authz;
        private Enforcer Enforcer => _enforcer ??= new Enforcer(AuthzRepository.GetRbacWithDomainsModel(), (CasbinCosmosAdapter)_provider.GetService(typeof(CasbinCosmosAdapter)));
        private IAuthzService Authz => _authz ??= new AuthzService(new AuthzRepository(Enforcer));

        public CasbinTests(StartupFixture startupFixture)
        {
            _provider = startupFixture.Provider;
        }

        [Fact]
        public void Context_NoDatabase_CanCreate()
        {
            var context = (CasbinDbContext<string>)_provider.GetService(typeof(CasbinDbContext<string>));
            context.Database.EnsureCreated();
        }

        [Fact]
        public void Enforcer_NoRoles_CanEnforce()
        {
            // Arrange
            Enforcer.Enforce(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "Test", "Test");
        }

        [Fact]
        public async Task Enforcer_NoRoles_CanAddRole()
        {
            // Arrange
            var roleDefinitionPolicy = CreateOwnerRolePolicy(Guid.NewGuid());
            try
            {
                // Act
                await Enforcer.AddPolicyAsync(roleDefinitionPolicy);

                // Assert
                var exists = Enforcer.HasPolicy(roleDefinitionPolicy);
                Assert.True(exists);
            }
            finally
            {
                await Enforcer.RemovePolicyAsync(roleDefinitionPolicy);
            }
        }

        [Fact]
        public async Task Enforcer_NoUsersInRole_CanAddUserToRole()
        {
            // Arrange
            var roleAssignmentPolicy = CreateRoleAssignmentPolicy(Guid.NewGuid(), Guid.NewGuid(), "Owner") ;
            try
            {
                // Act
                await Enforcer.AddGroupingPolicyAsync(roleAssignmentPolicy);

                // Assert
                var exists = Enforcer.HasGroupingPolicy(roleAssignmentPolicy);
                Assert.True(exists);
            }
            finally
            {
                await Enforcer.RemoveGroupingPolicyAsync(roleAssignmentPolicy);
            }
        }


        [Fact]
        public async Task Enforcer_UserAssignedToOwnerRole_AnyActionAllowedInDomain()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var domainId = Guid.NewGuid();
            var roleDefinitionPolicy = CreateOwnerRolePolicy(domainId);
            var roleAssignmentPolicy = CreateRoleAssignmentPolicy(userId, domainId, "Owner");
            try
            {
                await Enforcer.AddPolicyAsync(roleDefinitionPolicy);
                await Enforcer.AddGroupingPolicyAsync(roleAssignmentPolicy);

                // Act
                var result1 = await Enforcer.EnforceAsync(userId.ToString(), domainId.ToString(), "Permission.Family", "Update");
                var result2 = await Enforcer.EnforceAsync(userId.ToString(), domainId.ToString(), "Something.Random", "RandomAction");

                // Assert
                Assert.True(result1);
                Assert.True(result2);
            }
            finally
            {
                await Enforcer.RemoveGroupingPolicyAsync(roleAssignmentPolicy);
                await Enforcer.RemovePolicyAsync(roleDefinitionPolicy);
            }
        }

        [Fact]
        public async Task Enforcer_UserAssignedToOwnerRole_NotAllowedActionsInOtherDomains()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var domainId = Guid.NewGuid();
            var roleDefinitionPolicy = CreateOwnerRolePolicy(domainId);
            var roleAssignmentPolicy = CreateRoleAssignmentPolicy(userId, domainId, "Owner");
            var otherDomain = Guid.NewGuid().ToString();
            try
            {
                await Enforcer.AddPolicyAsync(roleDefinitionPolicy);
                await Enforcer.AddGroupingPolicyAsync(roleAssignmentPolicy);

                // Act
                var result = await Enforcer.EnforceAsync(userId.ToString(), otherDomain, "Permission.Family", "Update");
                
                // Assert
                Assert.False(result);
            }
            finally
            {
                await Enforcer.RemoveGroupingPolicyAsync(roleAssignmentPolicy);
                await Enforcer.RemovePolicyAsync(roleDefinitionPolicy);
            }
        }


        [Fact]
        public async Task PermissionService_User_CanAddRoles()
        {
            // Arrange
            var domainId = Guid.NewGuid();
            var roleDefinitionPolicy = CreateOwnerRolePolicy(domainId);
            try
            {
                await Authz.CreateOwnerRolePermissionsAsync(domainId);

                // Act
                var exists = Enforcer.HasPolicy(roleDefinitionPolicy);

            } finally
            {
                await Enforcer.RemovePolicyAsync(roleDefinitionPolicy);
            }

        }

        private static string[] CreateRoleAssignmentPolicy(Guid userId, Guid domainId, string role)
        {
            var rolePolicy = new[] { userId.ToString(), role, domainId.ToString() };
            return rolePolicy;
        }
       
        private static string[] CreateOwnerRolePolicy(Guid domainId)
        {
            var role = "Owner";
            var permission = "*";
            var action = "*";
            return CreateRolePolicy(role, domainId, permission, action);
        }

        private static string[] CreateRolePolicy(string role, Guid domainId, string permission, string action)
        {
            var policy = new[] { role, domainId.ToString(), permission, action };
            return policy;
        }
    }
}
