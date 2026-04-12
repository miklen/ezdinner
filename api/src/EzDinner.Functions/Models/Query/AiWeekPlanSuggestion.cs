namespace EzDinner.Functions.Models.Query
{
    public class AiWeekPlanSuggestion
    {
        public string Date { get; set; } = string.Empty;
        public string DishId { get; set; } = string.Empty;
        public string DishName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
