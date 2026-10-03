using EzDinner.Core.Aggregates.DishAggregate;
using NodaTime;

namespace EzDinner.Core.DomainServices.DishRecommendations;

public sealed class DishRecommendationCandidateValueObject
{
    public Guid DishId { get; }
    public string Name { get; }
    public double? Rating { get; }
    public IReadOnlyList<DishRole> Roles { get; }
    public bool IsUnclassified => Roles.Count == 0;
    public int WishVotes { get; }
    public bool IsWished { get; }
    public DishUsagePatternValueObject Usage { get; }
    public IReadOnlyList<LocalDate> AssignedDates { get; }

    public DishRecommendationCandidateValueObject(Guid dishId, string name, double? rating,
        IEnumerable<DishRole> roles, bool isWished, int wishVotes, DishUsagePatternValueObject usage,
        IEnumerable<LocalDate> assignedDates)
    {
        if (dishId == Guid.Empty) throw new ArgumentException("A canonical dish ID is required.", nameof(dishId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A dish name is required.", nameof(name));
        if (rating is < 0 or > 10) throw new ArgumentOutOfRangeException(nameof(rating));
        if (wishVotes < 0) throw new ArgumentOutOfRangeException(nameof(wishVotes));
        DishId = dishId;
        Name = name;
        Rating = rating;
        Roles = Array.AsReadOnly(roles.Distinct().ToArray());
        IsWished = isWished;
        WishVotes = wishVotes;
        Usage = usage;
        AssignedDates = Array.AsReadOnly(assignedDates.Distinct().Order().ToArray());
    }
}
