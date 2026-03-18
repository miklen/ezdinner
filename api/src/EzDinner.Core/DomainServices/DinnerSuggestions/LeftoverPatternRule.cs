namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public class LeftoverPatternRule : IScoringRule
    {
        private const double LeftoverFrequencyThreshold = 0.30;
        private const double LeftoverBonus = 15.0;

        public double Score(DishCandidateValueObject candidate, SuggestionContextValueObject context)
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
