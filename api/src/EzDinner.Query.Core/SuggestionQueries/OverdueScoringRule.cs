namespace EzDinner.Query.Core.SuggestionQueries
{
    /// <summary>
    /// Scores a dish based on how overdue it is relative to its typical rotation frequency.
    /// A dish used less recently than its typical frequency scores higher.
    /// </summary>
    public class OverdueScoringRule : IScoringRule
    {
        private const double OverdueWeight = 10.0;
        private const double DefaultTypicalFrequencyDays = 14.0;

        public double Score(DishCandidate candidate, SuggestionContext context)
        {
            var frequency = candidate.TypicalFrequencyDays > 0
                ? candidate.TypicalFrequencyDays
                : DefaultTypicalFrequencyDays;

            return (candidate.DaysSinceLast / frequency) * OverdueWeight;
        }
    }
}
