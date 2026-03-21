## Context

The suggestion engine uses a Strategy pattern with additive scoring rules registered via DI. Current rules: `OverdueScoringRule` (weight 10), `RatingScoringRule` (weight 5), `RecencyPenaltyRule` (penalty only), `LeftoverPatternRule` (bonus 15, conditional). The `OverdueScoringRule` dominates — a dish unused for 900 days scores 300 points, completely drowning out rating (max 5) and any new signals.

`DishCandidateValueObject` currently carries: `DishId`, `DishName`, `Rating`, `DaysSinceLast`, `TypicalFrequencyDays`, `LeftoverFrequencyRatio`. The architecture is plug-in ready: adding a rule requires implementing `IScoringRule` and registering in DI.

This change depends on `dish-metadata-ai-enrichment` having shipped: dishes must have `EffortLevel` and `SeasonAffinity` fields available.

## Goals / Non-Goals

**Goals:**
- Add `SeasonalAffinityRule` and `EffortMatchRule` as new pluggable scoring rules
- Propagate `EffortPreference` from HTTP request → context → `EffortMatchRule`
- Expose `SeasonAffinity` and `EffortLevel` on `DishCandidateValueObject`
- Rebalance `OverdueScoringRule` weight so new signals aren't drowned out
- Frontend per-day effort selector in week planning view

**Non-Goals:**
- Learned day-of-week tendency (inferring "Friday = Quick" from history) — deferred
- Tag diversity rule (penalizing same cuisine back-to-back) — future change
- Storing effort preferences beyond the current planning session

## Decisions

### D1: Rebalance OverdueScoringRule weight from 10 to 3
The current formula `(DaysSinceLast / TypicalFrequencyDays) * 10` produces unbounded scores for rarely-used dishes. Reducing to weight 3 caps the typical overdue score range while still rewarding rotation discipline. New rules use weights of 8 (SeasonalAffinityRule boost/penalty) and 6 (EffortMatchRule boost/penalty), making them meaningful without dominating.

Proposed weight table:
| Rule | Match/Boost | Mismatch/Penalty |
|------|-------------|-----------------|
| OverdueScoringRule | `(DaysSinceLast/TypicalFreq) * 3` | n/a |
| RatingScoringRule | `(Rating/10) * 5` | n/a |
| RecencyPenaltyRule | 0 | -20 (soft), -1000 (hard) |
| LeftoverPatternRule | +15 (conditional) | 0 |
| SeasonalAffinityRule | +8 (match) | -8 (mismatch) |
| EffortMatchRule | +6 (match) | -6 (mismatch) |

These weights are constants in each rule class. No configuration externalisation in this change.

### D2: Season derived inside SeasonalAffinityRule from context.TargetDate
The rule receives `SuggestionContextValueObject` which already carries `TargetDate` (a `LocalDate`). Season derivation (month → season) is a pure function inside the rule. No new context field needed. This keeps the derivation close to the rule that uses it.

### D3: EffortPreference added as nullable field on SuggestionContextValueObject
`SuggestionContextValueObject` is a value object in the domain. Adding a nullable `EffortLevel? EffortPreference` field is the minimal change. The `EffortMatchRule` checks for null and returns 0 immediately. No rule interface changes needed.

### D4: DishCandidateValueObject gains EffortLevel and SeasonAffinity
`DishCandidateFactory` builds candidates from `Dish` aggregates. It already extracts `Rating`, `DaysSinceLast`, etc. Adding `EffortLevel?` and `SeasonAffinity?` (nullable, from dish metadata) is a straightforward extension. Rules read these from the candidate.

### D5: Per-day effort preference in suggest-week via a date-keyed dictionary
The suggest-week endpoint needs to accept effort preferences for each of the 7 days independently. Rather than 7 separate query params, a repeatable param pattern `effortPreferences[2026-03-23]=Quick&effortPreferences[2026-03-25]=Elaborate` or a JSON body is cleanest. Since the current endpoints use GET with query params, a repeatable key-value format (`effortPref=2026-03-23:Quick&effortPref=2026-03-25:Elaborate`) is chosen for consistency with existing `exclude` param style.

`DinnerSuggestionService.SuggestWeek` iterates over each day, builds a per-day context with the matching effort preference (null if not specified), and calls the engine per day.

### D6: Frontend persists effort preferences in component state, not Pinia store
Effort preferences are ephemeral planning-session data. They don't need to survive navigation or page reload. Local `ref`/`reactive` state in the week planning component is sufficient. If the user reloads, preferences reset — acceptable for this use case.

## Risks / Trade-offs

- **[Risk] Weight rebalancing changes existing suggestion output** → Mitigation: this is intentional and expected. The current output is acknowledged as poor quality. The new weights are a considered starting point; they can be tuned after real-world use. Existing unit tests should be updated to reflect new expected scores, not preserved as-is.
- **[Risk] SeasonalAffinityRule penalizes good dishes during shoulder seasons** → Mitigation: penalty is -8, not blocking. An Elaborate winter stew still surfaces in summer if it's highly rated and overdue — it just ranks lower than summer dishes. The rule biases, it doesn't exclude.
- **[Risk] Unenriched dishes score neutrally on new rules** → Mitigation: this is the intended behavior (D1 in dish-metadata design). Neutral scoring means the engine degrades gracefully to previous behavior for unenriched dishes.
- **[Risk] EffortMatchRule penalizes dishes with mismatched effort even if they're the only option** → Mitigation: rules are additive and no hard block is applied. A -6 penalty can be overcome by overdue/rating scores. The last dish standing will still be suggested.

## Migration Plan

- No data migration needed
- Weight changes are code constants — deploy backend, suggestions immediately use new weights
- Frontend effort selector is additive — no existing flows broken
- Recommend deploying after `dish-metadata-ai-enrichment` and after at least some dishes have been enriched, so the new rules have data to work with

## Open Questions

- Should the effort preference selector in the frontend default to "unset" or pre-populate based on day of week (e.g., Friday always defaults to Quick)? Recommend "unset" for now; learned defaults deferred to a future change.
