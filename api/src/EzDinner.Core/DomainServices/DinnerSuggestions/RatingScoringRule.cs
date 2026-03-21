namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public class RatingScoringRule : IExplainableScoringRule
    {
        private const double RatingWeight = 5.0;
        private const double MaxRating = 10.0;
        private const double NotableRatingThreshold = 6.0;

        public double Score(DishCandidateValueObject candidate, SuggestionContextValueObject context)
        {
            return (candidate.Rating / MaxRating) * RatingWeight;
        }

        public string? Explain(DishCandidateValueObject candidate, SuggestionContextValueObject context)
        {
            if (candidate.Rating < NotableRatingThreshold)
                return null;

            return $"Rated {(int)System.Math.Round(candidate.Rating)}/10";
        }
    }
}
