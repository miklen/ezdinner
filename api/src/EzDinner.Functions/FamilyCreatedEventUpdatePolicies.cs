using EzDinner.Application.Commands.Authorization;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.FamilyAggregate;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EzDinner.Functions
{
    public class FamilyCreatedEventUpdatePolicies
    {
        private readonly ILogger<FamilyCreatedEventUpdatePolicies> _logger;
        private readonly IAuthzService _authz;

        public FamilyCreatedEventUpdatePolicies(ILogger<FamilyCreatedEventUpdatePolicies> logger, IAuthzService authz)
        {
            _logger = logger;
            _authz = authz;
        }

        [Function(nameof(FamilyCreatedEventUpdatePolicies))]
        public async Task Run([CosmosDBTrigger(
            databaseName: "EzDinner",
            containerName: "Families",
            Connection = "CosmosDb:ConnectionString",
            LeaseContainerName = "Leases",
            LeaseContainerPrefix = "policies",
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<Family> input)
        {
            if (input != null && input.Count > 0)
            {
                _logger.LogInformation("Families updated " + input.Count);
                var updatePermissionsCommand = new UpdateAuthorizationPoliciesCommand(_authz);

                foreach (var family in input)
                {
                    _logger.LogInformation("Updating policies for family " + family.Id);
                    await updatePermissionsCommand.Handle(family);
                }
            }
        }
    }
}
