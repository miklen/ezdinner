# Proposal

## Why

The dishes page recreates its filter and sort refs whenever users navigate away and return, losing their chosen catalog view. Users need that view restored across navigation, browser refreshes, and later sessions.

## What Changes

- Remember text search, selected role/effort/season/cuisine filters, archived-only view, sort key, and sort direction in this browser.
- Scope preferences to the signed-in user and active family; restore them before loading the catalog.
- Persist filter clearing and subsequent changes automatically, preserving existing search, filtering, sorting, and archive behavior.
- Validate saved preferences and discard metadata selections no longer available after a successful catalog load.
- Keep the catalog usable when browser storage is unavailable or saved data is invalid.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `dish-metadata-tags`: Add persistent catalog view preferences, including lifecycle restoration, family/account isolation, clearing, and storage failure behavior.

## Impact

- Active Nuxt 3 frontend under `web/`: dishes catalog, a focused preferences composable, and catalog components extracted along Vue responsibility boundaries.
- Reuse existing MSAL object ID, active-family state, repository archive option, and Vitest infrastructure.
- Browser-local preferences only; no API changes, server persistence, cross-device synchronization, or runtime dependencies.
- Preserve responsive layouts and English/Danish localization. Planning artifacts only in this change.
