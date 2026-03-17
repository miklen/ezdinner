namespace EzDinner.Query.Core.SuggestionQueries
{
    public interface IScoringRule
    {
        double Score(DishCandidate candidate, SuggestionContext context);
    }
}
