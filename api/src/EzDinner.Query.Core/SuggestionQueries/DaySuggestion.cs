using NodaTime;

namespace EzDinner.Query.Core.SuggestionQueries
{
    public class DaySuggestion
    {
        public LocalDate Date { get; }
        public DishScore? Suggestion { get; }

        public DaySuggestion(LocalDate date, DishScore? suggestion)
        {
            Date = date;
            Suggestion = suggestion;
        }
    }
}
