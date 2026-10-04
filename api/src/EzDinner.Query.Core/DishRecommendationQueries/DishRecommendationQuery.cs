using EzDinner.Core.DomainServices.DishRecommendations;
using EzDinner.Core.Aggregates.DishAggregate;

namespace EzDinner.Query.Core.DishRecommendationQueries;

public sealed class DishRecommendationQuery(DishRecommendationContextAssembler assembler,
    DishRecommendationService automatic, ResurfacingRule resurfacing,
    IDishRecommendationLlmClient provider, DishRecommendationLimits limits)
{
    public async Task<DishRecommendationResult> RecommendAsync(Guid familyId, DishRecommendationRequest request, CancellationToken cancellationToken)
    {
        limits.Validate(request);
        var context = await assembler.AssembleAsync(familyId, request.SelectedMonday, cancellationToken);
        var planningDate = request.TargetDate ?? request.SelectedMonday;
        var constraints = request.Constraints.ToArray();
        var available = context.Dishes.Where(dish => !request.ExcludedDishIds.Contains(dish.DishId) &&
            dish.Name.Contains(request.NameFilter.Trim(), StringComparison.OrdinalIgnoreCase) &&
            (request.Role is null || dish.Roles.Contains(request.Role.Value) ||
                (request.Role == DishRole.Main && dish.Candidate.IsUnclassified))).ToArray();
        if (request.Mode == RecommendationMode.Automatic || (request.Mode == RecommendationMode.More && request.Turns.Count == 0 && constraints.Length == 0))
        {
            var ranked = automatic.Recommend(available.Select(dish => dish.Candidate), planningDate,
                request.ExcludedDishIds, limits.MaximumResults, request.Role);
            var matches = ranked.Select(result => new RecommendedDish(result.Candidate.DishId, result.Candidate.Name,
                result.Candidate, result.Reasons, [], [])).ToArray();
            return new(matches.Length > 0 ? RecommendationOutcome.Matches : EmptyOutcome(request), matches, constraints, string.Join("; ", constraints), null);
        }
        if (available.Length == 0) return new(EmptyOutcome(request), [], constraints, string.Join("; ", constraints), null);
        var semanticContext = context with { Dishes = available };
        var response = await RecommendCatalogAsync(request, semanticContext, cancellationToken);
        var canonical = available.ToDictionary(dish => dish.DishId);
        var results = response.Matches.OrderByDescending(match => match.Suitability)
            .ThenByDescending(match => resurfacing.Score(canonical[match.DishId].Candidate, planningDate).Score)
            .ThenBy(match => match.DishId).Take(limits.MaximumResults)
            .Select(match => BuildMatch(match, canonical[match.DishId], planningDate)).ToArray();
        var outcome = response.Outcome == RecommendationOutcome.NoMatch ? EmptyOutcome(request) : response.Outcome;
        return new(outcome, results, constraints, response.ContextSummary, response.Message);
    }

    private async Task<RecommendationProviderResult> RecommendCatalogAsync(DishRecommendationRequest request,
        DishRecommendationContext context, CancellationToken cancellationToken)
    {
        var batches = DishRecommendationEvidenceFactory.Batch(request, context, limits).ToArray();
        using var concurrency = new SemaphoreSlim(3);
        var responses = await Task.WhenAll(batches.Select(async batch =>
        {
            await concurrency.WaitAsync(cancellationToken);
            try { return await RecommendBatchAsync(request, batch, cancellationToken); }
            finally { concurrency.Release(); }
        }));
        if (responses.Length == 1) return responses[0];
        var matches = responses.SelectMany(response => response.Matches).ToArray();
        if (matches.Length == 0)
            return responses.FirstOrDefault(response => response.Outcome == RecommendationOutcome.NeedsClarification) ?? responses[0];
        var finalists = matches.Select(match => match.DishId).ToHashSet();
        var finalistContext = context with { Dishes = context.Dishes.Where(dish => finalists.Contains(dish.DishId)).ToArray() };
        if (DishRecommendationEvidenceFactory.Batch(request, finalistContext, limits).Take(2).Count() == 1)
            return await RecommendBatchAsync(request, finalistContext, cancellationToken);
        return new(RecommendationOutcome.Matches, matches, string.Join("; ", request.Constraints.Concat(request.Turns)), null);
    }

    private async Task<RecommendationProviderResult> RecommendBatchAsync(DishRecommendationRequest request,
        DishRecommendationContext context, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(limits.ProviderTimeoutSeconds));
        RecommendationProviderResult response;
        try { response = await provider.RecommendAsync(request, context, timeout.Token).WaitAsync(timeout.Token); }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        { throw new DishRecommendationProviderException("PROVIDER_TIMEOUT", exception); }
        ValidateResponse(response, context, request);
        return response;
    }

    private RecommendedDish BuildMatch(RecommendationProviderMatch match, DishRecommendationEvidence evidence, NodaTime.LocalDate planningDate)
        => new(evidence.DishId, evidence.Name, evidence.Candidate, resurfacing.Score(evidence.Candidate, planningDate).Reasons, match.Explanations, match.Limitations);

    private static RecommendationOutcome EmptyOutcome(DishRecommendationRequest request)
        => request.Mode == RecommendationMode.More ? RecommendationOutcome.Exhausted : RecommendationOutcome.NoMatch;

    private static void ValidateResponse(RecommendationProviderResult response, DishRecommendationContext context, DishRecommendationRequest request)
    {
        if (!Enum.IsDefined(response.Outcome) || response.ContextSummary is null || response.ContextSummary.Length > 4_000 ||
            response.Matches is null || response.Matches.Count > 20 ||
            response.Matches.Any(match => match is null) ||
            (response.Outcome == RecommendationOutcome.Matches) != (response.Matches.Count > 0) ||
            response.Matches.Select(match => match.DishId).Distinct().Count() != response.Matches.Count)
            throw new DishRecommendationProviderException("MALFORMED_PROVIDER_RESPONSE");
        var canonical = context.Dishes.ToDictionary(dish => dish.DishId);
        foreach (var match in response.Matches)
        {
            if (!canonical.TryGetValue(match.DishId, out var dish) || request.ExcludedDishIds.Contains(match.DishId) ||
                !double.IsFinite(match.Suitability) || match.Suitability is < 0 or > 1 ||
                match.Explanations is null || match.Explanations.Count is < 1 or > 12 || match.Limitations is null ||
                match.Limitations.Count > 12 || match.Limitations.Any(limitation => string.IsNullOrWhiteSpace(limitation) || limitation.Length > 2_000))
                throw new DishRecommendationProviderException("UNSUPPORTED_PROVIDER_MATCH");
            if (request.Role is not null && !dish.Roles.Contains(request.Role.Value) && !(request.Role == DishRole.Main && dish.Candidate.IsUnclassified))
                throw new DishRecommendationProviderException("INVALID_PROVIDER_ROLE");
            var references = dish.Sources.Select(source => source.Reference).ToHashSet(StringComparer.Ordinal);
            if (match.Explanations.Any(reason => reason is null || !Enum.IsDefined(reason.Kind) || string.IsNullOrWhiteSpace(reason.Text) ||
                reason.Text.Length > 2_000 || reason.SourceReferences is null ||
                (reason.Kind == RecommendationEvidenceKind.Fact && reason.SourceReferences.Count == 0) ||
                reason.SourceReferences.Any(reference => !references.Contains(reference))))
                throw new DishRecommendationProviderException("UNSUPPORTED_PROVIDER_EVIDENCE");
        }
    }
}
