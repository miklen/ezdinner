## Context

The suggestion engine ranks dishes through six scoring rules: `RatingScoringRule`, `OverdueScoringRule`, `RecencyPenaltyRule`, `SeasonalAffinityRule`, `EffortMatchRule`, and `LeftoverPatternRule`. Currently `IScoringRule` returns only a `double`; the engine sums contributions and the winner's score is opaque to the caller. `DishScoreValueObject` exposes only `TotalScore`, `Rating`, and `DaysSinceLast`.

Users see a dish name with a small rating and "Nd ago" label — no indication of *why* this dish ranked first. The goal is to surface a short ordered list of plain-language reasons (e.g. "Not had in 18 days", "Matches winter", "Often served as leftovers") for the top-ranked suggestion on each day/week slot.

## Goals / Non-Goals

**Goals:**
- Produce per-rule reason strings for the winning candidate only
- Expose reasons through the existing `DishScoreValueObject` without a separate DTO
- Surface reasons in the `SuggestedDish` frontend component behind a small toggle (keep the default view compact)
- Reasons are computed entirely in the domain layer — no extra queries, no AI calls

**Non-Goals:**
- Explaining non-winning candidates (users don't need to know why rank #3 lost)
- Per-rule score breakdown numbers (text explanations only)
- Localisation — English only for now
- Changing how rules score; this is purely additive

## Decisions

### 1. `IExplainableScoringRule` interface (not baked into `IScoringRule`)

Keeping `IScoringRule.Score` signature unchanged avoids breaking existing rules before they can explain themselves. A separate `IExplainableScoringRule : IScoringRule` interface adds `string? Explain(DishCandidateValueObject, SuggestionContextValueObject)` — returning `null` means "no notable reason to surface" (zero-contribution, or neutral case).

**Alternative considered:** Add `Explain` directly to `IScoringRule` with a default `null` return. Rejected: interface pollution — `Score` and `Explain` are conceptually separate concerns.

### 2. Collect explanations after ranking (winner-only)

`DinnerSuggestionEngineService.Rank` identifies the winner, then calls `Explain` on each `IExplainableScoringRule` for that candidate, filters nulls, and stores the resulting list in `DishScoreValueObject.Reasons`. All other scored candidates get an empty `Reasons` list — callers that only need the top dish don't pay for explanations on losers.

**Alternative considered:** Collect reasons for all candidates during scoring. Rejected: wasteful — only the top suggestion is ever shown to the user.

### 3. Reason string design per rule

| Rule | Positive (non-null) | Negative (non-null) | Null (omit) |
|---|---|---|---|
| `RatingScoringRule` | "Rated {N}/10" (when rating ≥ 6) | — | rating < 6 |
| `OverdueScoringRule` | "Not had in {N} days" (when overdue ratio ≥ 1.0) | — | not overdue |
| `RecencyPenaltyRule` | — | "Served {N} days ago" (when penalty applies, ≤ 7 days) | no penalty |
| `SeasonalAffinityRule` | "Matches the season" | "Out of season" | AllYear or no affinity |
| `EffortMatchRule` | "Matches your effort preference" | — | no preference or no effortLevel |
| `LeftoverPatternRule` | "Often served as leftovers" | — | no bonus |

Negative reasons are included only for `RecencyPenaltyRule` and `SeasonalAffinityRule` — these meaningfully explain *why* a suggestion might seem odd (e.g. a winter dish appearing despite the penalty).

### 4. Frontend: toggle in `SuggestedDish.vue`

A small info icon (`mdi-information-outline`) next to the reroll button shows/hides a row of compact reason tags. The toggle is local state — no store needed. Tags render as inline `<span>` elements with muted styling. Mobile: same interaction (tap icon to toggle).

**Alternative considered:** Tooltip on the dish name. Rejected: tooltips are unreliable on mobile (see CLAUDE.md gotcha) and don't work well inside the plan timeline.

## Risks / Trade-offs

- [Breaking DishScoreValueObject constructor] Adding `Reasons` requires updating all call sites that construct this value object. There are only two: `DinnerSuggestionEngineService` (internal) and unit tests. Low risk.
- [Reason strings becoming stale] If rule logic changes, the explanation text may lag. Mitigation: keep `Explain` co-located with `Score` in the same class — a developer changing the rule threshold should update the explanation text at the same time.
- [Empty reasons list] If all rules return null for the winner (e.g. a perfectly average dish), the toggle button still appears but shows nothing. Mitigation: hide the toggle when `reasons` is empty.
