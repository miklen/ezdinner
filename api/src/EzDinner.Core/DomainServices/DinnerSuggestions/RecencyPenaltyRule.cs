namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public class RecencyPenaltyRule : IScoringRule
    {
        private const int ZeroScoreThresholdDays = 3;
        private const int RecencyPenaltyDays = 7;
        private const double ZeroScorePenalty = -1000.0;
        private const double RecentUsePenalty = -20.0;

        public double Score(DishCandidateValueObject candidate, SuggestionContextValueObject context)
        {
            if (candidate.DaysSinceLast <= ZeroScoreThresholdDays)
                return ZeroScorePenalty;

            if (candidate.DaysSinceLast <= RecencyPenaltyDays)
                return RecentUsePenalty;

            return 0.0;
        }
    }
}
