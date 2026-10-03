# Design

## Context

See `proposal.md` for motivation and the two delta specs for behavior. The active frontend is Nuxt 3 with Composition API, TypeScript, Pinia, Vuetify, and English/Danish localization. `web/layouts/default.vue` supplies one links array to the desktop rail and mobile bottom navigation. `plan.vue`, `PlanAssistantPanel`, and the dish catalog already provide useful mechanics but distribute exploration across routes; the mobile assistant closes after assignment.

`useWeekNav` chooses a Monday; Plan and `PlanDishDialog` load Monday minus two days through Sunday. `DinnerRepository` already provides range reads and add/remove/opt-out mutations. `Dinner.AddMenuItem` deduplicates and clears opt-outs. The replace-menu endpoint is a cross-history dish replacement, not a single-date replacement; do not use it for changing one dinner.

Existing suggestion rules use an unbounded overdue score, synthetic never-used defaults, and mean gaps including leftovers. The AI catalog omits notes, recipe content, ratings, and recurrence. Its day/week contracts intentionally return dated slots and sometimes recycle exclusions. Those contracts remain untouched. Recipe snapshots are optional and are being implemented separately.

## Goals / Non-Goals

**Goals:** Keep one source of truth for the new workspace's selected window and exploration; expose honest evidence; isolate deterministic domain policy from orchestration and provider I/O; make the UX acceptance loop concrete.

**Non-Goals:** Replace Plan, modify its planner selection, introduce a persistent chat aggregate, fetch recipe websites during recommendations, add nutritional data, or build a drag-and-drop requirement. A separate snapshot importer and existing catalog-preference work remain independent.

## Decisions

### 1. Add a dish-first feature rather than extend the assistant

Add `/plan-your-week` and a localized menu entry while keeping `/plan` and `/dishes`. Use a new feature folder under `web/components/WeekPlanning` and feature composables. Desktop allocates the majority of width to exploration, with a sticky narrow week column. Mobile starts on dishes, with a compact sticky week control and an expandable overview above the existing bottom navigation. Both full overviews use previous/next navigation, not an Expand week desktop action.

Keep current visual tokens and typefaces, with restrained emphasis on dish names and selected-day feedback. The signature element is the live nine-day overview showing actual dish names. The default recommendation selection is six dishes; the full catalog remains below it. Main browsing includes Main and clearly labelled unclassified dishes. Wishes are a filter/access point in exploration, not a second competing assistant. Asking replaces the recommendation selection with request results and leaves browsing available.

Alternative: reuse the large assistant panel. Rejected because its date drafts, duplicate dish browsing, and close-on-assignment behavior work against the new loop and risk legacy changes.

### 2. Make Vue component boundaries explicit

Use `<script setup lang="ts">`, typed props/emits, computed derivations, and watchers only for side effects. The route is a thin composition surface. Proposed component contracts:

| Component | Responsibility | Inputs and events |
|---|---|---|
| WeekPlanningWorkspace | Compose the feature and connect composables | Active family; coordinates typed child intents |
| DishExplorer | Browse/search/filter/sort dishes and wishes | Dishes, view preferences, planned dates; inspect/plan intents and preference updates |
| RecommendationExplorer | Render request, editable context, results, and follow-up controls | Discriminated request state and results; ask/more/reset/context-change intents |
| PlanningDishCard | Present a dish consistently across results and catalog | Dish, reasons, assigned dates, optional target date; inspect/assign intents |
| PlanningWeekOverview | Present all nine dates and navigate weeks | Window, dinners, selected date; navigate/select/remove intents |
| PlanningAssignmentPicker | Choose dates with existing decisions visible | Dish and the same nine-day model; assign intent |
| PlanningRecipeInspector | Inspect notes and saved recipe without leaving | Dish content, loading state; close intent |

`useWeekPlanning` owns selected Monday, optional target date, the scoped dinner read model, and mutation feedback. `useDishExploration` owns filters/sort/search and scroll restoration. `useDishRecommendations` owns request turns, visible constraints, exclusions, scope, and loading/error outcomes. Components never mutate props or perform duplicate range reads. Keep session state in a feature instance; no new global store unless actual cross-route reuse requires it. Reuse dish/wish repositories and compatible presentation primitives, not legacy orchestration. Do not apply the catalog preference key to this independent workspace.

Alternative: one route containing every state and dialog. Rejected for coupled lifecycle, weak test boundaries, and disrupted restoration.

### 3. Preserve browsing through explicit interactions

There is no initial target date. Plan opens a nine-day picker; selecting a day in the overview changes the primary card action to Add to that day, with another-day selection available. The canonical window is Monday minus two days through Monday plus six days, including past dates when browsing historical weeks. Every surface derives dates from it. Week changes clear a target only when it leaves the window.

Recipe inspection opens an accessible desktop dialog/mobile sheet. Save success updates the overview and assigned-date labels without reranking the candidate list, closing discovery, or auto-advancing. More ideas is explicit. Persist search, filters, request, and position across all local overlays. Selected-week changes retain the conversation but label existing results as from the prior context until explicitly refreshed. Use request generation identifiers and cancellation to discard stale family/week/request responses.

A day-scoped request requires Keep preferences or Start new request when its target changes; defer that decision until the next request, not on every exploratory click. A workspace-scoped request stays active. Role filters belong to catalog browsing; explicit conversational role requests override only recommendation scope, visibly.

### 4. Keep dinner mutations and undo narrowly scoped

Use existing add/remove mutations for ordinary assignments and removals; refresh the saved range after success. Do not treat existing duplicates as a new addition. Feedback carries the exact family, date, dish, and prior day state. Undo is an inverse of that action, never a replacement of the entire week.

Because adding clears opt-outs, show confirmation for an opted-out day. Restoration must not overwrite later changes. Add a narrow conditional undo command only for this new feature: an immutable prior/current dinner-state value object expresses the expected post-action state, the Application command loads the Dinner aggregate and checks expectations, then mutates through aggregate methods. Infrastructure performs the conditional save using the storage revision/ETag; conflicts return a refresh/review outcome. Inspect the existing repository's persistence adapter when implementing this command, extending its conditional-save capability without changing legacy callers. Undo ordinary actions preserves unrelated dishes; restoring an opt-out requires an unchanged post-action day. No generic history replacement or new dinner aggregate.

Alternative: blindly call set-opt-out during undo. Rejected because it clears other menu items and can destroy subsequent decisions.

### 5. Introduce an additive recommendation query

Add `POST /api/families/{familyId}/dish-recommendations`, separate from day/week endpoints. Validate authenticated family access to dishes, dinners, and wishes before loading evidence. Input contains selected Monday, optional target date within the nine-day window, mode (automatic/request/more), locale, bounded conversation turns/active constraints, and excluded result IDs. Validate dates, size, count, and scope at the boundary.

Output is a discriminated result: matches, no-match, exhausted, or needs-clarification, containing canonical dish IDs/names, typed factual signals, localized request reasons, evidence references/limitations, and the active context summary. Provider failure is an error, not no-match. Never accept provider names as authoritative or execute model-proposed mutations. Query orchestration reloads family data and current dinners each time; client conversation is intent, not trusted dish evidence. No new conversation database.

`EzDinner.Query.Core/DishRecommendationQueries` assembles read models and calls a pure recommendation service and an LLM client interface. Functions parse/authorize/map; Infrastructure implements Anthropic transport, budgets, and validation. Domain lives in `Core/DomainServices/DishRecommendations` with immutable `DishRecommendationCandidateValueObject`, `DishUsagePatternValueObject`, reason value objects, a candidate factory, bounded scoring Rules, and `DishRecommendationService`. These are ephemeral domain values/services, not aggregates with repositories. Existing Dish, Dinner, and Wishlist aggregates remain unchanged except a narrowly justified undo invariant method if needed. Domain imports no HTTP, repositories, provider, or logging.

### 6. Ground ranking and semantic reasoning separately

Automatic resurfacing is deterministic. Use distinct past calendar serving dates before today; retain raw serving counts and collapse consecutive dates into occurrences for recurrence. Estimate typical spacing as the median between occurrence starts when at least three occurrences exist. Compare to the optional target date, otherwise selected Monday, while clearly expressing last-served facts. Future assignments are separate context. Never-used and sparse-history dishes have explicit states, not synthetic 365-day/14-day claims. Cap overdue influence; combine known rating, votes, historic frequency, and recency. Neutral signals produce no reason; tie-break deterministically. Initial weights are implementation defaults validated with curated history examples, not claims about family preferences.

For free text, the provider evaluates request suitability from canonical titles, notes, optional recipe content, and metadata, with history and the current plan as secondary context. A recurrence shortlist must not be the only semantic search corpus: a recently served dish may best fit the request. Include all eligible catalog candidates within configurable content/token limits; if the evidence budget cannot cover them, return a clear narrowing request rather than silently excluding the rest or making unsupported recipe claims. No live website retrieval. Store source references with reasons and validate IDs, referenced supplied text, role context, and explicit exclusions. Supporting text is evidence, not proof of every inference; mark culinary inference and missing evidence explicitly.

Request matching precedes historical ranking. More ideas excludes shown/dismissed IDs within the request and reports exhaustion rather than using legacy fallback. Provider instructions prohibit unsolicited dietary goals; explicit user requests can ask for variety. New provider behavior does not alter the old week planner.

### 7. Treat UX proof as an acceptance gate

Use existing Vitest/Vue Test Utils and backend unit-test conventions. Verify domain history rules with real date sequences; query/provider tests use controlled adapters, never live LLM assertions. Component tests exercise inspect-return, multi-day assignment, occupied days, undo, week changes, day-scoped refinement, stale responses, and errors through public behavior.

Before marking implementation complete, visually inspect desktop and narrow mobile in both languages. Exercise a full session: find a forgotten favourite, inspect, assign, request a potato pairing, exclude fish, choose another day, request sides, remove/undo, navigate weeks, and return to Plan. Validate keyboard focus, touch targets, long names, empty catalog, no matches, provider failures, and five-entry mobile navigation. Record observations and fix interruptions. A screenshot alone does not prove continuity.

## Risks / Trade-offs

### Approved mutation provenance and shared creation extension

The user approved extending the original persistence design after review showed that cached-state undo could reverse another member's no-op assignment. The workspace now opts into Changed/NoOp/Conflict responses on the existing add/remove routes, supplying expected menu and opt-out state. Only a successful conditional save returns canonical before/after states for undo. NoOp and Conflict never create an undo receipt or clear local wish feedback. New dinners share a stable family/date identity and use create-if-absent; existing IDs remain intact. All legacy endpoints that can create absent days reload and reapply intent with bounded ETag/create retries, preserving their HTTP contracts. Existing bulk replacement/conversion remains unchanged. Keep stable identity/shared creation on rollback and avoid a mixed rollout with old random-ID creators. This explicitly extends the earlier decision to leave aggregates unchanged except undo.

- [Recipe snapshot work is concurrent] -> Integrate its optional published contract after that change stabilizes; no import or refresh work here, and missing snapshots remain valid.
- [LLM evidence is incomplete or output unsupported] -> Bounded input, explicit limitations, source references, canonical ID validation, and no-match/clarification outcomes; do not promise exact ingredient certainty from a title.
- [Growing catalogs increase prompt cost/latency] -> Configured request budgets, cancellation and transparent narrowing; preserve ordinary browsing during requests.
- [Global dinners store represents another range] -> Keep new window ownership local and refresh shared persistence on entry rather than introducing competing reads of one array.
- [Concurrent edits make undo stale] -> Conditional saves and clear conflicts; never restore an opt-out over later assignments.
- [Menu labels crowd mobile navigation] -> Verify both localized labels at narrow widths, retain clear accessible names, and adjust the new entry's presentation without removing existing entries.

## Migration Plan

1. Ship additive domain/query/provider and recommendation endpoint contracts; keep existing suggestion configuration and routes intact. Coordinate optional recipe model compatibility before integration.
2. Ship the independent frontend route and both navigation entries with shared dinner persistence. No Cosmos data migration or durable chat storage is required.
3. Validate complete desktop/mobile sessions and existing Plan/Dishes behavior before release.
4. Roll back by removing the new navigation entry/route and additive APIs. Dinners saved through the experiment remain ordinary dinners visible in existing Plan. Replacing Plan requires a separate future decision.
