namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public class OverdueScoringRule : IScoringRule
    {
        private const double OverdueWeight = 10.0;
        private const double DefaultTypicalFrequencyDays = 14.0;

        public double Score(DishCandidateValueObject candidate, SuggestionContextValueObject context)
        {
            var frequency = candidate.TypicalFrequencyDays > 0
                ? candidate.TypicalFrequencyDays
                : DefaultTypicalFrequencyDays;

            return (candidate.DaysSinceLast / frequency) * OverdueWeight;
        }
    }
}
