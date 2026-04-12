using NodaTime;

namespace EzDinner.Core.DomainServices.DinnerSuggestions
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
