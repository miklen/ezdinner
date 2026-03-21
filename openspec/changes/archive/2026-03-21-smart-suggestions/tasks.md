## 1. Domain — Extend Candidate and Context

- [x] 1.1 Add `EffortLevel?` and `SeasonAffinity?` to `DishCandidateValueObject`
- [x] 1.2 Add `EffortLevel? EffortPreference` to `SuggestionContextValueObject`
- [x] 1.3 Update `DishCandidateFactory` to populate `EffortLevel` and `SeasonAffinity` from `Dish` metadata

## 2. Domain — SeasonalAffinityRule

- [x] 2.1 Create `SeasonalAffinityRule` implementing `IScoringRule`
- [x] 2.2 Implement season derivation: month → `Summer | Autumn | Winter | Spring` from `context.TargetDate`
- [x] 2.3 Score: +8 if candidate season matches, -8 if it doesn't match, 0 if candidate has no season or season is `AllYear`
- [x] 2.4 Register `SeasonalAffinityRule` in `Program.cs` DI
- [x] 2.5 Unit tests: summer dish in summer → +8, winter dish in summer → -8, AllYear → 0, no affinity → 0, all four season boundaries

## 3. Domain — EffortMatchRule

- [x] 3.1 Create `EffortMatchRule` implementing `IScoringRule`
- [x] 3.2 Score: +6 if candidate effort matches preference, -6 if it doesn't, 0 if no preference in context or candidate has no effort level
- [x] 3.3 Register `EffortMatchRule` in `Program.cs` DI
- [x] 3.4 Unit tests: match → +6, mismatch → -6, no preference → 0, no effort level → 0, all three effort level combinations

## 4. Domain — Rebalance Weights

- [x] 4.1 Reduce `OverdueScoringRule` weight constant from 10 to 3
- [x] 4.2 Update existing `OverdueScoringRule` unit tests to reflect new weight
- [x] 4.3 Verify `RatingScoringRule` weight (5) remains appropriate relative to new scale — no change needed per design

## 5. Query Layer — Effort Preference Propagation

- [x] 5.1 Update `DinnerSuggestionService.SuggestDay` to accept optional `EffortLevel?` and pass to `SuggestionContextValueObject`
- [x] 5.2 Update `DinnerSuggestionService.SuggestWeek` to accept `Dictionary<LocalDate, EffortLevel>?` and apply per-day when building context

## 6. API Layer — Endpoint Parameters

- [x] 6.1 Add optional `effortPreference` query param to `SuggestDay` function; parse to `EffortLevel?`
- [x] 6.2 Add optional repeatable `effortPref=date:value` query params to `SuggestWeek` function; parse to `Dictionary<LocalDate, EffortLevel>`
- [x] 6.3 Pass parsed values through to `DinnerSuggestionService`

## 7. Frontend — Per-Day Effort Selector

- [x] 7.1 Add effort preference selector component (Quick / Medium / Elaborate / Unset) for use in the week planning view
- [x] 7.2 Wire one selector per day row in the week planning view
- [x] 7.3 Store selected effort preferences in local component state (not Pinia)
- [x] 7.4 Pass the selected `effortPreference` for each day when calling the suggest-day or suggest-week API
- [x] 7.5 Clear effort preferences when the user navigates away from the planning view
