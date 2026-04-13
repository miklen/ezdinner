## 1. Extract useWeekNav composable

- [x] 1.1 Create `web/composables/useWeekNav.ts` that returns a reactive `weekStart` ref defaulting to current week's Monday, or next week's Monday when today is Saturday or Sunday
- [x] 1.2 Replace the inline week-defaulting logic in `web/pages/plan.vue` with `useWeekNav()`
- [x] 1.3 Verify plan page behaviour is unchanged after the refactor

## 2. Make AssistantPanel weekStart prop optional

- [x] 2.1 Change the `weekStart` prop in `web/components/Plan/AssistantPanel.vue` from required to optional (`weekStart?: DateTime`)
- [x] 2.2 Add an internal `weekStart` ref (via `useWeekNav()`) used when the prop is absent; derive a single `resolvedWeekStart` computed that picks prop over internal ref
- [x] 2.3 Conditionally render `<PlanWeekNav>` inside the panel when `weekStart` prop is not provided, wired to the internal ref
- [x] 2.4 Replace all existing references to `props.weekStart` in the panel with `resolvedWeekStart`
- [x] 2.5 Verify the plan page (which still passes `weekStart`) renders the panel without its own week nav and behaves identically to before

## 3. Add assistant panel to dishes index page

- [x] 3.1 Add the `<Content split>` layout to `web/pages/dishes/index.vue` so the dish catalog occupies the main slot and the assistant panel occupies the `#support` slot (desktop sidebar)
- [x] 3.2 Import and render `<PlanAssistantPanel>` in the `#support` slot (no `weekStart` prop — panel is standalone)
- [x] 3.3 Load dinner data and wishlist on mount for the dishes page: call `dinnersStore.populateDinners(...)` and `wishlistStore.fetchWishes()` after the panel-relevant stores are ready; re-fetch when the panel's selected week changes (watch the panel's exposed week or use a shared signal)
- [x] 3.4 Add FAB button (same style as plan page) visible only on mobile (`d-flex d-md-none`)
- [x] 3.5 Add `v-bottom-sheet` with `<PlanAssistantPanel>` for mobile, opened by the FAB; load dinner/wishlist data on first open if not already loaded
- [x] 3.6 Wire `@dish:assigned` on both desktop and mobile panel instances to refresh dinners for the selected week

## 4. Data loading strategy for dishes page

- [x] 4.1 Ensure `dishesStore.populateDishes()` and `dishesStore.populateStats()` are called on the dishes page init (required by the panel's dish list and effort data) — confirm they are not already called and add if missing
- [x] 4.2 Load dinners once on desktop mount; re-load when week changes in the standalone panel
- [x] 4.3 On mobile, load dinners on first FAB tap (guard with a `panelDataLoaded` flag to avoid duplicate fetches)

## 5. Verify and polish

- [x] 5.1 Confirm the dishes page sidebar is hidden on mobile and the FAB is hidden on desktop
- [x] 5.2 Confirm the plan page still passes `weekStart` and the panel does not render internal week navigation there
- [x] 5.3 Check that assigning a dish from the dishes page panel correctly updates the dinner list in the panel
- [x] 5.4 Run `npm run lint` and `npm test` in `web/` and fix any issues
