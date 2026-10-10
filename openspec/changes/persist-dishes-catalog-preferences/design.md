# Design

## Context

See `proposal.md` for motivation and `specs/dish-metadata-tags/spec.md` for acceptance behavior.

Observed in the active frontend:
- `web/pages/dishes/index.vue` owns search, four filter Sets, archived view, and sort refs locally; `web/app.vue` renders `NuxtPage` without view caching.
- The catalog's `allDishes` is intentionally separate from `web/stores/dishes.ts`, whose active-only data serves other pages. `DishesRepository.all(familyId, true)` includes archived dishes; the catalog then displays archived-only results.
- The family watcher immediately calls `init()`. Archive toggling currently fetches inside its click handler; restored archive state must also affect initial loading.
- `web/stores/app.ts` already saves the active family to localStorage. MSAL initializes before providing `$msal`; `getObjectId()` exposes the active account's `localAccountId`.
- Filtering uses OR within a dimension and AND across dimensions. Cuisine selection uses lowercase keys. Sort defaults are name ascending and other keys descending. Clear filters preserves search and sorting.
- Nuxt runs with `ssr: false`; Composition API, Pinia setup stores, Vuetify, and English/Danish translations are established conventions. Vitest currently uses a Node environment with no SFC plugin.
- Existing metadata and archive requirements remain applicable. Their main specs lack Purpose sections, preventing the CLI's JSON overview; the complete text was inspected successfully. Repairing unrelated spec formatting is outside this change.

## Goals / Non-Goals

**Goals:**
- One reactive source of truth for preferences, independent of component lifetime and catalog data.
- Restore preferences before fetching, with explicit side effects and testable scope transitions.
- Extract catalog responsibilities following the requested Vue best practices while preserving existing markup, styling, and interactions.

**Non-Goals:**
- Cloud or cross-device synchronization, URL/query-state contracts, live cross-tab synchronization, or a generic persistence framework.
- Persisting dish data, statistics, scroll position, filter-panel expansion, dialogs, or assistant state.
- Changes to backend APIs, authentication, the legacy root frontend, or unrelated page behavior.

## Decisions

### 1. Browser-local, account-and-family-scoped records

Use localStorage keys of the form `ezdinner:dishes-catalog:v1:<encoded-account-id>:<encoded-family-id>`. Identity comes from `$msal.getObjectId()` and `appStore.activeFamilyId`; no read/write occurs without both. Saved records remain available after sign-out for the same account's later sessions, with other accounts using distinct keys.

Use a versioned JSON record containing `search: string`, `roles/effort/season/cuisine: string[]`, `showArchived: boolean`, `sortKey: 'name' | 'rating' | 'timesUsed' | 'lastUsed'`, and `sortDir: 'asc' | 'desc'`. Persist canonical metadata values rather than translated labels. Deduplicate arrays and lowercase cuisine keys. Normalize a cleared search field's null value to an empty string.

Decode from unknown data: require a supported version and object shape; validate each field, enum, and array element. Unusable records restore fresh defaults; invalid fields in a supported record restore their field defaults. Ignore unknown fields. Catch JSON and storage errors without interrupting rendering or controls.

Alternatives: sessionStorage cannot survive reopened sessions; server persistence adds an unnecessary API/data model; URL state alone does not restore selections when returning through ordinary navigation; KeepAlive cannot survive refreshes and also retains unrelated page state.

### 2. Pinia owns preferences; a composable owns browser effects

Add `web/stores/dishCatalogPreferences.ts`, using a setup store with explicit Vue/Pinia imports and a returned reactive map of preferences by scope. This map is the only preference state; expose typed actions for search, metadata toggles, archived view, sorting, clearing, and reconciliation. Store arrays rather than Sets so state remains serializable and inspectable. Derive active counts and selection lookups with computed values.

Add `web/composables/useDishCatalogPreferences.ts` to select the current scope, restore its record synchronously before enabling writes, and persist changes. Accept a small storage adapter for tests; production acquires localStorage only on the client inside guarded access. Keep parsing/normalization as pure functions rather than reactive effects. Keep an already-loaded scope's in-memory entry across catalog remounts; restore from disk when a new app instance first encounters that scope. Do not duplicate store values in local refs or use watchers to mirror them.

Watch explicit scope/preferences sources, with restoration and write ordering guaranteed before load effects. Associate writes with the scope whose values changed; a family transition must never write outgoing values under the incoming key. Persist each control change promptly, without a delayed save vulnerable to navigation. Dispose component-owned effects on unmount; Pinia state survives. Direct store access or computed wrappers preserve reactivity across scope changes; avoid destructuring reactive values into primitives.

Alternatives: a page-only composable loses in-memory state when storage fails; module-global refs create avoidable lifecycle/SSR coupling; placing UI preferences in the existing dishes data store mixes catalog presentation with active-dish data shared by planning.

### 3. Focused Vue component boundaries

Reuse the current visual design; this is a state behavior change. Preserve scoped styles, localized labels, touch targets, and existing props/events.

| Surface | Responsibility and contracts |
|---|---|
| `pages/dishes/index.vue` | Compose the catalog and existing desktop/mobile assistant; retain assistant wiring and keep catalog logic out of the route. |
| `components/Dish/Catalog.vue` | Own catalog fetches, statistics, creation, and derived results; connect preferences to controls and pass loaded results to the grid. |
| `components/Dish/CatalogControls.vue` | Render search, sort chips, metadata/archive controls, and filter expansion. Typed props supply preferences, available options, active count, and loading; typed emits report search updates, dimension/value toggles, sort selection, archive toggle, and clear filters. Expansion stays local. |
| `components/Dish/CatalogGrid.vue` | Render loading, empty states, archive banner, and existing `DishCard` children. Accept dishes, stats, and explicit display state; forward archive and move events to the container. |
| `components/Dish/CreateDishDialog.vue` | Render the existing creation form. Typed open/name models and a create event; form/dialog state stays temporary. |

All SFCs use `<script setup lang="ts">`, script/template/style ordering, props down and events up. Existing `DishCard` and `PlanAssistantPanel` contracts stay intact. This decomposition is bounded to the catalog being changed; do not redesign the page or refactor unrelated assistant logic.

### 4. Load against the restored scope and archive state

Wire catalog initialization to the resolved preference scope and archived flag after restoration. Use one load path for mount, family change, archived toggle, and explicit refresh, avoiding duplicate fetches from both a watcher and click handler. Capture family/archive state when beginning a request; a generation token or cancellation prevents obsolete responses from committing after scope/archive changes or unmount.

Keep catalog data and statistics local. Continue populating the existing active-dish store without `includeArchived`; wait for its population before applying its stats, as today. Persisted sorting applies to current catalog statistics. Toggling archived view while a catalog fetch is pending remains blocked as currently implemented.

After the current load succeeds, derive available filter options and reconcile saved selections against them. Do not reconcile while the list is empty due to initial loading, errors, or stale responses. The selected archive mode determines the fetched option universe, matching current behavior. Persist any reconciled selections. Clear filters updates metadata/archive preferences as one action, preserving search and sort; returning from archived view triggers the same load path.

Alternatives: persisting allDishes risks stale catalogs; pruning before data arrives erases valid filters; retaining unavailable selections leaves invisible filters that users cannot toggle off.

### 5. Focused verification with existing tooling

Use Vitest in its existing Node environment to exercise the actual setup store/composable with fresh Pinia instances, Vue effect scopes, and fake storage. Explicit imports make the store testable without Nuxt auto-import machinery; stub only the Nuxt/application boundaries. Verify consumer disposal/recreation for navigation and a fresh Pinia instance for browser-session restoration. Assert behavior and storage scope, not copied comparator implementations or file text.

Automate defaults, all saved fields, scope isolation, clearing, sort directions, codec failures, read/write exceptions, and reconciliation after successful loads. Verify restored archived request arguments and stale-response handling through the extracted catalog loading logic where practical. Perform real browser checks for detail-page/back navigation, app-menu navigation, refresh/reopen, and desktop/mobile controls. Run `npm test` and `npm run lint` from `web/` during implementation. No new test runtime dependency is required by this design.

## Risks / Trade-offs

- Browser storage can be blocked or cleared -> retain usable in-memory state; cross-session restoration requires available storage.
- Family switching can race catalog loads -> restore scope first and reject obsolete responses; never reconcile the wrong scope.
- Metadata may change while the page is away -> reconcile only after a successful load; unavailable selections intentionally disappear.
- Component extraction may alter layout/events -> move existing markup/styles without visual redesign and check both responsive layouts.
- Browser-local settings are not shared between devices -> document this boundary; no synchronization is implied.

## Migration Plan

- Deploy as a frontend change. No stored catalog preference format currently exists; absent v1 records use current defaults.
- Existing family and locale storage keys stay unchanged.
- Roll back the frontend if needed; earlier code ignores the new namespaced keys. Do not clear unrelated localStorage.
