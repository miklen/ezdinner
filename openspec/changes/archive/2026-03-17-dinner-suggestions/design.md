## Context

EzDinner already stores everything needed for suggestions: dish ratings, dinner history (dates, consecutive usage), and family structure. The `IDishQueryService` pattern and `DishStats` query model establish a precedent for read-only computation over this data. The suggestion feature adds a scoring engine on top of existing data — no new storage needed.

The stack is .NET 10 Azure Functions (isolated worker, STJ) on the backend and Nuxt 3 / Vue 3 / Pinia on the frontend. Suggestions must fit the existing per-family multi-tenant authorization model.

## Goals / Non-Goals

**Goals:**
- Score all non-deleted dishes for a given family and date context using rule-based heuristics
- Return a ranked suggestion (top-scoring dish) per unplanned day
- Support reroll by accepting an exclusion list of dish IDs from the client
- Lay a scoring architecture that can be extended with AI-based rules later without restructuring
- Full week suggestion (7 days, skipping already-planned days) and single-day suggestion

**Non-Goals:**
- Persisting suggestions — they are ephemeral, computed on demand
- User-configurable scoring weights in this iteration
- Dietary / tag constraints (future iteration)
- ML / AI scoring in this iteration
- Suggesting multiple dishes per dinner (multi-course menus)

## Decisions

### Decision 1: Scoring via composable rules, not a monolithic formula

**Chosen:** A `IScoringRule` interface with discrete implementations (`OverdueScoringRule`, `RatingScoringRule`, `RecencyPenaltyRule`, `LeftoverPatternRule`). The engine iterates all rules and sums scores.

**Why:** Each rule represents a distinct domain concept. Adding an AI rule later means adding a new `IScoringRule` implementation without modifying existing rules. Follows Open/Closed Principle. Also aligns with the "make the implicit explicit" DDD principle — each scoring concept becomes a named type.

**Alternative considered:** Single `DinnerSuggestionEngine.Score()` method with all logic inline. Rejected: rules would blur together, making it hard to tune or replace individual factors.

---

### Decision 2: Stateless reroll via client-side exclusion list

**Chosen:** Endpoints accept an optional `excludedDishIds` query parameter (comma-separated GUIDs). The client tracks previously suggested dish IDs and sends them as exclusions on reroll.

**Why:** Keeps the backend stateless and serverless-friendly. No session state, no database writes. Simple to implement and test.

**Alternative considered:** Server-side seed/offset parameter for deterministic shuffle. Rejected: more complex to implement and doesn't guarantee a meaningfully different dish when the candidate pool is small.

---

### Decision 3: Suggestion logic lives in `EzDinner.Query.Core`, not `EzDinner.Core`

**Chosen:** `IDinnerSuggestionService` in `EzDinner.Query.Core`. Scoring rules and value objects (`DishScore`, `DishCandidate`, `SuggestionContext`) in `EzDinner.Query.Core`.

**Why:** Suggestions are purely a read concern — they never modify state and are computed by joining data across aggregates. The existing `IDishQueryService` pattern lives in `Query.Core` for the same reason. Domain aggregates (`Dish`, `Dinner`) stay clean; scoring logic does not pollute them.

**Alternative considered:** Domain service in `EzDinner.Core`. Rejected: would require the domain to depend on multi-aggregate join queries — a cross-cutting read concern that belongs in the query layer.

---

### Decision 4: Dish usage pattern (leftovers) inferred from history, not declared

**Chosen:** Compute a `LeftoverFrequency` value — the ratio of consecutive-day appearances relative to total appearances — from existing Dinner history. A dish is considered a "leftover dish" if this ratio exceeds a threshold (e.g., ≥ 30%).

**Why:** No new data entry required. Pattern emerges from usage. Consistent with the existing approach of deriving stats from raw Dinner records.

**Alternative considered:** Explicit boolean flag on `Dish` aggregate ("this dish makes leftovers"). Rejected: requires UI changes and user education for an initial iteration.

---

### Decision 5: Week suggestion skips already-planned days

**Chosen:** The `SuggestWeek` endpoint accepts the week start date and skips any date that already has a `Dinner` with `IsPlanned = true`. Those days are not included in the response.

**Why:** We never want to suggest a dish for a day the family already decided on. Callers can apply suggestions to only unplanned days.

**Alternative considered:** Suggest for all 7 days regardless. Rejected: caller would need to cross-reference existing plan, duplicating logic.

---

### Decision 6: Frontend suggestion state managed in a dedicated composable, not in the dinners store

**Chosen:** New `useDinnerSuggestions` composable owns suggestion state (current suggestions, loading, reroll history). The plan page consumes both `useDinnersStore` and `useDinnerSuggestions`.

**Why:** Suggestions are ephemeral UI state, not persistent dinner data. Mixing them into `useDinnersStore` would conflate plan data (saved in CosmosDB) with transient suggestions (discarded on reload). Clean separation of concerns.

---

## Risks / Trade-offs

| Risk | Mitigation |
|------|-----------|
| History is sparse for new families (few dishes, few past dinners) | Scoring degrades gracefully: dishes with no history get a neutral "due now" score; ratings still differentiate them |
| Full-history query for all dishes is expensive for large families | Dish usage stats already queried without date bounds in `TopDishes`; acceptable for now. Future: cache stats in a materialized view |
| Rule weights are fixed; families may want different priorities | Weights extracted as constants with names (e.g., `OverdueWeight`, `RatingWeight`) so they're easy to tune or make configurable later |
| Reroll with small dish catalog may cycle through same dishes | Exclusion list grows with each reroll; if all dishes are excluded, return the best dish regardless of exclusions |

## Open Questions

- Should the week suggestion return one dish per day, or a ranked list (top 3) to give the user options without an explicit reroll? Deferred: start with 1 per day; add ranked list in follow-up if UX needs it.
- Should accepted suggestions automatically be added to the dinner plan, or require an explicit "Add to plan" confirmation? Start with explicit confirmation to keep suggestion and planning flows decoupled.
