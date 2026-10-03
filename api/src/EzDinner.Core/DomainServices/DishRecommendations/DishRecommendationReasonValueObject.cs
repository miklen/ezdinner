namespace EzDinner.Core.DomainServices.DishRecommendations;

public enum DishRecommendationReasonKind
{
    Rating,
    Wish,
    NeverUsed,
    ObservedRotation,
    ForgottenFavourite,
    FormerRegular,
    RecentlyServed
}

public sealed record DishRecommendationReasonValueObject(DishRecommendationReasonKind Kind, double? Value);

public sealed record DishRecommendationScoreValueObject(
    DishRecommendationCandidateValueObject Candidate,
    double Score,
    IReadOnlyList<DishRecommendationReasonValueObject> Reasons);
