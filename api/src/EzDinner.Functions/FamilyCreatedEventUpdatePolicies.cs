using EzDinner.Application.Commands.Authorization;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.FamilyAggregate;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace EzDinner.Functions
{
    public class FamilyCreatedEventUpdatePolicies
    {
        private readonly ILogger<FamilyCreatedEventUpdatePolicies> _logger;
        private readonly IAuthzService _authz;

        // The Azure Functions v4 isolated worker uses STJ for CosmosDB trigger deserialization,
        // which cannot bind Family's parameterized constructor. Receive raw strings and deserialize
        // with Newtonsoft (same serializer the CosmosClient uses) instead.
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
        };

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
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<string> inputDocuments)
        {
            if (inputDocuments != null && inputDocuments.Count > 0)
            {
                _logger.LogInformation("Families updated " + inputDocuments.Count);
                var updatePermissionsCommand = new UpdateAuthorizationPoliciesCommand(_authz);

                foreach (var json in inputDocuments)
                {
                    var family = JsonConvert.DeserializeObject<Family>(json, _jsonSettings);
                    if (family is null) continue;
                    _logger.LogInformation("Updating policies for family " + family.Id);
                    await updatePermissionsCommand.Handle(family);
                }
            }
        }
    }
}
