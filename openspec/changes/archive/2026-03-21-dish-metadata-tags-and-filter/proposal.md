## Why

Dish metadata (role, effort, season, cuisine) is collected and stored but never surfaced in the dish catalog, making it invisible to users who are scanning or browsing dishes. A legacy `Tag` feature was partially implemented but never finished and should be replaced with metadata-driven tags.

## What Changes

- Display dish metadata fields (roles, effort level, season affinity, cuisine) as compact visual tags on each `DishCard` in the dish overview
- Add filter chips to the dish catalog page so users can filter by any metadata value (e.g. show only "Quick" or "Main" dishes)
- **BREAKING**: Remove the old `Tag` type and `tags: Tag[]` fields from `Dish`, `DishSelector`, and `Dinner` — these were never implemented in the UI and are replaced by metadata tags

## Capabilities

### New Capabilities

- `dish-metadata-tags`: Show dish metadata as visual tags on DishCard and support filtering the catalog by metadata values

### Modified Capabilities

- `dish-metadata`: Remove the legacy `Tag`/`tags` fields from the dish type model

## Impact

- `web/components/Dish/DishCard.vue` — add metadata tag row
- `web/pages/dishes/index.vue` — add filter state, filter chips, update filtering logic
- `web/types/index.ts` — remove `Tag` interface and `tags` fields from `Dish`, `DishSelector`, `Dinner`
- `web/tests/repository/dishes-repository.test.ts` — remove `tags: []` from test fixtures
- No backend changes required — metadata is already returned by the API
