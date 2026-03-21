namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public class EffortMatchRule : IExplainableScoringRule
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

        public string? Explain(DishCandidateValueObject candidate, SuggestionContextValueObject context)
        {
            if (context.EffortPreference is null || candidate.EffortLevel is null)
                return null;

            if (candidate.EffortLevel == context.EffortPreference)
                return "Matches your effort preference";

            return null;
        }
    }
}
