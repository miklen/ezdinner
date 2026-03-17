namespace EzDinner.Query.Core.SuggestionQueries
{
    /// <summary>
    /// Penalises dishes used too recently.
    /// Used within the last 3 days: large negative override (zero-score threshold).
    /// Used within the last 7 days: significant penalty.
    /// </summary>
    public class RecencyPenaltyRule : IScoringRule
    {
        private const int ZeroScoreThresholdDays = 3;
        private const int RecencyPenaltyDays = 7;
        private const double ZeroScorePenalty = -1000.0;
        private const double RecentUsePenalty = -20.0;

        public double Score(DishCandidate candidate, SuggestionContext context)
        {
            if (candidate.DaysSinceLast <= ZeroScoreThresholdDays)
                return ZeroScorePenalty;

            if (candidate.DaysSinceLast <= RecencyPenaltyDays)
                return RecentUsePenalty;

            return 0.0;
        }
    }
}
