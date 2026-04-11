## Why

The wish list captures explicit family intent — dishes the family actively wants to eat — but the suggestion engine currently ignores it, treating a highly-voted wish the same as a dish no one has thought about. Surfacing wished-for dishes in suggestions closes the loop between the two features.

## What Changes

- A new `WishlistBoostRule` scoring rule boosts wished-for dishes based on vote count; the more votes, the stronger the boost.
- `SuggestionContextValueObject` gains a dictionary of wished dish IDs → vote counts, populated by the query layer before scoring.
- Both `SuggestDayQuery` and `SuggestWeekQuery` are extended to fetch active wish items and inject them into the suggestion context.
- The rule is registered with the DI container alongside the existing scoring rules.

## Capabilities

### New Capabilities
- `suggest-wish-boost`: Scoring rule and context extension that give wished-for dishes a score bonus proportional to their vote count in suggestions.

### Modified Capabilities
- `suggest-day`: Suggestion context now includes wished dish data; response `reasons` may include a wish-boost explanation.
- `suggest-week`: Same extension as suggest-day applied to the week-level query.

## Impact

- **`EzDinner.Core`** — new `WishlistBoostRule`; extended `SuggestionContextValueObject`
- **`EzDinner.Query.Core`** — `SuggestDayQuery` and `SuggestWeekQuery` fetch wishlist and include it in context
- **`EzDinner.Functions`** — DI registration for `WishlistBoostRule`
- **No API contract changes** — existing endpoints unchanged; reasons array may contain new explanation strings
