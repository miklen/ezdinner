namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public interface IScoringRule
    {
        double Score(DishCandidateValueObject candidate, SuggestionContextValueObject context);
    }
}
