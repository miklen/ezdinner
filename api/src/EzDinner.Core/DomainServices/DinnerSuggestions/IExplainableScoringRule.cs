namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public interface IExplainableScoringRule : IScoringRule
    {
        string? Explain(DishCandidateValueObject candidate, SuggestionContextValueObject context);
    }
}
