using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.WishlistAggregate;
using EzDinner.Core.DomainServices.DishRecommendations;
using NodaTime;

namespace EzDinner.Query.Core.DishRecommendationQueries;

public sealed class DishRecommendationContextAssembler(IDishRepository dishes,
    IDinnerRepository dinners, IWishlistRepository wishes, IClock clock)
{
    public async Task<DishRecommendationContext> AssembleAsync(Guid familyId, LocalDate monday, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dishTask = dishes.GetDishesAsync(familyId, includeArchived: false);
        var wishTask = wishes.GetActiveAsync(familyId);
        await Task.WhenAll(dishTask, wishTask).WaitAsync(cancellationToken);
        var canonical = dishTask.Result.Where(dish => dish.FamilyId == familyId && !dish.Deleted && !dish.IsArchived).ToArray();
        var history = new List<Dinner>();
        await foreach (var dinner in dinners.GetAsync(familyId, LocalDate.MinIsoValue, LocalDate.MaxIsoValue).WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (dinner.FamilyId == familyId) history.Add(dinner);
        }
        var now = clock.GetCurrentInstant();
        var today = now.InUtc().Date;
        var votes = wishTask.Result.Where(wish => wish.FamilyId == familyId && !wish.IsExpired(now))
            .GroupBy(wish => wish.DishId).ToDictionary(group => group.Key, group => group.Sum(wish => wish.Votes.Count));
        var candidates = DishRecommendationCandidateFactory.Create(familyId, canonical, history, votes, today, monday)
            .ToDictionary(candidate => candidate.DishId);
        var evidence = canonical.Select(dish => BuildEvidence(dish, candidates[dish.Id])).ToArray();
        var window = history.Where(dinner => dinner.Date >= monday.PlusDays(-2) && dinner.Date <= monday.PlusDays(6))
            .Select(dinner => new RecommendationDinnerContext(dinner.Date, dinner.Menu.Select(item => item.DishId).ToArray(), dinner.OptOut?.Reason)).ToArray();
        return new(evidence, window);
    }

    private static DishRecommendationEvidence BuildEvidence(Dish dish, DishRecommendationCandidateValueObject candidate)
    {
        var sources = new List<DishRecommendationSource> { new($"{dish.Id}:title", dish.Name) };
        if (!string.IsNullOrWhiteSpace(dish.Notes)) sources.Add(new($"{dish.Id}:notes", dish.Notes));
        if (dish.RecipeSnapshot is not null) sources.Add(new($"{dish.Id}:recipe", dish.RecipeSnapshot.Content));
        sources.Add(new($"{dish.Id}:metadata", System.Text.Json.JsonSerializer.Serialize(new
        {
            roles = dish.Metadata.Roles.Select(role => role.ToString()),
            effort = dish.Metadata.EffortLevel?.ToString(),
            season = dish.Metadata.SeasonAffinity?.ToString(),
            cuisine = dish.Metadata.Cuisine
        })));
        return new(dish.Id, dish.Name, candidate.Roles, sources.AsReadOnly(), candidate);
    }
}
