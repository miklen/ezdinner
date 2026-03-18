using EzDinner.Core.DomainServices.DinnerSuggestions;
using NodaTime;

namespace EzDinner.Query.Core.SuggestionQueries
{
    public class DaySuggestion
    {
        public LocalDate Date { get; }
        public DishScoreValueObject? Suggestion { get; }

        public DaySuggestion(LocalDate date, DishScoreValueObject? suggestion)
        {
            Date = date;
            Suggestion = suggestion;
        }
    }
}
