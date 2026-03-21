## Context

Dishes in EzDinner can fall out of favor over time — families try a recipe, stop using it, but keep it in the catalog for reference. The current suggestion engine uses `OverdueScoringRule` with weight 10, which rewards dishes proportionally to how long since they were last used. A dish unused for 2 years scores extremely high, surfacing constantly as a top suggestion even though the family has implicitly abandoned it.

The `Dish` aggregate already has a `Deleted` (soft-delete) flag, but this is used to hide dishes entirely — not to preserve them for reference. Archiving is a softer state: removed from active use but still accessible.

## Goals / Non-Goals

**Goals:**
- Add `IsArchived` state to the `Dish` aggregate with explicit `Archive()` / `Reactivate()` domain methods
- Filter archived dishes from suggestion candidates and default catalog queries
- Expose archive/reactivate via HTTP endpoints
- Preserve history integrity — archived dishes remain visible in past dinner records

**Non-Goals:**
- Bulk archive operations (archive by tag, archive all unused)
- Automatic archiving based on usage patterns (out of scope — explicit user action only)
- Changing how soft-delete (`Deleted`) works

## Decisions

### D1: `IsArchived` as a separate flag from `Deleted`
`Deleted` means "treat as if it never existed." `IsArchived` means "no longer in rotation but keep for reference." These are distinct intents and require separate flags. Archived dishes appear in history; deleted dishes are excluded even from history views.

### D2: Domain methods enforce state transitions
`Dish.Archive()` throws if already archived. `Dish.Reactivate()` throws if not archived. The application layer catches domain exceptions and maps to 409 Conflict. This keeps business rules in the domain, not scattered across handlers.

### D3: Catalog query opt-in via `includeArchived` parameter
Default behavior excludes archived dishes — this matches the primary use case (active catalog). The opt-in parameter is passed through to the repository query. No separate "archived catalog" endpoint needed.

### D4: Suggestion filtering at the query service layer
`DinnerSuggestionService` fetches dishes then passes them to `DishCandidateFactory`. The filter `dish => !dish.IsArchived` is applied at the dish-fetch step in the query service, before candidates are built. This keeps filtering logic out of the engine and out of the domain.

Alternatives considered:
- Filter in `DishCandidateFactory`: would mix candidate-building with eligibility concerns
- Filter as a scoring rule (large negative score): hacky, archived dishes could still appear if few candidates remain

### D5: Reactivation resets no historical data
`DaysSinceLast` will be large for a recently reactivated dish — this is correct behavior. The family is bringing the dish back because they want it again; the engine should treat it as overdue and surface it. No artificial reset needed.

## Risks / Trade-offs

- **Re-enrichment on reactivation**: If Change 2 (dish metadata + AI enrichment) ships later, reactivated dishes may need re-enrichment if their AI-suggested tags have expired. Not a concern for this change but worth noting as a future interaction.
- **`includeArchived` query param exposure**: The frontend must be careful not to inadvertently show archived dishes in contexts where they shouldn't appear (e.g., dish picker when planning a dinner). Each consumer of the dish list must explicitly opt in.

## Migration Plan

- No data migration required — existing dishes default `IsArchived = false`
- CosmosDB document schema change is additive; missing field reads as `false` (handled by default value in the aggregate constructor)
- Deploy backend then frontend — no coordination window needed

## Open Questions

- None at this time.
