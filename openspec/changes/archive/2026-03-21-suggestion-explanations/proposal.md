## Why

Users see a suggested dish but have no way to understand why it was chosen, making the suggestions feel arbitrary and easy to distrust or ignore. Surfacing human-readable reasons for each suggestion (e.g. "Highly rated · Not had in 18 days · Matches winter") gives users the context to trust — or consciously override — the recommendation.

## What Changes

- Extend `IScoringRule` with an optional explanation capability via a new `IExplainableScoringRule` interface that returns a human-readable reason string alongside the score
- All six scoring rules implement `IExplainableScoringRule` with concise, plain-language reason strings
- `DinnerSuggestionEngineService` collects non-null reasons from the top-ranked candidate into `DishScoreValueObject.Reasons`
- `DishScoreValueObject` gains a `IReadOnlyList<string> Reasons` property
- The API response for suggestions includes a `reasons` string array alongside the existing dish fields
- The `SuggestedDish` frontend component shows a small "why?" toggle that reveals the reason tags inline beneath the dish name

## Capabilities

### New Capabilities

- `suggestion-explanations`: Scoring rules produce human-readable reasons; reasons are collected for the top candidate and surfaced in the API and frontend

### Modified Capabilities

- `suggest-day`: The day suggestion response now includes a `reasons` field
- `suggest-week`: The week suggestion response now includes `reasons` per day

## Impact

- `EzDinner.Core` — `IScoringRule`, all six rule classes, `DishScoreValueObject`, `DinnerSuggestionEngineService`
- `EzDinner.Query.Core` — `DaySuggestion` (no structural change; `DishScoreValueObject` already flows through)
- `EzDinner.Functions` — suggestion function DTOs gain a `reasons` field
- `web/components/Plan/SuggestedDish.vue` — toggle to reveal reason tags
- `web/types` — `DishSuggestion` type gains `reasons: string[]`
- No database changes; no auth changes
