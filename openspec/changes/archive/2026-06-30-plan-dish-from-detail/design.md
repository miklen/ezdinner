## Context

The dish detail page (`web/pages/dishes/[id].vue`) already has an action row with Archive and Wish List buttons. Planning a dish from this page is a natural extension — the `dinnerRepo.addDishToMenu(familyId, date, dishId)` API already exists. The only missing piece is a date-picking UI with context (what's already planned on each day).

The primary use case is weekend planning: the user browses the catalog on Saturday or Sunday and wants to schedule a dish for the coming week without navigating to the plan page and losing their place.

## Goals / Non-Goals

**Goals:**
- Let users plan a dish directly from the detail page with minimal friction
- Show what's already planned per day so the user can make informed choices
- Feel native on mobile (bottom sheet) and polished on desktop (dialog)
- Lazy-load dinner data — no cost unless the user opens the picker

**Non-Goals:**
- Week navigation in the picker (fixed window only — not a full calendar)
- Removing dishes from the plan via this UI
- Opt-out / skip-day functionality from this UI
- Backend changes — all existing APIs are reused as-is

## Decisions

### New component: `Dish/PlanDishDialog.vue`

Encapsulates the entire picker: date range computation, dinner fetch, day list, and add-to-menu call. Receives `dishId` and `familyId` as props; emits `close` and `planned` (date, dishId). This keeps `[id].vue` clean.

**Alternative considered:** Inline dialog in `[id].vue` — rejected because it would add significant state and template complexity to an already large page component.

### Planning window: coming Sat + coming Sun + next Mon–Sun

```
Computed from today (no time zone — LocalDate logic mirrors NodaTime on backend):

nextSaturday = today + daysUntil(Saturday)   // 0 if today IS Saturday
windowStart  = nextSaturday
windowEnd    = nextSaturday + 8 days          // always lands on a Sunday

Result: always a 9-day window, Sat → Sun
```

This satisfies the primary use case (Saturday planner gets today → next Sunday = 8 days of real reach) and keeps the logic dead simple with no branching on weekday vs weekend.

**Alternative considered:** Rolling 8 days from tomorrow — rejected because it produces an irregular window that doesn't align with the week boundaries users expect.

### Lazy dinner fetch on dialog open

Dinner data is fetched once when the picker opens, using `dinnerRepo.getRange(familyId, windowStart, windowEnd)`. Loading state is shown during the fetch. No refetch while the picker is open — stale-while-open is acceptable for this short-lived interaction.

**Alternative considered:** Fetch on dish detail page load — rejected because most users never open the picker; paying the fetch cost unconditionally would add latency to every dish detail view.

### Responsive: dialog (desktop) vs bottom sheet (mobile)

Uses Vuetify's `useDisplay` (`smAndDown`) to switch between `<v-dialog>` and `<v-bottom-sheet>` — identical to the pattern already used in `PlannedDinnerDetails.vue`. The dialog variant uses a fixed width (420px). The sheet variant uses `max-height: 80dvh` with a drag handle and safe-area bottom padding.

### Day row interaction: click to plan, stays open

Clicking a day row immediately calls `addDishToMenu`. No confirmation step — the snackbar is the feedback. The picker stays open so the user can add the dish to multiple days. The day row updates inline to show the newly added dish.

**Alternative considered:** Close on select — rejected because the primary mental model is "plan this dish for several days" (e.g., cook once, eat twice).

### Existing dishes displayed as pill row per day

Each day row shows a compact horizontal list of dish names already planned. This uses `DishPill` in read-only mode (no remove). If there are no dishes, a subtle "free" label is shown in muted color.

### "Plan dish" button placement and styling

Added to the `dish-detail__archive-action` flex row in `[id].vue`, after the Wish List button. Styled consistently with the Wish List button (outlined, primary color) but with a calendar-plus icon (`mdi-calendar-plus`). Hidden when dish is archived.

## Risks / Trade-offs

- **Stale dinner data in picker** → Acceptable; user is planning, not reviewing live state. Could add a refresh button in v2 if needed.
- **No undo for adding to plan** → Consistent with the rest of the app (plan page also has no undo). Remove is always possible from the plan page.
- **9-day window may feel limiting for far-future planning** → Out of scope by design; the use case is next-week planning only.

## Open Questions

- Should successfully planned days show a checkmark/highlight in the picker row so the user can see at a glance which days already have this specific dish? (Nice-to-have, can be added in v2)
