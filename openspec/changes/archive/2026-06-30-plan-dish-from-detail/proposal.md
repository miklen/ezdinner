## Why

When planning dinners for the coming week, users browse the dish catalog and want to schedule a dish they're viewing without navigating away to the plan page. The current flow breaks context — you lose your place in the catalog.

## What Changes

- Adds a "Plan dish" action button to the dish detail page action row (alongside Archive and Wish List)
- Opens a week picker dialog/sheet showing the coming weekend + next full week
- Shows existing planned dishes per day for context (informational only — multiple dishes per day are allowed)
- Adds the dish to the selected day's dinner menu via the existing `addDishToMenu` API
- Confirms success inline (snackbar) without navigating away

## Capabilities

### New Capabilities

- `plan-dish-from-detail`: Plan a dish directly from the dish detail page by picking a day from the coming week

### Modified Capabilities

<!-- None — dish detail page gets a new action; existing dinner planning APIs are unchanged -->

## Impact

- **Frontend**: `web/pages/dishes/[id].vue`, new `Dish/PlanDishDialog.vue` component
- **API**: No changes — uses existing `dinnerRepo.addDishToMenu()` and `dinnerRepo.getRange()`
- **Mobile**: Dialog renders as a bottom sheet for native app feel
- **Desktop**: Renders as a compact dialog
