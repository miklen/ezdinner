## Context

The dinner suggestions feature (commit c5e1685) is implemented entirely within `EzDinner.Query.Core`. The layer currently contains two distinct kinds of code:

1. **Pure domain logic** — scoring rules, the suggestion engine, and value objects. No I/O. No infrastructure dependencies. Could be unit-tested without a database.
2. **Query orchestration** — the service that loads data from repositories and returns a shaped result.

Placing domain logic in the Query layer violates the CQRS architecture: the Query layer should coordinate reads, not own business rules. `EzDinner.Core` is the correct home for pure domain concepts.

`EzDinner.Query.Core` already declares a project reference to `EzDinner.Core`, so no new reference is needed.

### Files currently in `EzDinner.Query.Core/SuggestionQueries/`

| File | Nature | Correct layer |
|------|--------|---------------|
| `IScoringRule.cs` | Domain interface | `EzDinner.Core` |
| `OverdueScoringRule.cs` | Pure domain rule | `EzDinner.Core` |
| `RatingScoringRule.cs` | Pure domain rule | `EzDinner.Core` |
| `RecencyPenaltyRule.cs` | Pure domain rule | `EzDinner.Core` |
| `LeftoverPatternRule.cs` | Pure domain rule | `EzDinner.Core` |
| `DinnerSuggestionEngine.cs` | Pure domain service | `EzDinner.Core` |
| `DishCandidate.cs` | Domain value object | `EzDinner.Core` |
| `DishScore.cs` | Domain value object | `EzDinner.Core` |
| `SuggestionContext.cs` | Domain value object | `EzDinner.Core` |
| `DishCandidateFactory.cs` | Mixed: pure computation + repo I/O | Split (see below) |
| `IDinnerSuggestionService.cs` | Query interface | Stay in `EzDinner.Query.Core` |
| `DinnerSuggestionService.cs` | Query orchestrator | Stay in `EzDinner.Query.Core` |
| `DaySuggestion.cs` | Query result model | Stay in `EzDinner.Query.Core` |

## Goals / Non-Goals

**Goals:**
- Move all pure domain logic (scoring rules, engine, value objects) to `EzDinner.Core/Suggestions/`
- Eliminate the I/O dependency in `DishCandidateFactory` by extracting the pure computation into a domain factory and inlining the data loading into `DinnerSuggestionService`
- Update all `using` directives and namespaces
- No behavioural changes — all tests pass unchanged

**Non-Goals:**
- Changing the scoring algorithm or weights
- Changing the HTTP API surface
- Changing integration test logic
- Adding new scoring rules or features

## Decisions

### Decision 1: Target subdirectory in EzDinner.Core

**Chosen:** `EzDinner.Core/Suggestions/`
**Namespace:** `EzDinner.Core.Suggestions`

Existing aggregates live under `EzDinner.Core/Aggregates/`. Suggestions are not a single aggregate — they are a domain service and supporting value objects. A `Suggestions/` subdirectory follows the same flat-by-feature pattern used elsewhere in Core.

**Alternative considered:** Nest under `Aggregates/DishAggregate/` — rejected, because suggestions span multiple aggregates (Dish + Dinner) and are not owned by either.

### Decision 2: Splitting DishCandidateFactory

`DishCandidateFactory` currently mixes two responsibilities:
- `BuildCandidatesAsync` — loads dishes and dinners from repositories (infrastructure I/O)
- `BuildCandidates` / `BuildCandidate` — pure statistical computation on pre-loaded data

**Chosen:** Remove `BuildCandidatesAsync` and its repository dependencies. Move the pure `BuildCandidates` / `BuildCandidate` methods to `EzDinner.Core/Suggestions/DishCandidateFactory.cs`. Inline the data loading that `BuildCandidatesAsync` did into `DinnerSuggestionService.SuggestDay`, matching the pattern already used by `SuggestWeek`.

**Rationale:** `SuggestWeek` already loads its own data inline. `SuggestDay` delegates data loading to the factory only as an inconsistency. After the split, both methods in `DinnerSuggestionService` follow the same load-then-compute pattern, and `DishCandidateFactory` becomes a pure domain factory with no dependencies.

**Alternative considered:** Keep `DishCandidateFactory` in Query.Core with repos injected, move only the scoring rules to Core — rejected, because the candidate-building computation *is* domain logic (computing DaysSinceLast, TypicalFrequencyDays, LeftoverFrequencyRatio from usage history). It belongs in Core alongside the types it produces.

### Decision 3: DaySuggestion stays in EzDinner.Query.Core

`DaySuggestion` is a query result container (date + nullable `DishScore`). It is defined by the query interface and returned to the HTTP function. It has no domain behaviour. Keeping it in Query.Core is correct; it is the shaped output of the read operation.

## Risks / Trade-offs

- **Risk:** Integration tests reference types via their old namespace — compilation fails until all `using` statements are updated.
  → Mitigation: update `using` directives in `EzDinner.IntegrationTests` as part of the same change.

- **Risk:** DI registrations in `Program.cs` reference concrete types — if namespaces change, the build will fail clearly (not silently).
  → Mitigation: update registrations in the same pass; the compiler will catch any missed references.

- **Trade-off:** `DinnerSuggestionService.SuggestDay` becomes slightly longer (inline data loading). Acceptable — it matches `SuggestWeek`'s structure and keeps infrastructure concerns out of the domain factory.
