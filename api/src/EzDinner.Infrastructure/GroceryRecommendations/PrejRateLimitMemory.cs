using System.Collections.Concurrent;

namespace EzDinner.Infrastructure.GroceryRecommendations;

public sealed class PrejRateLimitMemory
{
    private readonly ConcurrentDictionary<string, DateTime> deadlines = new(StringComparer.Ordinal);

    public bool IsBlocked(string scope, DateTime now) => deadlines.TryGetValue(scope, out var until) && until > now;
    public void Extend(string scope, DateTime until) =>
        deadlines.AddOrUpdate(scope, until, (_, prior) => prior > until ? prior : until);
}
