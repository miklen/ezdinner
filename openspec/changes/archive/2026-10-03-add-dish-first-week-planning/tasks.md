# Tasks

## 1. Domain recommendation policy

- [x] 1.1 Add immutable recommendation candidate, usage-pattern, and reason value objects plus a factory under Core/DomainServices/DishRecommendations; verify unit cases for deleted/archived dishes, unknown roles, unrated dishes, and never-used history without synthetic defaults.
- [x] 1.2 Implement recurrence from distinct past dates, consecutive-day occurrence grouping, and median spacing after at least three occurrences; verify 30-40 day sequences, leftovers, sparse history, and future dates with domain unit tests.
- [x] 1.3 Implement bounded resurfacing rules for ratings, wishes, recency, established rotation, and former frequency with deterministic ties; verify curated forgotten-favourite and former-regular examples and that indefinite age cannot dominate every other signal.

## 2. Recommendation query and provider

- [x] 2.1 Add the Query.Core recommendation request/result contracts and context assembly using canonical family dishes, notes, optional snapshots, historical dinners, current nine-day assignments, and wishes; verify query tests with and without snapshot content and without any dinner writes.
- [x] 2.2 Add a dedicated LLM interface and Infrastructure adapter for suitability reasoning, cumulative constraints, localized explanations, and factual/inferred evidence; verify controlled provider tests for potato pairings, preparation steps, family adaptations, and no unsolicited variety goals.
- [x] 2.3 Validate canonical IDs, supplied source references, exclusions, role scope, malformed responses, no-match, and exhaustion; verify fixtures prove request suitability precedes historical score and More ideas never silently recycles excluded dishes.
- [x] 2.4 Bound request/evidence sizes, provider timeouts, and cancellation without silently restricting semantic search to overdue dishes; verify budget overflow returns a narrowing outcome and failures remain distinguishable from no-match.
- [x] 2.5 Add the additive authorized recommendation Function and registrations without changing legacy planner bindings; verify invalid dates/locale/payloads and unauthorized family requests are rejected before provider access, and document the API contract and configured limits beside the feature.

## 3. Dinner mutation and undo integrity

- [x] 3.1 Add the narrowly scoped conditional undo use case, expected dinner-state value object, and infrastructure conditional-save support for the new feature; verify addition/removal inverses, duplicate no-ops, preserved unrelated items, restored opt-outs, and storage conflicts with focused backend tests.
- [x] 3.2 Expose authorized conditional undo through an additive endpoint and typed frontend repository contract; verify unauthorized requests, stale expectations, and unchanged-day restoration and document its preconditions without altering legacy add/remove callers.

## 4. Frontend state and additive entry

- [x] 4.1 Add the typed recommendation repository and discriminated request/result models; verify repository tests for automatic, request, more, clarification, failure, and conditional undo responses.
- [x] 4.2 Create the independent /plan-your-week route and localized navigation entry using a thin route and focused feature container; verify both desktop rail and mobile navigation expose it while existing links and routes remain available.
- [x] 4.3 Implement one Monday-derived nine-day window and locally owned dinner reads; verify overview/picker date identity, preceding/following weekends, previous/next navigation, historical windows, and out-of-window target clearing.
- [x] 4.4 Implement feature composables for exploration, recommendation conversation, and mutations with computed derived state and explicit typed intents; verify family/account reset, cancelled/stale responses, and that the existing global dinner range cannot overwrite the new window.

## 5. Dish exploration and conversation

- [x] 5.1 Build consistent dish cards and catalog exploration with metadata filters, four sort modes, wishes, and assigned-date signals; verify main/unclassified versus side-only behavior, combined filters, unavailable metadata, and changing roles through public component interactions.
- [x] 5.2 Build the initial recommendation selection and free-text/follow-up interface with editable context, More ideas, dismissal, and reset; verify cumulative constraints, exhaustion, role requests independent of catalog filters, and retained prior results on errors.
- [x] 5.3 Implement optional day scope and explicit keep/start-new behavior before requesting for a different day; verify no forced day selection, no automatic advance, and no silently inherited day restriction.
- [x] 5.4 Add in-workspace recipe/notes inspection using the optional snapshot contract from capture-recipe-snapshots; verify missing content, sanitized Markdown, keyboard focus return, and exact exploration/scroll restoration after closing.

## 6. Week overview and continuous decisions

- [x] 6.1 Build the supporting desktop overview and compact mobile week control/full overview with existing-style previous/next navigation; verify all nine dates show actual menu names, multiple dishes, empty days, opt-out reasons, and no balance or completeness judgment.
- [x] 6.2 Build the shared-window assignment picker and optional direct selected-day action using existing add/remove persistence; verify occupied days, duplicate detection, multi-day leftovers, confirmation before clearing opt-outs, and actionable save failures.
- [x] 6.3 Wire success feedback, explicit removal, and conditional undo without closing exploration or reranking results; verify an inspect/assign/continue/remove/undo session retains request, filters, position, and unrelated saved choices.
- [x] 6.4 Complete English/Danish copy, responsive spacing, touch targets, accessible labels, focus handling, and request loading/retry states; verify locale coverage and keyboard operation, and ensure the five navigation entries fit narrow mobile screens.

## 7. Integration and experience acceptance

- [x] 7.1 Run backend build/unit tests and frontend prepare/tests/lint appropriate to the changed modules; record results and fix failures, including regression coverage that existing Plan, Dishes, assistant, and day/week endpoints retain their contracts.
- [x] 7.2 Exercise the complete design acceptance session on desktop and narrow mobile in English and Danish: browse a forgotten favourite, inspect, assign, request potato pairings, exclude fish, change target day, request sides, remove/undo, and navigate both weekends; record visual/interaction evidence and fix interrupted context or unclear actions.
- [x] 7.3 Verify shared saved dinners by alternating new workspace and existing Plan, plus empty catalog, long dish names, provider failure, storage conflict, and family switch during a pending request; record observations and the additive rollback path in the feature documentation.
