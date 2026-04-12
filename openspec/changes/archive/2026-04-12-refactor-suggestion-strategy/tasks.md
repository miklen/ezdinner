## 1. Shared Context Assembly (Query.Core)

- [x] 1.1 Create `FamilySuggestionContext` value object in `EzDinner.Query.Core/SuggestionQueries/` holding loaded dishes, historical dinners, wishlist entries, and excluded dish IDs
- [x] 1.2 Create `SuggestionContextAssembler` internal service in `EzDinner.Query.Core/SuggestionQueries/` with parallel repository calls (`Task.WhenAll`) returning `FamilySuggestionContext`

## 2. Planner Abstraction (Query.Core)

- [x] 2.1 Create `IDinnerWeekPlanner` interface in `EzDinner.Query.Core/SuggestionQueries/` with method `PlanWeek(FamilySuggestionContext, LocalDate weekStart, IReadOnlyList<LocalDate> alreadyPlanned) → IReadOnlyList<DaySuggestion>`
- [x] 2.2 Create `RuleBasedDinnerWeekPlanner` in `EzDinner.Query.Core/SuggestionQueries/` by extracting the inline week-loop from `DinnerSuggestionService.SuggestWeek`

## 3. AI Infrastructure Adapter (Infrastructure)

- [x] 3.1 Define `IAnthropicWeekPlanClient` interface in `EzDinner.Query.Core/SuggestionQueries/` with method `PlanWeekAsync(string dishCatalogMarkdown, LocalDate weekStart, string? userContext) → IReadOnlyList<AiDayPlanResult>` (or equivalent minimal return type)
- [x] 3.2 Refactor `AnthropicWeekPlannerService` into `AnthropicWeekPlanClient : IAnthropicWeekPlanClient` — remove all repository dependencies; retain only API call and JSON parsing

## 4. AI Planner (Query.Core)

- [x] 4.1 Create `AiDinnerWeekPlanner : IDinnerWeekPlanner` in `EzDinner.Query.Core/SuggestionQueries/` that builds the dish catalog from `FamilySuggestionContext`, calls `IAnthropicWeekPlanClient`, and maps results to `DaySuggestion`

## 5. Update DinnerSuggestionService (Query.Core)

- [x] 5.1 Inject `IDinnerWeekPlanner` and `SuggestionContextAssembler` into `DinnerSuggestionService`
- [x] 5.2 Rewrite `DinnerSuggestionService.SuggestWeek` to: assemble context via `SuggestionContextAssembler`, determine already-planned dates, delegate to `IDinnerWeekPlanner`
- [x] 5.3 Remove the now-inlined week-loop code and direct repository calls from `DinnerSuggestionService.SuggestWeek`

## 6. DI Registration (Functions)

- [x] 6.1 Add `Suggestions:Planner` config read in `Program.cs`; register `RuleBasedDinnerWeekPlanner` or `AiDinnerWeekPlanner` as `IDinnerWeekPlanner` based on config value; default to rule-based with startup warning if key is absent/unknown
- [x] 6.2 Register `SuggestionContextAssembler` in DI
- [x] 6.3 Register `AnthropicWeekPlanClient` as `IAnthropicWeekPlanClient` (only when AI planner is active, or always — scoped to avoid wasted allocation)
- [x] 6.4 Remove `IAiWeekPlannerService` registration from `Setup.RegisterEnrichment`

## 7. Update AiWeekPlanFunction (Functions)

- [x] 7.1 Replace `IAiWeekPlannerService` dependency with `IDinnerSuggestionService` in `AiWeekPlanFunction`
- [x] 7.2 Update handler to call `IDinnerSuggestionService.SuggestWeek` and map `DaySuggestion[]` → `AiWeekPlanSuggestion[]` (same response model, same mapping logic)

## 8. Cleanup

- [x] 8.1 Delete `IAiWeekPlannerService.cs` and the `AiPlanningQueries/` folder from `EzDinner.Query.Core`
- [x] 8.2 Verify no remaining references to `IAiWeekPlannerService` across the solution (build must succeed)
- [x] 8.3 Run unit tests (`cd api/test/EzDinner.UnitTests && dotnet test`) and confirm all pass
- [x] 8.4 Run backend build (`cd api/src/EzDinner.Functions && dotnet build`) with no errors or warnings
