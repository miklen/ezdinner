## Why

The dinner suggestions feature was implemented with all backend code — including pure domain logic (scoring rules, suggestion engine, value objects) — placed in the Query layer (`EzDinner.Query.Core`). This violates CQRS architecture and DDD principles: business rules belong in the domain, not in the read-model layer.

## What Changes

- Move scoring rule interface and all four implementations (`IScoringRule`, `OverdueScoringRule`, `RatingScoringRule`, `RecencyPenaltyRule`, `LeftoverPatternRule`) from `EzDinner.Query.Core` to `EzDinner.Core`
- Move `DinnerSuggestionEngine` (domain service) from `EzDinner.Query.Core` to `EzDinner.Core`
- Move value objects `DishCandidate`, `DishScore`, and `SuggestionContext` from `EzDinner.Query.Core` to `EzDinner.Core`
- Keep `IDinnerSuggestionService`, `DinnerSuggestionService`, `DishCandidateFactory`, and `DaySuggestion` in `EzDinner.Query.Core` — these are query orchestration concerns (data loading, candidate assembly, result shaping)
- Update `EzDinner.Query.Core` project reference to depend on `EzDinner.Core`
- Update all `using` directives across `EzDinner.Functions` and `EzDinner.Query.Core`

## Capabilities

### New Capabilities

None — this is a structural refactor. No behaviour changes.

### Modified Capabilities

- `suggest-day`: implementation restructured; public API and response shape unchanged
- `suggest-week`: implementation restructured; public API and response shape unchanged

## Impact

- **EzDinner.Core**: gains a `Suggestions/` subdirectory with scoring rules, engine, and value objects
- **EzDinner.Query.Core**: loses domain files; retains query orchestration; gains project reference to `EzDinner.Core`
- **EzDinner.Functions**: `using` directives updated; no functional changes
- **EzDinner.IntegrationTests**: `using` directives updated; test logic unchanged
- No API surface changes — HTTP endpoints, DTOs, and AutoMapper profiles are untouched
