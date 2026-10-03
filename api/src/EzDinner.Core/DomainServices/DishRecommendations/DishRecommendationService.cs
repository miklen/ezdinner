using EzDinner.Core.Aggregates.DishAggregate;
using NodaTime;

namespace EzDinner.Core.DomainServices.DishRecommendations;

public sealed class DishRecommendationService(ResurfacingRule resurfacing)
{
    public IReadOnlyList<DishRecommendationScoreValueObject> Recommend(
        IEnumerable<DishRecommendationCandidateValueObject> candidates, LocalDate planningDate,
        IReadOnlySet<Guid> exclusions, int limit = 6, DishRole? role = DishRole.Main)
    {
        if (limit is < 1 or > 20) throw new ArgumentOutOfRangeException(nameof(limit));
        return candidates.Where(candidate => !exclusions.Contains(candidate.DishId))
            .Where(candidate => role is null || candidate.Roles.Contains(role.Value) || (role == DishRole.Main && candidate.IsUnclassified))
            .Select(candidate => resurfacing.Score(candidate, planningDate))
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Candidate.Name, StringComparer.Ordinal)
            .ThenBy(result => result.Candidate.DishId)
            .Take(limit).ToArray();
    }
}
