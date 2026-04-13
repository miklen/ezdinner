## Context

`Plan/AssistantPanel.vue` currently receives `weekStart: DateTime` and `dinners: Dinner[]` as required props from `plan.vue`, which owns the week-navigation state. The panel cannot stand alone on another page without duplicating that state and data-loading logic.

The dishes index page (`pages/dishes/index.vue`) uses a full-width catalog layout with no sidebar. It loads dishes and dish stats but does not load dinners or wishlist data.

The plan page uses a `<Content split>` component for the two-column layout and a FAB + bottom-sheet for mobile. This pattern should be replicated on the dishes page.

## Goals / Non-Goals

**Goals:**
- Make `AssistantPanel` usable on any page by making `weekStart` optional — when omitted, the panel renders its own week-navigation
- Add the assistant panel (sidebar + FAB/sheet) to the dishes index page
- Load the required data (dinners, wishlist, dish stats) on the dishes page only when needed
- Keep the plan page behavior exactly as-is (plan.vue continues to own weekStart and passes it in)

**Non-Goals:**
- Changing any panel feature behavior (AI planner, wishlist mode, dish rows, filters)
- Moving the panel to the dish detail page (`dishes/[id].vue`)
- Synchronizing the week selection between plan and dishes pages across sessions

## Decisions

### Decision 1: Optional weekStart prop with internal fallback

`weekStart` becomes `optional` in the panel's props. When it is `undefined`, the panel renders `PlanWeekNav` internally and manages its own `weekStart` ref.

**Alternative considered**: Create a wrapper component `AssistantPanelStandalone` that adds week-nav and passes it down. Rejected — doubles the component surface for a trivial prop change.

**Rationale**: The panel already imports `PlanWeekNav` indirectly through the plan page; making the prop optional is minimal and keeps one component.

### Decision 2: Extract `useWeekNav` composable

Week-navigation state (defaulting to next week on Sat/Sun, the `weekStart` ref) is duplicated between `plan.vue` and the panel's new internal mode. Extract it to `composables/useWeekNav.ts` so both callers share the same defaulting logic.

**Alternative considered**: Inline the logic in both places. Rejected — duplicated logic with identical business rules (Sat/Sun → next week default) is a maintenance hazard.

### Decision 3: Dishes page loads dinner/wishlist data lazily (on panel open)

The dishes page does not load dinners or wishlist by default — it is a catalog, not a planning surface. Data for the assistant panel is loaded when the user first opens the panel (FAB tap on mobile, or the sidebar renders on desktop).

**Alternative considered**: Eagerly load all data on page mount. Rejected — adds unnecessary latency and CosmosDB reads for users who only browse the catalog.

**Implementation**: On desktop, load panel data once after `onMounted` (sidebar is always visible). On mobile, load on first FAB tap.

### Decision 4: Keep component in `Plan/` folder, rename to reflect shared use

Move `Plan/AssistantPanel.vue` → `Plan/AssistantPanel.vue` (no move needed). The `Plan/` folder already contains shared planning UI components used by the plan page. Adding cross-page usage does not require restructuring.

**Alternative considered**: Move to `components/Shared/` or `components/Assistant/`. Rejected — the component is deeply tied to planning domain concepts; the `Plan/` folder is the correct home.

## Risks / Trade-offs

- **Dinner data staleness on dishes page**: When the user assigns a dish from the dishes page, the panel refreshes correctly, but the dish catalog grid does not update (it doesn't show planned status). This is acceptable — the dishes page is a catalog, not a plan view.
  → Mitigation: No action needed; the catalog grid has no concept of "planned" state.

- **Double data loading when both pages are visited in the same session**: Pinia stores (`dinnersStore`, `wishlistStore`) cache their data. If the user navigates between plan and dishes, data is not re-fetched unless the week changes.
  → Mitigation: Rely on existing store caching; no extra work needed.

- **Week picker state not persisted**: The dishes-page panel starts at the current/next week default on every mount. Users cannot bookmark a specific week from the dishes page.
  → Accepted trade-off: consistent with the plan page behavior on fresh load.

## Migration Plan

1. Extract `useWeekNav` composable
2. Make `weekStart` prop optional in `AssistantPanel`; add internal week-nav when prop is absent
3. Update `plan.vue` to use `useWeekNav` (no visible behavior change)
4. Add assistant panel + data loading to `dishes/index.vue`
5. Add i18n keys for any new strings (none anticipated — panel strings are already translated)

No backend changes. No database migrations. Rollback: revert the four frontend file changes.
