## Why

The suggestion engine rewards dishes with high `DaysSinceLast` — dishes that have fallen out of favor accumulate enormous overdue scores and keep surfacing as top suggestions indefinitely. Explicit archiving gives families a clean, intentional way to remove dishes from rotation without losing the historical record of when they were used.

## What Changes

- Dishes can be **archived**: removed from suggestions and hidden from the active catalog
- Dishes can be **reactivated**: restored to full active status at any time
- Archived dishes remain visible in the catalog via a "Show archived" toggle
- Archived dishes are visually distinct (muted) when shown
- Suggestion candidates are filtered to exclude archived dishes
- Dish catalog queries exclude archived dishes by default, with opt-in to include them
- Past dinner records are unaffected — archived dishes still appear in history

## Capabilities

### New Capabilities

- `dish-archive`: Ability to archive and reactivate dishes; archive state filters dishes from suggestions and catalog

### Modified Capabilities

- `suggest-day`: Suggestion candidates must exclude archived dishes
- `suggest-week`: Suggestion candidates must exclude archived dishes

## Impact

- **Domain**: `Dish` aggregate gains `IsArchived` property, `Archive()` and `Reactivate()` methods
- **Application**: Two new command handlers — `ArchiveDishCommand`, `ReactivateDishCommand`
- **API**: Two new endpoints — `PATCH /api/families/{familyId}/dishes/{dishId}/archive` and `.../reactivate`
- **Query**: `DinnerSuggestionService` filters archived dishes from candidates; dish catalog query gains `includeArchived` parameter
- **Frontend**: Archive/reactivate actions on dish detail; "Show archived" toggle in catalog; muted visual treatment for archived dishes
