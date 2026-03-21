## 1. Type Cleanup

- [x] 1.1 Remove `Tag` interface from `web/types/index.ts`
- [x] 1.2 Remove `tags: Tag[]` field from `Dish`, `DishSelector`, and `Dinner` interfaces in `web/types/index.ts`
- [x] 1.3 Remove `tags: []` from test fixtures in `web/tests/repository/dishes-repository.test.ts`

## 2. Metadata Tags on DishCard

- [x] 2.1 Add a metadata tag row below the stat caption in `DishCard.vue` — render one chip per set metadata field (roles, effort, season, cuisine)
- [x] 2.2 Use icons and short labels matching the existing metadata display (e.g. `mdi-lightning-bolt` for Quick, `mdi-weather-sunny` for Summer)
- [x] 2.3 Show nothing when no metadata fields are set (no empty row)

## 3. Filter State and Logic

- [x] 3.1 Add filter state in `web/pages/dishes/index.vue` — a `ref<Set>` per dimension (roles, effortLevel, seasonAffinity, cuisine)
- [x] 3.2 Extend the `dishes` computed property to apply active metadata filters (AND across dimensions, OR within a dimension)
- [x] 3.3 Derive available filter options from the loaded `allDishes` list (distinct values per dimension; normalize cuisine casing)

## 4. Filter UI

- [x] 4.1 Add filter chip rows to `web/pages/dishes/index.vue` below the sort chips — one chip per available metadata value
- [x] 4.2 Style active filter chips as filled/colored and inactive as outlined; use a click handler to toggle
- [x] 4.3 Show a "Clear filters" link/button when any filter is active; clicking it resets all filter sets
- [x] 4.4 Hide a filter dimension's chips entirely when no dishes in the current list have that dimension set
