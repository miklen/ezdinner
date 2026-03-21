## 1. Domain — explainable rule interface

- [x] 1.1 Create `IExplainableScoringRule` interface in `EzDinner.Core/DomainServices/DinnerSuggestions/` with method `string? Explain(DishCandidateValueObject candidate, SuggestionContextValueObject context)`

## 2. Domain — implement Explain on each rule

- [x] 2.1 Implement `IExplainableScoringRule` on `RatingScoringRule` — return "Rated {N}/10" when rating ≥ 6, null otherwise
- [x] 2.2 Implement `IExplainableScoringRule` on `OverdueScoringRule` — return "Not had in {N} days" when `daysSinceLast / typicalFrequency ≥ 1.0`, null otherwise
- [x] 2.3 Implement `IExplainableScoringRule` on `RecencyPenaltyRule` — return "Served {N} days ago" when penalty applies (≤ 7 days), null otherwise
- [x] 2.4 Implement `IExplainableScoringRule` on `SeasonalAffinityRule` — return "Matches the season" on boost, "Out of season" on penalty, null for AllYear/no affinity
- [x] 2.5 Implement `IExplainableScoringRule` on `EffortMatchRule` — return "Matches your effort preference" on match, null otherwise
- [x] 2.6 Implement `IExplainableScoringRule` on `LeftoverPatternRule` — return "Often served as leftovers" when bonus applies, null otherwise

## 3. Domain — propagate reasons through the engine and score

- [x] 3.1 Add `IReadOnlyList<string> Reasons` property to `DishScoreValueObject` (default empty; update constructor)
- [x] 3.2 Update `DinnerSuggestionEngineService.Rank` — after ranking, collect reasons from `IExplainableScoringRule` implementations for the top candidate only; set `Reasons` on the winner's `DishScoreValueObject`

## 4. API — surface reasons in response DTOs

- [x] 4.1 Add `string[] Reasons` to the suggestion function's response DTO (or anonymous type) for both suggest-day and suggest-week endpoints; map from `DishScoreValueObject.Reasons`

## 5. Frontend — type and component update

- [x] 5.1 Add `reasons: string[]` to the `DishSuggestion` type in `web/types`
- [x] 5.2 Update `SuggestedDish.vue` — add local `showReasons` toggle ref; render an info icon button (hidden when `reasons` is empty); render a row of compact reason tags when toggled on
