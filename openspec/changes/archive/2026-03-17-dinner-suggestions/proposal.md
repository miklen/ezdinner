## Why

Families spend mental energy deciding what to cook each week. EzDinner already holds the data to automate this — dish ratings, usage history, and frequency patterns — but offers no suggestions. This change adds rule-based suggestions as a foundation for future AI-driven recommendations.

## What Changes

- New endpoint to suggest dishes for a full week (7 days starting Monday)
- New endpoint to suggest a dish for a single day
- Both endpoints support "reroll" — re-requesting returns a different suggestion
- Suggestion engine scores dishes using rule-based heuristics:
  - Days since a dish was last used
  - How that compares to the dish's typical rotation frequency (how often it normally appears)
  - Whether consecutive-day usage is historically common for this dish (leftovers pattern)
  - Average family rating
- New frontend panel/button on the plan page to accept or reroll suggestions per-day and for the full week

## Capabilities

### New Capabilities

- `suggest-week`: Generate a full-week dinner suggestion for a family; supports rerolling the entire week
- `suggest-day`: Generate a single-day dinner suggestion for a family; supports rerolling a single day

### Modified Capabilities

_(none — no existing spec requirements are changing)_

## Impact

- **Backend**: New Azure Functions (`SuggestWeek`, `SuggestDay`), new query service (`IDinnerSuggestionService`), new domain-level scoring value objects (`DishScore`, `SuggestionContext`)
- **Frontend**: New composable (`useDinnerSuggestions`), new components (`PlanSuggestionBar`, `PlanSuggestedDinner`) on the plan page
- **Database**: Read-only — suggestions are computed from existing Dinner and Dish data; no new containers needed
- **APIs**: Two new GET endpoints; no breaking changes to existing endpoints
