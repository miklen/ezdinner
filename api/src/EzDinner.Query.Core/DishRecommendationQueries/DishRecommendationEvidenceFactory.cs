using System.Text.Json;

namespace EzDinner.Query.Core.DishRecommendationQueries;

public static class DishRecommendationEvidenceFactory
{
    public static string Serialize(DishRecommendationRequest request, DishRecommendationContext context)
        => JsonSerializer.Serialize(new
        {
            locale = request.Locale,
            turns = request.Turns,
            activeConstraints = request.Constraints,
            role = request.Role?.ToString(),
            nameFilter = request.NameFilter,
            targetDate = request.TargetDate?.ToString("yyyy-MM-dd", null),
            selectedMonday = request.SelectedMonday.ToString("yyyy-MM-dd", null),
            exclusions = request.ExcludedDishIds,
            dishes = context.Dishes.Select(dish => new
            {
                dish.DishId, dish.Name,
                roles = dish.Roles.Select(role => role.ToString()), dish.Sources,
                dish.Candidate.Rating, dish.Candidate.WishVotes, dish.Candidate.IsWished,
                lastServed = dish.Candidate.Usage.LastServed?.ToString("yyyy-MM-dd", null),
                dish.Candidate.Usage.ServingCount, dish.Candidate.Usage.TypicalSpacingDays,
                assignedDates = dish.Candidate.AssignedDates.Select(date => date.ToString("yyyy-MM-dd", null))
            }),
            dinners = context.Dinners.Select(dinner => new { date = dinner.Date.ToString("yyyy-MM-dd", null), dinner.DishIds, dinner.OptOutReason })
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
}
