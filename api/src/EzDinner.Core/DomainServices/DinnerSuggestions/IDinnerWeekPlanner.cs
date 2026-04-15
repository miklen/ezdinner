using NodaTime;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public interface IDinnerWeekPlanner
    {
        Task<IReadOnlyList<DaySuggestion>> PlanWeekAsync(
            FamilySuggestionContext context,
            LocalDate weekStart,
            IReadOnlyList<LocalDate> alreadyPlanned,
            string? userContext = null);
    }
}
