# Proposal

## Why

Plan your week starts on the current week even on Saturday and Sunday, when families are preparing the following week. Its dish assignment picker has no week navigation and uses a generic dialog rather than the established planning dialog and mobile sheet presentation.

## What Changes

- Initialize Plan your week to the current Monday on Monday-Friday and the following Monday on Saturday-Sunday, matching the existing Plan flow.
- Add the shared previous/next week controls inside the assignment picker, with the same week labels and return-to-current-week interaction used elsewhere.
- Keep the picker open with the same dish while navigating; update the workspace overview and assignment dates together through their existing shared selected Monday.
- Preserve the nine-day window, including past dates when browsing previous weeks, and retain browsing context, additive assignment, duplicate prevention, opt-out confirmation, and safe undo.
- Match the existing dish planning presentation: centered desktop dialog, mobile bottom sheet, shared day rows, spacing, typography, loading states, and English/Danish localization.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `dish-first-week-planning`: Specify weekday/weekend initialization, week navigation from the open assignment picker, and responsive visual consistency with the existing planning controls.

## Impact

- Frontend: `web/composables/useWeekPlanning.ts`, existing `useWeekNav.ts` initialization, `web/components/WeekPlanning/Workspace.vue`, and `PlanningAssignmentPicker.vue`.
- Reuse `Plan/WeekNav.vue`, `Dish/PlanDayRow.vue`, and the presentation of `Dish/PlanDishDialog.vue`; extract a focused shared presentation component/styles if needed to avoid duplicated shells.
- Extend existing composable, workspace interaction, and locale tests. Include desktop/mobile browser comparison in implementation validation.
- No API, backend, persistence, authorization, or dependency changes are expected. The existing Plan and dish-detail workflows keep their behavior; their separate `plan-dish-from-detail` specification is outside this change.
