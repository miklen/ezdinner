namespace EzDinner.Functions.Models.Command
{
    public class AiWeekPlanRequest
    {
        /// <summary>
        /// ISO date string for the Monday that starts the planning week (e.g. "2026-04-14").
        /// </summary>
        public string? WeekStart { get; set; }

        /// <summary>
        /// Optional freetext context from the planner (e.g. "busy Tuesday, guests Friday").
        /// </summary>
        public string? Context { get; set; }

        /// <summary>
        /// Dish IDs the user has explicitly skipped in a previous suggestion round.
        /// These are excluded from the catalog before the AI prompt is built.
        /// </summary>
        public string[]? ExcludedDishIds { get; set; }
    }
}
