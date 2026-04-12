## Why

The primary planner in a family currently splits her planning session across two browser tabs — one for the dish catalog (to browse by last-used) and one for the week plan (to assign dishes to days). The plan page's right panel shows a wishlist in isolation and the suggestion bar surfaces a small algorithmic selection, but neither combines the signals she actually needs simultaneously: freshness (last used), family demand (wish votes), and effort level (for matching busy vs. relaxed days). Consolidating these into a single planning surface eliminates the two-tab workflow and brings the app in line with how planning actually happens.

## What Changes

- **Right panel on the plan page replaced** — the wishlist panel is retired from `/plan`; replaced by a unified Planning Assistant panel
- **Planning Assistant panel added** — scrollable, searchable list of all ~50-60 active dishes sorted by last-used (longest first), showing wish vote indicator + effort level inline; supports direct day-assignment from the panel
- **AI week planner added** — a "Plan this week" action in the panel that calls Claude Haiku with dish signals (freshness, wishes, effort) plus a freetext context input from the planner; returns a full-week draft she can accept/edit before committing
- **Suggestion bar retired** — the per-day suggestion bar above the plan is removed; the Planning Assistant panel covers this function more comprehensively
- **Wishlist management relocated** — viewing and managing wishes moves to the dish catalog and dish detail pages; the plan page only displays wish signal, not manages it
- **Dish picker sort order fixed** — the autocomplete inside expanded dinner cards is sorted by last-used descending (longest first) instead of unordered; effort level added to dish row display

## Capabilities

### New Capabilities
- `planning-assistant-panel`: Unified right-panel component on the plan page — browsable dish list with freshness + wish + effort signals, direct day-assignment, and AI-powered week plan generation

### Modified Capabilities
- `wish-list`: Display of wish list on the plan page is removed; wish signal (vote count) is surfaced read-only inside the Planning Assistant panel instead; full management remains on dish catalog and dish detail pages
- `suggest-week`: The per-day suggestion bar UI is retired; the AI week planner replaces it as the primary "suggest for me" surface; the backend scoring endpoint may be reused as a data source for the AI prompt context

## Impact

- `web/pages/plan.vue` — removes `PlanWishList` from support slot, removes `PlanSuggestionBar`, adds `PlanAssistantPanel`
- `web/components/Plan/AssistantPanel.vue` — new component (primary deliverable)
- `web/components/Plan/PlannedDinnerDetails.vue` — sort `filteredDishes` by last-used; add effort level to `PlanDishRow`
- `web/components/Plan/DishRow.vue` — add effort level display
- `web/components/Plan/WishList.vue` — retired from plan page (component may remain for future reuse)
- `web/components/Plan/SuggestionBar.vue` — retired
- `api/src/EzDinner.Functions` — new function for AI week plan generation (calls Claude Haiku)
- `api/src/EzDinner.Application` — new command/query for AI week plan; dish list query needs last-used + effort + wish data in one response
- i18n: new keys in `en.json` and `da.json` for panel labels, effort filter, AI planner UI
