using EzDinner.Core.Aggregates.UserAggregate;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace EzDinner.Infrastructure
{
    public class UserRepository : IUserRepository
    {
        private readonly GraphServiceClient _graphClient;
        private readonly string _tenantDomain;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(GraphServiceClient graphClient, string tenantDomain, ILogger<UserRepository> logger)
        {
            _graphClient = graphClient;
            _tenantDomain = tenantDomain;
            _logger = logger;
        }

        public async Task<Core.Aggregates.UserAggregate.User> GetUser(Guid id)
        {
            var user = await _graphClient.Users[id.ToString()].GetAsync();
            // TODO use Automapper
            return new Core.Aggregates.UserAggregate.User()
            {
                Id = id,
                GivenName = user!.GivenName,
                FamilyName = user.Surname
            };
        }

        public async Task<Core.Aggregates.UserAggregate.User?> GetUser(string email)
        {
            _logger.LogInformation("GetUser: looking up email={Email}, tenantDomain={TenantDomain}", email, _tenantDomain);

            var userPage = await _graphClient.Users.GetAsync(config =>
                config.QueryParameters.Filter = $"mail eq '{email}'");
            var user = userPage?.Value?.FirstOrDefault();
            _logger.LogInformation("GetUser: mail filter returned {Count} result(s)", userPage?.Value?.Count ?? 0);

            if (user is null)
            {
                var filter = $"identities/any(i:i/issuerAssignedId eq '{email}' and i/issuer eq '{_tenantDomain}')";
                _logger.LogInformation("GetUser: trying identities filter: {Filter}", filter);
                var identityPage = await _graphClient.Users.GetAsync(config =>
                {
                    config.QueryParameters.Filter = filter;
                    config.QueryParameters.Count = true;
                    config.Headers.Add("ConsistencyLevel", "eventual");
                });
                _logger.LogInformation("GetUser: identities filter returned {Count} result(s), OData count={ODataCount}",
                    identityPage?.Value?.Count ?? 0, identityPage?.OdataCount);
                user = identityPage?.Value?.FirstOrDefault();
            }

            if (user is null)
            {
                _logger.LogWarning("GetUser: no user found for email={Email}", email);
                return null;
            }
            return new Core.Aggregates.UserAggregate.User()
            {
                Id = Guid.Parse(user.Id!),
                GivenName = user.GivenName,
                FamilyName = user.Surname
            };
        }
    }
}
