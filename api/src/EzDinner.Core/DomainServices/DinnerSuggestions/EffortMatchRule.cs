namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public class EffortMatchRule : IScoringRule
    {
        private const double EffortMatchBoost = 6.0;
        private const double EffortMismatchPenalty = -6.0;

        public double Score(DishCandidateValueObject candidate, SuggestionContextValueObject context)
        {
            if (context.EffortPreference is null)
                return 0;

            if (candidate.EffortLevel is null)
                return 0;

            if (candidate.EffortLevel == context.EffortPreference)
                return EffortMatchBoost;

            return EffortMismatchPenalty;
        }
    }
}
