## Context

The existing rule-based suggestion system is architecturally clean:

- **Core**: `DinnerSuggestionEngineService` orchestrates `IScoringRule` implementations (Strategy pattern)
- **Query.Core**: `DinnerSuggestionService` loads data and drives the engine
- **Infrastructure**: repository implementations only

The AI week planner (`AnthropicWeekPlannerService`) was added in Infrastructure with full orchestration logic: it loads dishes/dinners/wishlist from repositories, builds a dish catalog, calls the Anthropic API, parses the JSON response, and returns structured results. This puts business logic in the wrong layer and duplicates data-loading code already present in `DinnerSuggestionService`.

The two systems address the same problem (what should we cook this week?) but through separate entry points (`IDinnerSuggestionService` vs `IAiWeekPlannerService`), making it impossible to swap them without changing call sites.

## Goals / Non-Goals

**Goals:**
- Restore Clean Architecture: Infrastructure holds only I/O adapters, Query.Core holds orchestration
- Introduce `IDinnerWeekPlanner` so rule-based and AI paths are interchangeable behind a single interface
- Consolidate duplicate data-loading into one shared internal component in Query.Core
- Make the active strategy a DI/config choice — no code change to switch strategies
- Eliminate `IAiWeekPlannerService` and the `AiPlanningQueries/` namespace; `IDinnerSuggestionService` becomes the sole public suggestion API

**Non-Goals:**
- Changing any suggestion behavior (scores, AI prompt, output format)
- Adding new suggestion strategies beyond the two existing ones
- Frontend or API surface changes
- Changing the day-suggestion path (remains rule-based only)

## Decisions

### 1. Planner interface lives in Query.Core, not Core

`IDinnerWeekPlanner` depends on loaded data (dishes, dinners, context) and returns `IReadOnlyList<DaySuggestion>`. This is query-layer orchestration, not pure domain logic, so it belongs in `EzDinner.Query.Core` rather than `EzDinner.Core`.

*Alternative: put interface in Core.* Rejected — Core must remain free of query result types and I/O abstractions.

**Naming rationale:** "Strategy" is a design pattern name, not domain language. Domain experts talk about the system *planning dinners* — `IDinnerWeekPlanner` expresses what the abstraction does in terms the domain understands. Implementations are `RuleBasedDinnerWeekPlanner` and `AiDinnerWeekPlanner`.

### 2. Shared context assembly extracted as an internal factory in Query.Core

A new internal class `SuggestionContextAssembler` in Query.Core centralises the parallel loading of dishes, historical dinners, wishlist, and exclusion resolution used by both planners. Both `RuleBasedDinnerWeekPlanner` and `AiDinnerWeekPlanner` receive a pre-loaded `FamilySuggestionContext` value object.

**Naming rationale:** "DataLoader" is programmer jargon. The class *assembles the context* needed to plan a week — `SuggestionContextAssembler` expresses intent. The value object is `FamilySuggestionContext` (distinct from the existing per-day `SuggestionContextValueObject`).

*Alternative: pass repositories directly to planners.* Rejected — planners would then independently re-implement loading; shared code would diverge again.

### 3. Infrastructure retains only the HTTP adapter

`AnthropicWeekPlannerService` is renamed/refactored to `AnthropicWeekPlanClient : IAnthropicWeekPlanClient`. It accepts a dish catalog string and a prompt, calls the API, and returns the raw parsed result. No repository access.

The interface `IAnthropicWeekPlanClient` is defined in Query.Core so the strategy can depend on an abstraction rather than the concrete Infrastructure class.

*Alternative: inline the API call inside `AiWeekSuggestionStrategy`.* Rejected — makes the strategy untestable without hitting the real API.

### 4. Planner selection via DI, driven by config key

`Program.cs` reads a `Suggestions:Planner` config value (`"RuleBased"` | `"Ai"`) and registers the appropriate `IDinnerWeekPlanner` implementation. Switching planners requires only an app setting change.

*Alternative: runtime planner switching via feature flag.* Out of scope; config-time selection is sufficient and simpler.

### 5. `DinnerSuggestionService.SuggestWeek` delegates to the active planner

`IDinnerSuggestionService` remains the public query API unchanged. Internally, `DinnerSuggestionService.SuggestWeek` uses `IDinnerWeekPlanner` (injected). `SuggestDay` is unaffected and remains rule-based.

`AiWeekPlanFunction` is updated to call `IDinnerSuggestionService.SuggestWeek` instead of `IAiWeekPlannerService`. The response model (`AiWeekPlanSuggestion[]`) stays the same; it's mapped from `DaySuggestion` as before.

### 6. `RuleBasedDinnerWeekPlanner` wraps the existing engine — no engine changes

The existing `DinnerSuggestionEngineService` and all scoring rules are untouched. A new thin `RuleBasedDinnerWeekPlanner` class wraps the engine loop that currently lives inline in `DinnerSuggestionService.SuggestWeek`.

## Risks / Trade-offs

- **Risk: Strategy registration error at startup** — if neither strategy is registered (bad config key), injection fails at first request. → Mitigation: default to `"RuleBased"` if key is absent; add startup validation log.
- **Risk: `FamilySuggestionContext` becomes a large shared bag** — over time, planners may need different data, inflating the shared context. → Mitigation: keep only fields used by both planners; planners can load extras themselves if needed.
- **Risk: Test coverage gap during refactor** — existing `DinnerSuggestionService` tests may not cover the strategy dispatch path. → Mitigation: add unit tests for `DinnerSuggestionService` with both strategy stubs before or alongside the refactor.

## Migration Plan

1. Add `IDinnerWeekPlanner`, `FamilySuggestionContext`, `SuggestionContextAssembler` to Query.Core
2. Extract `RuleBasedDinnerWeekPlanner` from `DinnerSuggestionService`
3. Extract `AiDinnerWeekPlanner` from `AnthropicWeekPlannerService`; define `IAnthropicWeekPlanClient`
4. Thin out `AnthropicWeekPlannerService` → `AnthropicWeekPlanClient`
5. Update `DinnerSuggestionService.SuggestWeek` to delegate to `IDinnerWeekPlanner`
6. Update DI registration in `Program.cs`; remove `RegisterEnrichment`'s `IAiWeekPlannerService` binding
7. Update `AiWeekPlanFunction` to use `IDinnerSuggestionService`
8. Delete `IAiWeekPlannerService`, `AiPlanningQueries/` namespace

Rollback: git revert. No DB migrations or deployed config changes required — the new `Suggestions:Strategy` key defaults to `"RuleBased"` so existing deployments without the key continue working.

## Open Questions

- Should `AiWeekSuggestionStrategy` retain the full prompt text inside Query.Core, or accept it as injected config? (Current approach: prompt is hardcoded in the strategy — acceptable for now.)
- Is `DaySuggestion` the right output type for the AI strategy, or should the AI path produce richer output in future? (Deferred — `DaySuggestion` is sufficient and avoids new public types.)
