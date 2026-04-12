## Why

The AI week-planning feature was implemented with service logic embedded in the Infrastructure layer (`AnthropicWeekPlannerService`), violating Clean Architecture. Additionally, the AI planner and the existing rule-based engine solve the same problem — generating week suggestions — but are completely separate code paths with duplicated data-loading logic and no shared abstraction, making it impossible to swap or extend the suggestion strategy without touching multiple layers.

## What Changes

- Extract orchestration logic from `AnthropicWeekPlannerService` (Infrastructure) into a proper service in `EzDinner.Query.Core`, leaving Infrastructure as a thin Anthropic API adapter only
- Introduce a `IWeekSuggestionStrategy` interface in `EzDinner.Query.Core` so the rule-based engine and the AI planner are interchangeable implementations
- Consolidate duplicated data-loading code (dishes, dinners, wishlist, exclusions) shared between `DinnerSuggestionService` and `AnthropicWeekPlannerService` into a single shared component
- Remove `IAiWeekPlannerService` and `AiPlanningQueries/` namespace; the unified `IDinnerSuggestionService.SuggestWeek` becomes the single entry point regardless of strategy
- Register the active strategy via DI configuration so switching between rule-based and AI requires only a config change, not code changes
- Move `AnthropicWeekPlannerService` business logic to `EzDinner.Query.Core` as `AiWeekSuggestionStrategy`; the Infrastructure class becomes a pure `IAnthropicWeekPlanClient` adapter (HTTP call + JSON parse only)

## Capabilities

### New Capabilities

- `suggestion-strategy`: Swappable strategy abstraction for week suggestions — defines how the active strategy is selected (AI vs rule-based) and what guarantees each strategy must uphold

### Modified Capabilities

*(No behavioral requirement changes — existing `suggest-week`, `suggest-day`, and related specs remain valid. This refactor changes implementation structure only.)*

## Impact

- **EzDinner.Core** — No changes; scoring rules and engine remain as-is
- **EzDinner.Query.Core** — New `IWeekSuggestionStrategy`, `AiWeekSuggestionStrategy`; `DinnerSuggestionService` becomes strategy-aware; shared data-loading extracted
- **EzDinner.Infrastructure** — `AnthropicWeekPlannerService` gutted to thin HTTP adapter; `IAiWeekPlannerService` registration removed
- **EzDinner.Functions** — `AiWeekPlanFunction` routes to `IDinnerSuggestionService` (via AI strategy) instead of `IAiWeekPlannerService`; response model unchanged
- **EzDinner.Functions/Program.cs** — DI registration updated; strategy selected by config key
- No API surface or frontend changes required
