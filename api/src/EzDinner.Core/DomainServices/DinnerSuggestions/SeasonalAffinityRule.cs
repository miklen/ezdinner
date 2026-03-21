using EzDinner.Core.Aggregates.DishAggregate;

namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public class SeasonalAffinityRule : IExplainableScoringRule
    {
        private const double SeasonMatchBoost = 8.0;
        private const double SeasonMismatchPenalty = -8.0;

        public double Score(DishCandidateValueObject candidate, SuggestionContextValueObject context)
        {
            if (candidate.SeasonAffinity is null || candidate.SeasonAffinity == SeasonAffinity.AllYear)
                return 0;

            var currentSeason = DeriveSeasonFrom(context.TargetDate.Month);

            if (candidate.SeasonAffinity == currentSeason)
                return SeasonMatchBoost;

            return SeasonMismatchPenalty;
        }

        public string? Explain(DishCandidateValueObject candidate, SuggestionContextValueObject context)
        {
            if (candidate.SeasonAffinity is null || candidate.SeasonAffinity == SeasonAffinity.AllYear)
                return null;

            var currentSeason = DeriveSeasonFrom(context.TargetDate.Month);

            if (candidate.SeasonAffinity == currentSeason)
                return "Matches the season";

            return "Out of season";
        }

        private static SeasonAffinity DeriveSeasonFrom(int month) => month switch
        {
            12 or 1 or 2 => SeasonAffinity.Winter,
            3 or 4 or 5  => SeasonAffinity.Spring,
            6 or 7 or 8  => SeasonAffinity.Summer,
            _            => SeasonAffinity.Autumn,
        };
    }
}
