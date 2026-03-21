## Why

The suggestion engine is dominated by a single signal: how long since a dish was last used. The `OverdueScoringRule` at weight 10 drowns out everything else — rating, recency, and leftover patterns all play minor roles. The engine cannot distinguish a quick weeknight dish from an elaborate Sunday roast, or a summer grilling dish from a winter stew. With structured dish metadata now available (from `dish-metadata-ai-enrichment`), the engine can be made genuinely contextual: seasonal, effort-aware, and meaningfully balanced.

## What Changes

- New `SeasonalAffinityRule`: boosts dishes whose `SeasonAffinity` matches the season of the suggestion target date. Dishes with no affinity set score neutrally.
- New `EffortMatchRule`: boosts dishes whose `EffortLevel` matches an optional effort preference on the suggestion context. Only fires when effort preference is provided. Dishes with no effort level set score neutrally.
- `OverdueScoringRule` weight reduced so seasonal and effort signals have meaningful impact
- `SuggestionContextValueObject` gains optional `EffortPreference` field
- Both suggestion endpoints accept an optional `effortPreference` query parameter
- Week planning UI gains a per-day effort preference selector (Quick / Medium / Elaborate / unset)
- Effort preference is passed per-day when requesting suggestions; persisted for the planning session only

## Capabilities

### New Capabilities

- `suggest-seasonal`: Seasonal affinity scoring rule and season derivation from target date
- `suggest-effort`: Effort match scoring rule and effort preference in suggestion context

### Modified Capabilities

- `suggest-day`: Accepts optional `effortPreference` query param; scoring weights rebalanced
- `suggest-week`: Accepts optional `effortPreference` per day; scoring weights rebalanced

## Impact

- **Domain**: Two new `IScoringRule` implementations in `EzDinner.Core/DomainServices/DinnerSuggestions/`
- **Domain**: `SuggestionContextValueObject` gains `EffortPreference` (nullable `EffortLevel`)
- **Domain**: `DishCandidateValueObject` gains `EffortLevel` and `SeasonAffinity` from dish metadata
- **Query**: `DinnerSuggestionService` passes effort preference through to context
- **Functions**: `SuggestDay` and `SuggestWeek` accept `effortPreference` query param
- **Frontend**: Week planning view gains per-day effort selector; suggestion requests include effort preference
- **Depends on**: `dish-metadata-ai-enrichment` (dishes need `EffortLevel` and `SeasonAffinity` fields)
