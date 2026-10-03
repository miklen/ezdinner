using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.DomainServices.DishRecommendations;
using NodaTime;

namespace EzDinner.Query.Core.DishRecommendationQueries;

public enum RecommendationMode { Automatic, Request, More }
public enum RecommendationOutcome { Matches, NoMatch, Exhausted, NeedsClarification }
public enum RecommendationEvidenceKind { Fact, Inference }

public sealed record DishRecommendationRequest(LocalDate SelectedMonday, LocalDate? TargetDate,
    RecommendationMode Mode, string Locale, IReadOnlyList<string> Turns,
    IReadOnlyList<string> Constraints, IReadOnlySet<Guid> ExcludedDishIds, DishRole? Role = DishRole.Main, string NameFilter = "");

public sealed record DishRecommendationSource(string Reference, string Text);
public sealed record DishRecommendationEvidence(Guid DishId, string Name,
    IReadOnlyList<DishRole> Roles, IReadOnlyList<DishRecommendationSource> Sources,
    DishRecommendationCandidateValueObject Candidate);
public sealed record RecommendationDinnerContext(LocalDate Date, IReadOnlyList<Guid> DishIds, string? OptOutReason);
public sealed record DishRecommendationContext(IReadOnlyList<DishRecommendationEvidence> Dishes,
    IReadOnlyList<RecommendationDinnerContext> Dinners);

public sealed record RecommendationExplanation(string Text, RecommendationEvidenceKind Kind,
    IReadOnlyList<string> SourceReferences);
public sealed record RecommendedDish(Guid DishId, string Name,
    DishRecommendationCandidateValueObject Signals,
    IReadOnlyList<DishRecommendationReasonValueObject> HistoricalReasons,
    IReadOnlyList<RecommendationExplanation> Explanations, IReadOnlyList<string> Limitations);
public sealed record DishRecommendationResult(RecommendationOutcome Outcome,
    IReadOnlyList<RecommendedDish> Dishes, IReadOnlyList<string> ActiveConstraints,
    string ContextSummary, string? Message);

public sealed record RecommendationProviderMatch(Guid DishId, double Suitability,
    IReadOnlyList<RecommendationExplanation> Explanations, IReadOnlyList<string> Limitations);
public sealed record RecommendationProviderResult(RecommendationOutcome Outcome,
    IReadOnlyList<RecommendationProviderMatch> Matches, string ContextSummary, string? Message);

public interface IDishRecommendationLlmClient
{
    Task<RecommendationProviderResult> RecommendAsync(DishRecommendationRequest request,
        DishRecommendationContext context, CancellationToken cancellationToken);
}

public sealed class DishRecommendationProviderException(string code, Exception? inner = null) : Exception(code, inner);

public sealed class DishRecommendationLimits
{
    public int MaximumTurns { get; init; } = 12;
    public int MaximumConstraints { get; init; } = 12;
    public int MaximumRequestCharacters { get; init; } = 4_000;
    public int MaximumExclusions { get; init; } = 1_000;
    public int MaximumEvidenceCharacters { get; init; } = 100_000;
    public int MaximumCandidates { get; init; } = 300;
    public int MaximumResults { get; init; } = 6;
    public int ProviderTimeoutSeconds { get; init; } = 30;

    public void Validate(DishRecommendationRequest request)
    {
        if (request.SelectedMonday.DayOfWeek != IsoDayOfWeek.Monday ||
            request.SelectedMonday < new LocalDate(1900, 1, 3) || request.SelectedMonday > new LocalDate(9998, 12, 28))
            throw new ArgumentException("INVALID_SELECTED_MONDAY");
        if (request.TargetDate is LocalDate target && (target < request.SelectedMonday.PlusDays(-2) || target > request.SelectedMonday.PlusDays(6)))
            throw new ArgumentException("TARGET_OUTSIDE_WINDOW");
        if (request.Locale is not ("en" or "da")) throw new ArgumentException("INVALID_LOCALE");
        if (!Enum.IsDefined(request.Mode) || (request.Role is DishRole role && !Enum.IsDefined(role))) throw new ArgumentException("INVALID_SCOPE");
        if (request.NameFilter is null || request.NameFilter.Length > 100) throw new ArgumentException("INVALID_NAME_FILTER");
        if (request.Turns.Count > MaximumTurns || request.Constraints.Count > MaximumConstraints ||
            request.Turns.Concat(request.Constraints).Any(string.IsNullOrWhiteSpace) ||
            request.Turns.Concat(request.Constraints).Sum(text => (long)text.Length) > MaximumRequestCharacters ||
            request.ExcludedDishIds.Count > MaximumExclusions || request.ExcludedDishIds.Contains(Guid.Empty))
            throw new ArgumentException("REQUEST_LIMIT_EXCEEDED");
        if (request.Mode == RecommendationMode.Request && request.Turns.Count == 0 && request.Constraints.Count == 0)
            throw new ArgumentException("REQUEST_INTENT_REQUIRED");
    }
}
