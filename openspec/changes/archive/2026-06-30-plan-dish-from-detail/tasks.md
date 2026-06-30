## 1. New PlanDishDialog component

- [x] 1.1 Create `web/components/Dish/PlanDishDialog.vue` with props: `dishId: string`, `dishName: string`, `familyId: string`, `modelValue: boolean`; emits `update:modelValue`, `planned`
- [x] 1.2 Implement `computePlanningWindow()` utility: returns array of `DateTime` from next Saturday through the Sunday 8 days later (9 days total), excluding any dates before today
- [x] 1.3 Implement lazy dinner fetch on dialog open using `dinnerRepo.getRange(familyId, windowStart, windowEnd)`; show loading skeleton while fetching
- [x] 1.4 Render day list: one row per date showing day name, formatted date, and existing dish names as read-only `DishPill` components (or a "free" muted label if none)
- [x] 1.5 Implement `addToDay(date)`: calls `dinnerRepo.addDishToMenu`, updates local dinner state inline, shows success snackbar with dish name and date; picker stays open
- [x] 1.6 Add i18n keys for all user-visible strings (EN + DA): button label, dialog title, snackbar message, "free" label, loading state, close button
- [x] 1.7 Wire responsive layout: `<v-dialog width="420">` on desktop (`!smAndDown`), `<v-bottom-sheet max-height="80dvh">` on mobile (`smAndDown`) with drag handle and safe-area bottom padding

## 2. Dish detail page integration

- [x] 2.1 Add `planDishDialog` ref (`shallowRef(false)`) to `web/pages/dishes/[id].vue`
- [x] 2.2 Add "Plan dish" button to the `dish-detail__archive-action` flex row, after the Wish List button; hide when `dish.isArchived`; style consistent with Wish List button (outlined, primary, `mdi-calendar-plus` icon)
- [x] 2.3 Mount `<DishPlanDishDialog>` in the template, bound to `planDishDialog`, passing `dishId`, `dishName`, and `familyId`

## 3. Visual design polish

- [x] 3.1 Style day rows: subtle hover state, clear visual separation between weekend rows (Sat/Sun) and weekday rows
- [x] 3.2 Ensure "Plan dish" button matches the visual rhythm of Archive/Wish List buttons (same height, spacing, font-size)
- [x] 3.3 Verify dialog and bottom sheet feel native: dialog has rounded corners and correct shadow; sheet has rounded top corners, drag handle, smooth open/close animation

## 4. Verify

- [x] 4.1 Test on desktop: open picker, verify dinner data loads, add dish to a free day and a day with existing dishes, confirm snackbar appears and picker stays open
- [x] 4.2 Test on mobile viewport: verify bottom sheet opens, scrolls correctly, safe-area padding works, dish adds correctly
- [x] 4.3 Verify "Plan dish" button is hidden when viewing an archived dish
- [x] 4.4 Verify all strings appear correctly in both EN and DA locales
