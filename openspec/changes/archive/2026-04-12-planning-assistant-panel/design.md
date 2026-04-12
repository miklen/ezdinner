## Context

The plan page currently has a two-column layout (`Content` component with `split` prop): the week plan fills the left column (~67%) and the right support column (~33%) shows `PlanWishList`. A `PlanSuggestionBar` sits above the week list, offering per-day algorithm-driven hints.

The planner's real workflow is:
1. Survey the week mentally (calendar constraints, who's home, busy days)
2. Open `/dishes` sorted by last-used → browse for dishes that are "due" AND match day constraints
3. Note wishes the family has expressed
4. Return to `/plan` to assign

The two-tab friction exists because no single surface combines freshness, wish demand, and effort signal in a browsable form. The plan page's dish picker (inside expanded dinner cards) has the right metadata fields already (`DishRow` shows `daysSince` + rating), but the list is unordered and missing effort level — making it search-only, not browse-friendly.

The `dishStats` (including `lastUsed`) are already attached to dish objects in `dishesStore.dishes` (consumed by `PlanDishRow`), so no new API calls are needed to enable sort-by-last-used.

The AI infrastructure is already available: `AnthropicClient` is registered in DI, `AnthropicEnrichmentProvider` demonstrates the Haiku call pattern (structured JSON prompt → parse response). A week planner function follows the same pattern.

## Goals / Non-Goals

**Goals:**
- Replace the wishlist panel + suggestion bar with a single Planning Assistant panel that combines all planning signals
- Enable dish-first assignment: browse the panel, pick a dish, assign it to a day — without leaving the plan page
- Add AI-powered full-week draft generation using Claude Haiku
- Sort the existing dinner-card dish picker by last-used; add effort level to dish row
- Preserve wishlist management accessibility (panel toggle)

**Non-Goals:**
- Calendar integration (app does not know the planner's schedule — effort/context input remains manual)
- Leftover tracking as a structured data concept (same dish assigned twice already works)
- Roles as a planning filter (mental model only, not needed in the UI)
- Changes to the suggestion scoring backend (the existing scoring engine is reused as a data source for AI context, not modified)
- Mobile planning UX (mobile is view-only; panel is desktop-only)

## Decisions

### Decision 1: Panel replaces right support slot; wishlist becomes a toggle within the panel

**Chosen:** The `PlanWishList` component is removed from the support slot. The new `PlanAssistantPanel` takes its place and contains two modes toggled by a tab/button at the panel top: **Plan** (default) and **Wishlist**. In Plan mode, full dish list with signals. In Wishlist mode, the existing wishlist management UI (reused component).

**Alternative considered:** Keep wishlist as a separate panel and add a third pane. Rejected — three panels at this screen width would be cramped and add visual noise. The wishlist is now primarily a *signal* that feeds into planning decisions; seeing it as a standalone list is a secondary use case.

**Alternative considered:** Move wishlist entirely to `/dishes`. Rejected — family members add wishes in the context of planning; removing it from the plan page creates unnecessary friction for non-planners.

### Decision 2: Sort dish picker in dinner cards by last-used descending

**Chosen:** `filteredDishes` in `PlannedDinnerDetails` is sorted by `dishStats.lastUsed` descending (longest ago first), with wished dishes still floating to the absolute top. Effort level is added to `PlanDishRow` as a small colored badge.

**Rationale:** No new API calls needed — `dishStats` is already present on dish objects in the store. This aligns the in-card picker with her primary signal, making it useful even when the panel is not in use.

### Decision 3: Day assignment via inline day-picker popover on each dish row

**Chosen:** Each dish row in the Planning Assistant panel has a `+` button. Clicking it expands an inline day-picker within the row showing the 7 days of the current week (short weekday name + date). Clicking a day assigns the dish and collapses the picker. Days that already have the dish planned show a checkmark; days with other dishes planned show a small dish-count indicator (but remain clickable — same dish twice is valid for leftovers).

**Alternative considered:** Staging model — click dish to "stage" it, then click a day card in the main column. Rejected — requires spatial coordination between two columns and loses the "stay in panel" flow.

**Alternative considered:** Drag-and-drop. Rejected — high implementation complexity, poor keyboard accessibility, and ambiguous affordance.

### Decision 4: AI week planner as a new backend Function calling Claude Haiku

**Chosen:** A new `POST /api/families/{familyId}/suggest/ai-week` Azure Function that:
1. Loads all active dishes with their last-used stats and wish data
2. Loads the current week's partial plan (already-assigned days)
3. Accepts a `weekStart` date and optional `context` string from the request body
4. Builds a Haiku prompt with the dish catalog as structured data + user context
5. Returns a JSON draft: array of `{ date, dishId, dishName, reason }` for unplanned days only

The frontend shows the draft in the panel as a preview with per-day accept/skip controls and a global "Apply all". Each accepted assignment calls the existing `dinnerRepo.addDishToMenu`.

**Prompt design:** Dish catalog is included as a compact table (name | effort | weeks-since-last | wish-votes). Haiku is asked to return ONLY a JSON array — same extraction pattern as `AnthropicEnrichmentProvider`. Prompt caching is applied to the system prompt block; the dish list is included in a `cache_control: ephemeral` block as it changes rarely per session.

**Token budget:** ~50-60 dishes × ~15 tokens = ~900 tokens for catalog, ~300 tokens system + context = ~1200 total input. Output is ~50 tokens × 7 days = ~350 tokens. Well within Haiku's context and cost profile.

**Alternative considered:** Frontend-only AI call via a Nuxt server route. Rejected — Anthropic API key must not be exposed to the browser; backend keeps it server-side.

**Alternative considered:** Reuse/extend the existing `SuggestWeek` function. Rejected — `SuggestWeek` is a deterministic scoring algorithm; the AI planner adds freetext context reasoning and a different response shape. Keeping them separate preserves the existing scoring engine for future use.

### Decision 5: Suggestion bar retired

**Chosen:** `PlanSuggestionBar` is removed from `plan.vue`. The Planning Assistant panel serves the same function more powerfully.

**Rationale:** The suggestion bar offers 1-3 per-day algorithmic hints without the browsable context that makes them actionable. The planner's feedback is that suggestions "haven't yielded actionable results" — the bar's narrow surface and lack of effort/freshness context are the root cause.

## Risks / Trade-offs

- **Panel width at md (960px):** The support column is `md="4"` (~320px at 960px breakpoint). Dish rows with name + effort + days-ago + `+` button may be tight. Mitigation: effort shown as a 1-letter badge (Q/M/E) at small sizes; dish name truncates with ellipsis; `+` button is touch-target sized.

- **AI planner latency:** Haiku is fast (~1-2s) but the call depends on network and cold starts. Mitigation: show a loading skeleton in the panel draft area; the action is explicitly user-initiated so latency is expected.

- **AI planner dish hallucination:** Haiku may return dish names or IDs not in the catalog if the prompt is not strict enough. Mitigation: prompt instructs returning only dishIds from the provided list; backend validates each returned dishId against the loaded catalog before including it in the response; invalid entries are silently dropped.

- **Dish stats not loaded on plan page:** Currently `dishRepo.allUsageStats` is NOT called on the plan page — only on `/dishes`. The `dishStats` attached to `dishesStore.dishes` may be null/empty unless the user has visited `/dishes` first in the session. Mitigation: `PlanAssistantPanel` triggers a `dishRepo.allUsageStats` load on mount (similar to how `/dishes` does it); stats are stored on the dish objects in the store so subsequent renders are fast.

- **Wishlist panel removal discoverability:** Users accustomed to the wishlist being in the right panel must find it in the new panel toggle. Mitigation: the "Wishlist" tab is visually prominent with a badge showing active wish count.

## Open Questions

- Should the AI planner accept per-day effort hints (e.g., "Quick on Tuesday") via structured toggles, or rely on freetext only? Structured toggles are more reliable for Haiku but add UI complexity. Start with freetext; add structured hints in a follow-up if freetext proves ambiguous.
- Should the panel be wider (`md="5"`) to accommodate the richer rows? Needs a visual review once the component is built — can be adjusted without functional impact.
