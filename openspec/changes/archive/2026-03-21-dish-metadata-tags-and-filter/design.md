## Context

The dish catalog (`/dishes`) currently shows each dish in a grid of `DishCard` components. Cards display name, rating, and usage stats — but not the rich metadata (roles, effort, season, cuisine) that was added in a prior feature and is already returned by the API. A legacy `Tag` type (`{ value: string; color: string }`) exists in the type model and is scaffolded onto `Dish`, `DishSelector`, and `Dinner`, but was never wired to any UI and was never populated by the backend.

## Goals / Non-Goals

**Goals:**
- Surface dish metadata visually on each DishCard as compact, read-only tag pills
- Enable catalog filtering by metadata values without requiring a backend query — filter entirely client-side on the already-loaded dish list
- Clean up the dead `Tag` code to avoid confusion

**Non-Goals:**
- Editing metadata from the card (that belongs on the dish detail page)
- Backend filtering API changes
- Adding new metadata fields

## Decisions

### 1. Client-side filtering
Filter the already-loaded `allDishes` array reactively in the existing `dishes` computed property. No new API calls needed — all metadata is already present on `Dish` objects returned by `/api/dishes/family/:id`.

### 2. Filter state: multi-select per dimension
Each metadata dimension (role, effort, season, cuisine) holds a `Set` of active filter values. A dish passes the filter if it matches all active dimensions (AND across dimensions, OR within a dimension). This matches how tag filtering works in most catalog UIs. Using a `ref<Set>` makes toggle logic simple.

### 3. Tag display: inline pill strip on DishCard
Add a wrapping flex row of small chips below the stat caption on DishCard. Only show tags for fields that are set. Use distinct but subtle colors per dimension to give visual differentiation without overwhelming the card. Tags are non-interactive on the card (clicking navigates to dish detail as before).

### 4. Filter UI: collapsible chip group below search bar
Add a second chip row on the catalog page (below the sort chips) for filter chips. Group chips by dimension with a subtle separator label, or render them flat in a single row. Flat single row is simpler and sufficient at the number of values involved.

Active filters show as filled/colored chips; inactive as outlined. Tapping a chip toggles it. A "Clear filters" link appears when any filter is active.

### 5. Remove legacy Tag type
`Tag` and `tags` fields are removed from `Dish`, `DishSelector`, and `Dinner`. The field was never populated by the API or stored in CosmosDB (the backend never returned it with data). Removing it eliminates dead scaffolding. Test fixtures with `tags: []` are updated accordingly.

## Risks / Trade-offs

- [Risk] Dishes without metadata (e.g. created before AI enrichment) will show no tags and won't appear in filtered results when a filter is active → Mitigation: this is correct behavior; filter should mean "has this property". Empty metadata dishes are simply excluded when a filter is active.
- [Risk] Cuisine is a free-text field — same cuisine may appear with different capitalizations → Mitigation: build the cuisine filter option list from distinct values in the loaded dish set, normalized to consistent case. Dedup before rendering.

## Migration Plan

No data migration needed. The `tags` field was never populated in CosmosDB — removing it from the frontend type is a safe cleanup. Deploy frontend only.
