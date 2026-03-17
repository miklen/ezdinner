## 1. Domain Scoring Model (EzDinner.Query.Core)

- [x] 1.1 Create `DishCandidate` value object — holds dish ID, name, rating, computed stats (days since last use, typical frequency, leftover frequency ratio)
- [x] 1.2 Create `SuggestionContext` value object — holds the target date, previously planned/suggested dish IDs for adjacent days, and the exclusion list
- [x] 1.3 Create `DishScore` value object — holds dish ID and computed total score (immutable, comparable)
- [x] 1.4 Define `IScoringRule` interface — `Score(DishCandidate candidate, SuggestionContext context): double`
- [x] 1.5 Implement `OverdueScoringRule` — scores based on `daysSinceLast / typicalFrequencyDays` ratio
- [x] 1.6 Implement `RatingScoringRule` — scales dish average rating into a score contribution
- [x] 1.7 Implement `RecencyPenaltyRule` — applies a large penalty for dishes used in the last 7 days; zero-score threshold for last 3 days
- [x] 1.8 Implement `LeftoverPatternRule` — applies a bonus when dish has ≥ 30% consecutive-day usage ratio and was used on the preceding day

## 2. Suggestion Engine (EzDinner.Query.Core)

- [x] 2.1 Create `DinnerSuggestionEngine` — accepts `IEnumerable<IScoringRule>`, exposes `Rank(IEnumerable<DishCandidate>, SuggestionContext): IReadOnlyList<DishScore>` (sorted descending)
- [x] 2.2 Create `DishCandidateFactory` — queries `IDishRepository` and `IDinnerRepository`, computes history stats (leftover frequency, typical frequency, days since last use), and returns `IEnumerable<DishCandidate>`
- [x] 2.3 Create `IDinnerSuggestionService` interface — `SuggestDay(Guid familyId, LocalDate date, IReadOnlyList<Guid> excludedDishIds): Task<DishScore?>` and `SuggestWeek(Guid familyId, LocalDate weekStart, IReadOnlyList<Guid> excludedDishIds): Task<IReadOnlyList<DaySuggestion>>`
- [x] 2.4 Implement `DinnerSuggestionService` — orchestrates `DishCandidateFactory` + `DinnerSuggestionEngine`; skips already-planned days for week; applies exclusion list; falls back to best dish when exclusion exhausts candidates
- [x] 2.5 Create `DaySuggestion` value object — holds `LocalDate Date` and `DishScore? Suggestion`

## 3. HTTP Endpoints (EzDinner.Functions)

- [x] 3.1 Register `IDinnerSuggestionService`, `DishCandidateFactory`, `DinnerSuggestionEngine`, and all `IScoringRule` implementations in DI (`Program.cs`)
- [x] 3.2 Create `SuggestDayFunction` — `GET /api/suggest/family/{familyId}/day/{date}?exclude={dishId1,dishId2,...}` — authorizes as FamilyMember, delegates to `IDinnerSuggestionService.SuggestDay`
- [x] 3.3 Create `SuggestWeekFunction` — `GET /api/suggest/family/{familyId}/week/{weekStart}?exclude={dishId1,...}` — authorizes as FamilyMember, delegates to `IDinnerSuggestionService.SuggestWeek`
- [x] 3.4 Create `SuggestionQueryModel` — response shape: `{ dishId, dishName }` (null when no suggestion); `WeekSuggestionQueryModel`: `{ date, suggestion: SuggestionQueryModel? }[]`
- [x] 3.5 Add AutoMapper profile for `DishScore` → `SuggestionQueryModel` and `DaySuggestion` → `WeekSuggestionQueryModel`

## 4. Frontend Repository & Composable (web)

- [x] 4.1 Add `SuggestionsRepository` (`web/repository/suggestions-repository.ts`) — `suggestDay(familyId, date, excludedDishIds?)` and `suggestWeek(familyId, weekStart, excludedDishIds?)`
- [x] 4.2 Add TypeScript types for suggestion responses (`web/types/index.ts`) — `DishSuggestion { dishId: string, dishName: string }`, `DaySuggestion { date: string, suggestion: DishSuggestion | null }`
- [x] 4.3 Create `useDinnerSuggestions` composable (`web/composables/useDinnerSuggestions.ts`) — exposes `suggestions: Ref<DaySuggestion[]>`, `loading: Ref<boolean>`, `suggestWeek(weekStart)`, `suggestDay(date)`, `rerollWeek()`, `rerollDay(date)`; tracks exclusion lists internally per reroll

## 5. Frontend Components (web)

- [x] 5.1 Create `PlanSuggestionBar` component (`web/components/Plan/SuggestionBar.vue`) — "Suggest week" button that triggers `useDinnerSuggestions.suggestWeek`, displays per-day suggestions, "Reroll week" button
- [x] 5.2 Create `PlanSuggestedDish` component (`web/components/Plan/SuggestedDish.vue`) — chip/card showing suggested dish name for a day, with "Use this" (adds to plan) and "Reroll" (rerolls that day) actions
- [x] 5.3 Wire `PlanSuggestionBar` into `plan.vue` — show bar below week header, pass `dinnersStore.dinners` to allow `SuggestedDish` to skip already-planned days in the UI
- [x] 5.4 Connect "Use this" action — calls `dinnersStore`-backed `dinnerRepository.addDishToMenu` then removes the suggestion for that day

## 6. Integration Tests (EzDinner.IntegrationTests)

- [x] 6.1 Test `DinnerSuggestionService.SuggestDay` — with known dish history, verify top-ranked dish is correctly selected
- [x] 6.2 Test `DinnerSuggestionService.SuggestDay` with exclusion list — verify excluded dishes are skipped and second-ranked dish is returned
- [x] 6.3 Test `DinnerSuggestionService.SuggestWeek` — verify already-planned days are absent from response and unplanned days receive a suggestion
- [x] 6.4 Test `LeftoverPatternRule` — seed history with consecutive-day usage; verify bonus is applied when preceding day matches
