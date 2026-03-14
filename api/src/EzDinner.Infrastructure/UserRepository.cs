using EzDinner.Core.Aggregates.UserAggregate;
using Microsoft.Graph;

namespace EzDinner.Infrastructure
{
    public class UserRepository : IUserRepository
    {
        private readonly GraphServiceClient _graphClient;

        public UserRepository(GraphServiceClient graphClient)
        {
            _graphClient = graphClient;
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
            var userPage = await _graphClient.Users.GetAsync(config =>
                config.QueryParameters.Filter = $"mail eq '{email}'");
            var user = userPage?.Value?.FirstOrDefault();
            if (user is null) return null;
            return new Core.Aggregates.UserAggregate.User()
            {
                Id = Guid.Parse(user.Id!),
                GivenName = user.GivenName,
                FamilyName = user.Surname
            };
        }
    }
}
