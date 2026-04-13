## Why

The Planning Assistant panel is currently only accessible from the plan page, forcing users to context-switch away from the dish catalog when they want to plan a week. Planners who browse dishes should be able to assign dishes to days without leaving the catalog.

## What Changes

- Extract `PlanAssistantPanel` into a reusable `AssistantPanel` component that accepts `weekStart` and `dinners` as props but owns its own week-navigation when not provided externally
- Add a `WeekPicker` composable (or embed week-navigation state inside the panel) so the assistant can stand alone outside the plan page
- Embed the assistant panel on the dishes index page in the same sidebar layout used by the plan page (desktop: side-by-side; mobile: FAB + bottom sheet)
- The dishes page must load the data the assistant needs (dinners for the selected week, dish stats, wishlist) when the panel is open
- Keep the existing `PlanAssistantPanel` usage on the plan page — the plan page continues to own `weekStart` and passes it in; the panel does not duplicate week navigation there

## Capabilities

### New Capabilities

- `assistant-panel-shared`: Refactor `Plan/AssistantPanel.vue` into a shared component (`Shared/AssistantPanel.vue` or `Assistant/Panel.vue`) with an optional `weekStart` prop; when omitted, the panel owns its own week navigation internally

### Modified Capabilities

- `planning-assistant-panel`: The panel now renders on both plan and dishes pages; week navigation is available within the panel itself when used standalone

## Impact

- `web/components/Plan/AssistantPanel.vue` — refactored/moved; `weekStart` prop becomes optional
- `web/pages/plan.vue` — continues to pass `weekStart` to the panel (no behavior change)
- `web/pages/dishes/index.vue` — gains assistant panel sidebar + FAB/bottom-sheet for mobile, plus dinner/wishlist data loading
- `web/components/Plan/AssistantDishRow.vue` — no change needed (already self-contained)
- `web/components/Plan/WishList.vue` — no change needed
- Possibly a new `useWeekNav` composable to share week-navigation logic between plan.vue and the standalone panel
