using System.Collections.Concurrent;
using System.Text.Json;
using EzDinner.Core.DomainServices.DishRecommendations;
using EzDinner.Infrastructure.GroceryRecommendations;
using EzDinner.Query.Core.DishRecommendationQueries;
using Microsoft.Extensions.Logging;

namespace EzDinner.Infrastructure.DishRecommendations;

public sealed class CachedDishIngredientProfileReader(IGroceryRecommendationCache cache,
    DishIngredientProfileInference inference, ILogger<CachedDishIngredientProfileReader>? logger = null) : IDishIngredientProfileReader
{
    private readonly SemaphoreSlim inferenceSlots = new(3);
    public const int SuccessTtlSeconds = 30 * 24 * 60 * 60;
    public const int FailureTtlSeconds = 15 * 60;

    public async Task<DishIngredientProfilesResult> ReadAsync(Guid familyId,
        IReadOnlyList<DishRecommendationEvidence> dishes, CancellationToken cancellationToken)
    {
        var profiles = new ConcurrentDictionary<Guid, IReadOnlyList<DishIngredientMatchValueObject>>();
        var misses = new List<DishRecommendationEvidence>();
        var scope = $"family:{familyId}";
        foreach (var dish in dishes)
        {
            if (cancellationToken.IsCancellationRequested) break;
            try
            {
                var cached = await cache.ReadProfileAsync(scope, DishIngredientProfileInference.CacheKey(familyId, dish), cancellationToken)
                    .WaitAsync(cancellationToken);
                if (cached is null) { misses.Add(dish); continue; }
                var stored = JsonSerializer.Deserialize<StoredProfile>(cached) ?? throw new JsonException("Expected cached profile");
                if (stored.Ingredients is not null) profiles[dish.DishId] = stored.Ingredients.Select(Restore).ToArray();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (Exception exception) when (exception is JsonException or ArgumentException)
            {
                misses.Add(dish);
            }
            catch (Exception exception)
            {
                logger?.LogWarning(exception, "Ingredient profile cache read unavailable");
                break;
            }
        }
        await Task.WhenAll(Batch(misses).Select(batch => InferBatchAsync(familyId, scope, batch, profiles, cancellationToken)));
        if (profiles.Count != dishes.Count)
            logger?.LogInformation("Ingredient profiles incomplete: {CompletedCount}/{DishCount}; cache misses {CacheMissCount}; cancellation requested {Cancelled}",
                profiles.Count, dishes.Count, misses.Count, cancellationToken.IsCancellationRequested);
        return new(profiles.ToDictionary(profile => profile.Key, profile => profile.Value), profiles.Count != dishes.Count);
    }

    private async Task InferBatchAsync(Guid familyId, string scope, IReadOnlyList<DishRecommendationEvidence> batch,
        ConcurrentDictionary<Guid, IReadOnlyList<DishIngredientMatchValueObject>> profiles, CancellationToken cancellationToken)
    {
        var entered = false;
        try
        {
            await inferenceSlots.WaitAsync(cancellationToken);
            entered = true;
            cancellationToken.ThrowIfCancellationRequested();
            var inferred = await inference.InferAsync(batch, cancellationToken).WaitAsync(cancellationToken);
            foreach (var dish in batch)
            {
                if (!inferred.TryGetValue(dish.DishId, out var ingredients))
                {
                    logger?.LogWarning("Ingredient profile rejected by evidence validation");
                    await PublishAsync(scope, DishIngredientProfileInference.CacheKey(familyId, dish), new StoredProfile(null),
                        FailureTtlSeconds, cancellationToken);
                    continue;
                }
                profiles[dish.DishId] = ingredients;
                var stored = new StoredProfile(ingredients.Select(Store).ToArray());
                await PublishAsync(scope, DishIngredientProfileInference.CacheKey(familyId, dish), stored,
                    SuccessTtlSeconds, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger?.LogInformation("Ingredient profile inference cancelled for {DishCount} dishes", batch.Count);
        }
        catch (DishRecommendationProviderException exception)
        {
            logger?.LogWarning("Ingredient profile inference failed for {DishCount} dishes: {FailureCode}", batch.Count, exception.Message);
            foreach (var dish in batch)
            {
                if (cancellationToken.IsCancellationRequested) break;
                await PublishAsync(scope, DishIngredientProfileInference.CacheKey(familyId, dish), new StoredProfile(null),
                    FailureTtlSeconds, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception, "Ingredient profile batch unavailable");
        }
        finally { if (entered) inferenceSlots.Release(); }
    }

    private async Task PublishAsync(string scope, string key, StoredProfile profile, int ttlSeconds, CancellationToken cancellationToken)
    {
        try
        {
            await cache.WriteProfileAsync(scope, key, JsonSerializer.Serialize(profile), ttlSeconds, cancellationToken)
                .WaitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception, "Ingredient profile cache publication unavailable");
        }
    }

    public static IEnumerable<IReadOnlyList<DishRecommendationEvidence>> Batch(IReadOnlyList<DishRecommendationEvidence> dishes)
    {
        var batch = new List<DishRecommendationEvidence>();
        foreach (var dish in dishes)
        {
            var next = batch.Append(dish).ToArray();
            if (batch.Count > 0 && (next.Length > 10 || DishIngredientProfileInference.SerializeEvidence(next).Length > 40_000))
            {
                yield return batch.ToArray();
                batch.Clear();
            }
            batch.Add(dish);
        }
        if (batch.Count > 0) yield return batch.ToArray();
    }

    private static StoredIngredient Store(DishIngredientMatchValueObject ingredient) => new(ingredient.Key,
        ingredient.Importance, ingredient.Confidence, ingredient.EvidenceKind, ingredient.SourceReferences,
        ingredient.SupportingExcerpt, ingredient.Role);
    private static DishIngredientMatchValueObject Restore(StoredIngredient ingredient) => new(ingredient.Key,
        ingredient.Importance, ingredient.Confidence, ingredient.EvidenceKind, ingredient.SourceReferences,
        ingredient.SupportingExcerpt, ingredient.Role);
    private sealed record StoredProfile(IReadOnlyList<StoredIngredient>? Ingredients);
    private sealed record StoredIngredient(string Key, double Importance, double Confidence, IngredientEvidenceKind EvidenceKind,
        IReadOnlyList<string> SourceReferences, string? SupportingExcerpt, IngredientRole Role);
}
