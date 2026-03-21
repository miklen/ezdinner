namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public class OverdueScoringRule : IExplainableScoringRule
    {
        private const double OverdueWeight = 3.0;
        private const double DefaultTypicalFrequencyDays = 14.0;

        public double Score(DishCandidateValueObject candidate, SuggestionContextValueObject context)
        {
            var frequency = candidate.TypicalFrequencyDays > 0
                ? candidate.TypicalFrequencyDays
                : DefaultTypicalFrequencyDays;

            return (candidate.DaysSinceLast / frequency) * OverdueWeight;
        }

        public string? Explain(DishCandidateValueObject candidate, SuggestionContextValueObject context)
        {
            var frequency = candidate.TypicalFrequencyDays > 0
                ? candidate.TypicalFrequencyDays
                : DefaultTypicalFrequencyDays;

            if (candidate.DaysSinceLast / frequency < 1.0)
                return null;

            return $"Not had in {candidate.DaysSinceLast} days";
        }
    }
}
