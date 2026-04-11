namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public class WishlistBoostRule : IExplainableScoringRule
    {
        private const double BaseBoostPerVote = 0.3;

        public double Score(DishCandidateValueObject candidate, SuggestionContextValueObject context)
        {
            if (context.WishedDishIds.TryGetValue(candidate.DishId, out var voteCount))
                return voteCount * BaseBoostPerVote;

            return 0;
        }

        public string? Explain(DishCandidateValueObject candidate, SuggestionContextValueObject context)
        {
            if (context.WishedDishIds.TryGetValue(candidate.DishId, out var voteCount))
                return $"Wished for by the family ({voteCount} votes)";

            return null;
        }
    }
}
