# Design

## Context

See `proposal.md` for motivation and `specs/dish-first-week-planning/spec.md` for acceptance behavior.

Observed implementation:
- `web/pages/plan-your-week.vue` composes `WeekPlanning/Workspace.vue`; the workspace shares one `useWeekPlanning` instance between the overview, assignment picker, planned-date indicators, and recommendation context.
- `useWeekPlanning` defaults to `DateTime.local().startOf('week')`. `useWeekNav` already selects the following Monday on Saturday/Sunday and is used by the existing Plan and dish-detail planning flows.
- `PlanningWeekOverview` uses `Plan/WeekNav.vue`. `PlanningAssignmentPicker` receives dates but no Monday or navigation contract, uses a generic dialog at all widths, and has no loading presentation distinct from mutation disabling.
- `Workspace` closes `pickerDish` on every Monday change, which would immediately close a picker containing new week controls.
- `useWeekPlanning` already loads nine dates, invalidates superseded reads, clears saved menus on navigation, gates writes through `canMutate`, and clears selected targets only when outside the window.
- `Dish/PlanDishDialog.vue` already has the desired header, week controls, divider, skeleton rows, centered desktop dialog, and mobile sheet with handle, scroll container, and safe-area padding. It owns separate persistence and add/remove behavior, so using it wholesale would bypass the workspace's duplicate, opt-out, conflict, and undo rules.

The existing `dish-first-week-planning` spec already requires one synchronized nine-day window and historical dates; it does not define initial Monday or in-picker navigation. `plan-dish-from-detail` has older coming-week/no-past wording that differs from its current navigable implementation. This change explicitly targets Plan your week and does not reinterpret or update that separate contract.

Planning inspection was source-based: the browser runtime failed to initialize, so no rendered desktop/mobile comparison was possible during proposal preparation.

## Goals / Non-Goals

**Goals:**
- One owner for selected Monday, current dinner data, and mutation safety.
- Reuse the existing calendar default and shared visual components with typed props/events and derived dates.
- Keep responsive presentation independent of persistence so both assignment workflows can share it safely.

**Non-Goals:**
- Backend/domain model changes, API changes, new week preference persistence, or live midnight/weekend rollover of an already-open workspace.
- Redesigning dish browsing, recommendation results, overview cards, or the separate dish-detail assignment rules.

## Decisions

### Reuse weekday/weekend initialization only at workspace creation

Initialize `useWeekPlanning` from the existing `useWeekNav` default and convert the Monday to a non-null ISO calendar string with `toFormat('yyyy-MM-dd')`. Keep an explicit `initialMonday` authoritative for callers/tests. After initialization, `useWeekPlanning.monday` remains the only selected-week state. Reopening the picker and cached route activation continue to use it.

Alternative: duplicate the weekend conditional in `useWeekPlanning`. Rejected because the existing Plan and workspace defaults could drift. A continuously computed default was also rejected because it could override a user's manual selection.

### Route picker navigation through the workspace's existing selected Monday

Add a `monday` prop and a typed `navigate(offset)` event to `PlanningAssignmentPicker`, using `Plan/WeekNav.vue` and the overview's existing offset conversion pattern. Add a distinct `loading` prop and retry event for failed reads. `Workspace` wires these to `planning.monday`, `planning.navigate`, `planning.loading`, and `planning.refresh`.

Change the Monday watcher to clear obsolete pending opt-out confirmation while retaining `pickerDish`. Family/account changes still close the picker and reset scoped state. Keep the existing selected-date clearing behavior and recommendation-context invalidation; navigation does not silently rerun or reorder recommendations.

Alternative: give the picker a separate week and fetch its own dinners. Rejected because the overview and assignment window would disagree, and the workspace mutation guard would reject dates outside its selected window.

### Share presentation rather than assignment orchestration

Extract a focused presentational picker shell from `Dish/PlanDishDialog.vue`, under `web/components/Plan/`, and migrate both pickers to it. The shell owns breakpoint-based dialog/sheet presentation, header/close control, handle, safe-area padding, maximum height, and scrolling. Accept typed open/title/close-label props, emit the open-state change, and expose navigation, body, and optional footer slots. Put shell styling in that shared component, derived from the current dialog/sheet and `web/assets/global.scss` tokens.

The workspace picker supplies `WeekNav` and its existing `PlanDayRow` entries through the shell. Render skeleton day rows while `loading`; show localized retry feedback on failed reads and retain mutation disabling while busy. If week switching during a save can change its scope, add a focused optional `disabled` prop to shared `WeekNav` (default false), disable navigation only for the active mutation, and allow it during reads. Preserve all existing detail-picker persistence and behavior when adopting the shell.

Do not display a removal affordance for a disabled duplicate assignment in the workspace: extend `PlanDayRow` with a focused mode for its already-assigned indicator and accessible label while preserving the detail picker's toggle/remove default. Retain weekend accents and the divider before Monday; maintain existing overview today badges and selection states.

Alternative: copy the detail dialog CSS into `PlanningAssignmentPicker`. Rejected because independently styled instances of the same planning concept can drift. Replacing workspace persistence with `PlanDishDialog` is also rejected because it would lose existing mutation guarantees.

### Preserve loaded-window safety and calendar semantics

Keep `planningWindow`, generation checks, `loadedScope`, and `canMutate` as the authority for readable/savable state. New dates can appear immediately after navigation but cannot accept an assignment until their dinner read succeeds. A failed read must never turn unknown saved decisions into apparently empty actionable days. Retry refreshes the current window. Only `WeekNav` changes the selected Monday, and its center action continues to return to the actual current Monday, including on weekends.

Alternative: recalculate weekday/weekend defaults every time the user opens the picker or presses the center label. Rejected because those interactions already have selection-preservation and return-to-current-week meanings.

### Validate actual behavior with fixed calendar dates and shared component coverage

Extend `web/tests/composables/week-planning.test.ts` to freeze local calendar time for all seven weekdays and a year boundary. Preserve explicit-Monday tests. Update `web/tests/components/week-planning.test.ts` to freeze time instead of deriving fixtures from an uncontrolled real date, and exercise in-picker navigation, selected-dish continuity, synchronization, loading/retry, late reads, duplicates, opt-out confirmation, and context preservation. Add focused shared-shell tests for both breakpoints and detail-picker behavior. Extend `web/tests/week-planning-locales.test.ts` for any new keys in both locale files.

Automated tests verify interaction and scope; browser comparison verifies typography, controls, spacing, scroll behavior, and focus on desktop/mobile. Both are needed for completion.

## Risks / Trade-offs

- [Shared shell extraction affects the existing detail picker] -> Preserve its control and persistence contracts and test opening, closing, navigating, and toggling dates at both breakpoints.
- [Retaining the picker makes concurrent week reads visible] -> Keep scope/generation guards and loading feedback; test superseded responses and failed reads before allowing writes.
- [A navigation action during save changes displayed context] -> Disable week navigation during mutations with a backward-compatible shared-control prop; reads remain navigable.
- [Generic duplicate rows imply removal] -> Use an already-assigned visual/accessible mode in the workspace while retaining detail-picker removal semantics.
- [Responsive polish cannot be proven by stubs] -> Compare with the rendered detail picker and Plan navigation at desktop and mobile sizes, in both locales; state any unavailable visual verification explicitly.

## Migration Plan

Deploy through the normal frontend pipeline after the tests, lint, build, and visual checks pass. No stored data or backend migration is required. Reverting the frontend commit restores the prior presentation and initial week behavior without affecting saved dinners.
