# Tasks

## 1. Preference state and browser persistence

- [ ] 1.1 Add the typed Pinia setup store in `web/stores/dishCatalogPreferences.ts` with independent account/family entries and actions for search, metadata toggles, archived view, sorting, and clear filters; verify actual-store Vitest tests cover defaults, OR-ready multi-selections, cuisine normalization, direction toggling, and clearing that preserves search/sort.
- [ ] 1.2 Add the versioned codec and `web/composables/useDishCatalogPreferences.ts` with guarded browser storage, synchronous restore-before-write ordering, and a fake-storage boundary; verify round-trip tests cover every preference field, malformed JSON, unsupported versions, invalid fields, null search clearing, and read/write exceptions.
- [ ] 1.3 Wire composable scope changes to the existing MSAL object ID and active family; verify tests dispose/recreate consumers, restart with fresh Pinia, switch A/B/A families, switch accounts, and omit identity without overwriting another scope or writing anonymous records.
- [ ] 1.4 Document the storage key, v1 payload, browser-local lifetime, and restore/write ordering next to the persistence code; verify the comments match the codec and scoped persistence tests.

## 2. Catalog composition and loading

- [ ] 2.1 Extract the catalog container, controls, grid, and creation dialog under `web/components/Dish/` using the design's typed props/events; leave `web/pages/dishes/index.vue` as catalog/assistant composition and preserve existing markup/styles. Verify desktop/mobile search, filters, sorting, dish actions, creation, and assistant interactions before adding persistence bindings.
- [ ] 2.2 Bind catalog controls and pure computed dish results to the preference source, removing duplicate local preference refs while keeping expansion/dialog/assistant state temporary; verify changing controls updates visible results and a disposed/recreated catalog consumer restores the choices.
- [ ] 2.3 Restore preferences before the catalog load and consolidate mount/family/archive loading into one path; capture request scope and reject obsolete results. Verify focused loading tests use saved archive state on the initial request, retain active-only shared dish-store calls, and prevent late responses from replacing the current scope or reconciling its selections.
- [ ] 2.4 Reconcile metadata selections against available options only after a successful current catalog load; verify tests preserve valid selections, prune unavailable values, persist the corrected record, and retain all selections during pending/failed/stale loads.
- [ ] 2.5 Preserve English/Danish labels, scoped styles, and existing sort/filter semantics in extracted components; verify both locales and mobile/desktop layouts, including filter counts, clear filters, archived banner, empty results, and touch interactions.

## 3. Integration verification

- [ ] 3.1 In a real browser, choose search, multiple metadata filters, archive state, and a non-default sort direction; verify dish-detail/back and app-menu navigation, refresh, and browser reopen restore the same choices against current data. Record results for desktop and mobile widths.
- [ ] 3.2 Verify family switching and account switching preserve independent preferences; verify clearing filters survives reopening while preserving search/sort, and planning/dish pickers remain active-only.
- [ ] 3.3 Run `npm test` and `npm run lint` from `web/`, plus `openspec validate persist-dishes-catalog-preferences --strict`; record results and resolve failures attributable to this change.
