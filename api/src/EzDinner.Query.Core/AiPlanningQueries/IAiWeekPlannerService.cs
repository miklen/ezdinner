using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EzDinner.Query.Core.AiPlanningQueries
{
    public interface IAiWeekPlannerService
    {
        /// <summary>
        /// Generates a dinner plan for unplanned days in the week using Claude Haiku.
        /// Already-planned days are detected from the live dinner records.
        /// Dishes in <paramref name="excludedDishIds"/> are omitted from the catalog.
        /// </summary>
        Task<IReadOnlyList<AiWeekPlanSuggestionResult>> PlanWeekAsync(
            Guid familyId,
            string weekStart,
            string? context,
            IReadOnlyList<string>? excludedDishIds = null,
            CancellationToken ct = default);
    }

    public class AiWeekPlanSuggestionResult
    {
        public string Date { get; set; } = string.Empty;
        public Guid DishId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
