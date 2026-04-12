using NodaTime;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EzDinner.Query.Core.SuggestionQueries
{
    public interface ILlmWeekPlanClient
    {
        Task<IReadOnlyList<LlmDayPlanResult>> PlanWeekAsync(
            string dishCatalogMarkdown,
            LocalDate weekStart,
            IReadOnlyList<LocalDate> unplannedDates,
            string? userContext,
            CancellationToken ct = default);
    }

    public class LlmDayPlanResult
    {
        public string Date { get; set; } = string.Empty;
        public Guid DishId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
