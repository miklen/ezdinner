namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public class RatingScoringRule : IScoringRule
    {
        private const double RatingWeight = 5.0;
        private const double MaxRating = 10.0;

        public double Score(DishCandidateValueObject candidate, SuggestionContextValueObject context)
        {
            return (candidate.Rating / MaxRating) * RatingWeight;
        }
    }
}
