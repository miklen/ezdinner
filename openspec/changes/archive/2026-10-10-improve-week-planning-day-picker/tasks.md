# Tasks

## 1. Align initial selected week

- [x] 1.1 Reuse `useWeekNav` initialization in `useWeekPlanning` while preserving explicit `initialMonday`; add fixed local-date cases to `web/tests/composables/week-planning.test.ts` for Monday-Friday, Saturday, Sunday, and the 2027-01-02 year boundary, and verify all expected Mondays and nine-day windows pass.
- [x] 1.2 Make the calendar fixtures in `web/tests/components/week-planning.test.ts` deterministic rather than dependent on the execution date; verify existing workspace tests pass with a frozen weekday and a frozen weekend and explicit-Monday tests retain their supplied date.

## 2. Reuse responsive picker presentation

- [x] 2.1 Extract the existing dialog/sheet shell from `Dish/PlanDishDialog.vue` into a focused presentational component under `web/components/Plan/` with typed open/title/close-label props, open-state event, and navigation/body/footer slots; migrate the detail picker and add focused tests verifying desktop dialog, mobile sheet, open/close behavior, and preserved date navigation/add/remove actions.
- [x] 2.2 Adopt the shared shell in `PlanningAssignmentPicker.vue`, preserving `PlanDayRow`, weekend tinting, Monday divider, saved-dish pills, opt-out notes, and mutation guards; add component tests verifying the same nine dates and dish actions at both breakpoints and readable long-name content with reachable close/navigation controls.
- [x] 2.3 Extend `PlanDayRow` with an already-assigned indicator/label mode for the workspace's disabled duplicate rows while retaining the detail picker's toggle/remove default; verify focused row tests show truthful accessible labels and no removal affordance for disabled duplicate assignments.

## 3. Navigate inside the open assignment picker

- [x] 3.1 Add selected-Monday input and typed navigation events to `PlanningAssignmentPicker`, render shared `Plan/WeekNav`, and wire it to `planning.navigate` in `Workspace`; retain `pickerDish` on Monday changes while clearing obsolete opt-out confirmation, and verify workspace interaction tests cover previous/next/current-week controls, the same dish remaining open, synchronized overview/targets, historical dates, and clearing only out-of-window selected days.
- [x] 3.2 Add distinct loading and retry contracts to the picker; use shared skeleton rows during reads, localized read-error retry feedback, and `canMutate` gating. Verify tests cover pending/failed reads, successful retry, navigation with superseded responses, and assignment saved only to the selected loaded date.
- [x] 3.3 Prevent week navigation during an active save using a backward-compatible disabled navigation prop if needed, including swipe and center actions; keep navigation available during reads. Verify shared-navigation and workspace tests cover these states while existing Plan/detail navigation remains enabled by default.
- [x] 3.4 Verify navigation retains explorer search/filter/sort, browsing position, and recommendation conversation/results; add interaction regression cases for reopening/cached returns, duplicate prevention, opt-out confirmation, family/account changes while open, additive assignment, and safe undo.

## 4. Localize the completed interaction

- [x] 4.1 Reuse existing translation keys where possible and add every new visible/accessibility string to both `web/i18n/locales/en.json` and `da.json`; extend `web/tests/week-planning-locales.test.ts` and picker tests to verify live locale switching updates titles, week/date labels, close labels, and loading/retry feedback while retaining the selected dish/week.

## 5. Verify integrated behavior and visual consistency

- [x] 5.1 Run frontend `npm test`, `npm run lint`, and `npm run build` from `web/` after required Nuxt preparation; verify all checks pass and record their results and any relevant limitations.
- [x] 5.2 Compare rendered Plan your week assignment against `Dish/PlanDishDialog` and Plan week navigation on desktop and mobile, in English and Danish: verify matching typography, spacing, colors, controls, skeleton/error/empty/planned/opted-out states, all nine dates, mobile scroll and safe-area behavior, keyboard/focus return, and preserved `/dishes/:id` navigation. Record evidence and explicitly state any unavailable browser verification.
