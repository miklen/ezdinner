namespace EzDinner.Query.Core.SuggestionQueries
{
    /// <summary>
    /// Applies a bonus when a dish historically appears on consecutive days
    /// (≥ 30% consecutive-day usage ratio) and was used on the preceding day.
    /// </summary>
    public class LeftoverPatternRule : IScoringRule
    {
        private const double LeftoverFrequencyThreshold = 0.30;
        private const double LeftoverBonus = 15.0;

        public double Score(DishCandidate candidate, SuggestionContext context)
        {
            if (candidate.LeftoverFrequencyRatio >= LeftoverFrequencyThreshold
                && context.AdjacentDishIds.Contains(candidate.DishId))
            {
                return LeftoverBonus;
            }

            return 0.0;
        }
    }
}
