namespace EzDinner.Query.Core.SuggestionQueries
{
    /// <summary>
    /// Scales the dish's average rating (0–10) into a score contribution.
    /// </summary>
    public class RatingScoringRule : IScoringRule
    {
        private const double RatingWeight = 5.0;
        private const double MaxRating = 10.0;

        public double Score(DishCandidate candidate, SuggestionContext context)
        {
            return (candidate.Rating / MaxRating) * RatingWeight;
        }
    }
}
