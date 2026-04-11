## Context

The suggestion engine scores candidates using a pipeline of `IScoringRule` implementations. Each rule receives a `DishCandidateValueObject` (the dish) and a `SuggestionContextValueObject` (per-request context: target date, adjacent dishes, excluded dishes, effort preference). Rules return a numeric score delta; the engine sums deltas, sorts descending, and picks the top candidate.

The wish list (`WishlistItem` aggregate) tracks family-requested dishes with vote counts and expiry. Currently, the suggestion queries (`SuggestDayQuery`, `SuggestWeekQuery`) do not fetch the wishlist — it is invisible to the scoring pipeline.

## Goals / Non-Goals

**Goals:**
- Wished dishes receive a score boost proportional to their vote count.
- The boost produces an explanation string in the `reasons` array.
- No new API parameters are required — the wish context is resolved internally.
- Both suggest-day and suggest-week benefit from the same rule.

**Non-Goals:**
- Guaranteeing a wished dish is always suggested (the boost is additive, not a hard override).
- Surfacing wishlist data in the suggestion API response beyond the `reasons` string.
- Implementing any wish list UI changes in this change.

## Decisions

### 1. Pass wished dish IDs + vote counts via `SuggestionContextValueObject`

**Decision:** Add `IReadOnlyDictionary<Guid, int> WishedDishIds` (dishId → voteCount) to `SuggestionContextValueObject`.

**Rationale:** The context object is already the canonical carrier of per-request state that rules read. Adding wish data here keeps all rules stateless and decoupled from I/O — consistent with `EffortPreference` (also injected via context).

**Alternative considered:** Pass wish data through `DishCandidateValueObject` instead. Rejected — candidates are built before context is assembled, and mixing request-level state (wishes) with dish-level data (history, rating) blurs the model boundary.

### 2. New `WishlistBoostRule` in `EzDinner.Core.DomainServices.DinnerSuggestions`

**Decision:** Create a dedicated rule implementing both `IScoringRule` and `IExplainableScoringRule`.

**Rationale:** Follows the established Rule pattern used by `RatingScoringRule`, `OverdueScoringRule`, etc. A separate class keeps the boost tunable and testable in isolation without touching existing rules.

**Score formula:** `voteCount × baseBoostPerVote`. Starting value for `baseBoostPerVote`: **0.3**. This puts a 3-vote wish (≈ a full family) in the same ballpark as `RatingScoringRule` at a 4-star dish (score ≈ 0.9), making it a meaningful but not dominant signal.

**Reason string:** `"Wished for by the family (N votes)"` — emitted only when the dish is on the wish list.

### 3. Wishlist fetched in both suggestion queries

**Decision:** `SuggestDayQuery` and `SuggestWeekQuery` call `IWishlistRepository.GetActiveAsync(familyId)` and filter non-expired items before building `SuggestionContextValueObject`.

**Rationale:** The queries already assemble all other context (dinner history, dish catalog). Adding one async call here keeps the domain rule free of I/O.

**Note:** `IWishlistRepository.GetActiveAsync(Guid familyId)` already exists with no `now` parameter. The queries will filter `!IsExpired(now)` locally, consistent with how `GetWishlistQuery` works.

## Risks / Trade-offs

- **Stale wish data if cached:** If suggestion queries ever add caching, wish state won't be reflected until cache expires. Currently no caching is in place — no mitigation needed now.
- **Boost constant is opaque:** The `0.3` base boost per vote is a judgment call. If it proves too strong or too weak, it can be adjusted as a constant in `WishlistBoostRule` without a spec change.
- **No upper cap on vote count:** A dish with many votes could accumulate a very large boost. In practice, family sizes are small (2–6 members), capping practical max votes at ~6. No ceiling is added for now.

## Open Questions

- Should the boost decay as the wish approaches expiry (e.g., reduce score in the last 3 days)? Deferred — simple vote-count boost first; decay can be added later.
