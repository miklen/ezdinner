using System.Text.Json;

namespace EzDinner.Query.Core.DishRecommendationQueries;

public static class DishRecommendationEvidenceFactory
{
    public static IEnumerable<DishRecommendationContext> Batch(DishRecommendationRequest request,
        DishRecommendationContext context, DishRecommendationLimits limits)
    {
        var emptyLength = Serialize(request, context with { Dishes = [] }).Length;
        var batch = new List<DishRecommendationEvidence>();
        var batchLength = emptyLength;
        foreach (var dish in context.Dishes)
        {
            var dishLength = Serialize(request, context with { Dishes = [dish] }).Length - emptyLength;
            if (batch.Count > 0 && (batch.Count >= limits.MaximumCandidates ||
                (long)batchLength + dishLength + 1 > limits.MaximumEvidenceCharacters))
            {
                yield return context with { Dishes = batch.ToArray() };
                batch.Clear();
                batchLength = emptyLength;
            }
            batchLength += dishLength + (batch.Count > 0 ? 1 : 0);
            batch.Add(dish);
        }
        if (batch.Count > 0) yield return context with { Dishes = batch.ToArray() };
    }

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
