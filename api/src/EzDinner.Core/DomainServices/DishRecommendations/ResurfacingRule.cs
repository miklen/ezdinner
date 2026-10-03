using NodaTime;

namespace EzDinner.Core.DomainServices.DishRecommendations;

public sealed class ResurfacingRule
{
    public DishRecommendationScoreValueObject Score(DishRecommendationCandidateValueObject candidate, LocalDate planningDate)
    {
        var reasons = new List<DishRecommendationReasonValueObject>();
        var score = RatingScore(candidate, reasons) + WishScore(candidate, reasons);
        var age = candidate.Usage.DaysSinceLastServing(planningDate);
        if (age is null)
        {
            reasons.Add(new(DishRecommendationReasonKind.NeverUsed, null));
            return new(candidate, score, reasons.AsReadOnly());
        }
        score += RotationScore(candidate, age.Value, reasons);
        score += FormerFrequencyScore(candidate, age.Value, reasons);
        if (age < 7)
        {
            score -= 20 * Math.Clamp((7 - age.Value) / 7d, 0, 1);
            reasons.Add(new(DishRecommendationReasonKind.RecentlyServed, age));
        }
        return new(candidate, score, reasons.AsReadOnly());
    }

    private static double RatingScore(DishRecommendationCandidateValueObject candidate, List<DishRecommendationReasonValueObject> reasons)
    {
        if (candidate.Rating is not double rating || rating == 5) return 0;
        reasons.Add(new(DishRecommendationReasonKind.Rating, rating));
        return (rating - 5) * 4;
    }

    private static double WishScore(DishRecommendationCandidateValueObject candidate, List<DishRecommendationReasonValueObject> reasons)
    {
        if (!candidate.IsWished) return 0;
        reasons.Add(new(DishRecommendationReasonKind.Wish, candidate.WishVotes));
        return 12 + Math.Min(candidate.WishVotes, 5) * 2;
    }

    private static double RotationScore(DishRecommendationCandidateValueObject candidate, int age, List<DishRecommendationReasonValueObject> reasons)
    {
        if (candidate.Usage.TypicalSpacingDays is not double spacing) return 0;
        var ratio = age / spacing;
        if (ratio < 0.8) return 0;
        reasons.Add(new(DishRecommendationReasonKind.ObservedRotation, spacing));
        if (ratio >= 1.5 && candidate.Rating >= 8)
            reasons.Add(new(DishRecommendationReasonKind.ForgottenFavourite, age));
        return Math.Clamp((ratio - 0.8) * 15, 0, 18);
    }

    private static double FormerFrequencyScore(DishRecommendationCandidateValueObject candidate, int age, List<DishRecommendationReasonValueObject> reasons)
    {
        if (candidate.Usage.OccurrenceStarts.Count < 5 || candidate.Usage.TypicalSpacingDays is not double spacing || age < spacing * 1.5) return 0;
        reasons.Add(new(DishRecommendationReasonKind.FormerRegular, candidate.Usage.OccurrenceStarts.Count));
        return Math.Min(candidate.Usage.OccurrenceStarts.Count, 10);
    }
}
