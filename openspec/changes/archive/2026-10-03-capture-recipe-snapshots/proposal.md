# Proposal

## Why

Linked recipes can be changed or removed by their publishers, leaving a family's saved dish without the recipe they intended to keep. EzDinner should preserve a point-in-time recipe while ensuring that family-authored shopping reminders and recipe adjustments are never overwritten by an import or refresh.

## What Changes

- Add an optional captured recipe snapshot to a dish alongside, rather than inside, the existing user-authored notes.
- Allow a family member to import a recipe from the dish's source URL, review the extracted result, and confirm it before saving.
- Extract machine-readable recipe data when available and use the configured LLM provider to distill recipe content when deterministic extraction is insufficient.
- Allow a family member to refresh an existing snapshot, with an explicit warning and preview before replacement.
- Preserve existing snapshots when a URL changes or is removed unless the family member explicitly replaces or removes the snapshot.
- Retain compatibility with existing Cosmos DB dish documents that have no recipe snapshot, without a data migration.
- Present user notes and the captured recipe as distinct sections in the dish details UI on desktop and mobile, with all new text localized in English and Danish.

## Capabilities

### New Capabilities

- `recipe-snapshot`: Capturing, previewing, saving, displaying, refreshing, and safely removing point-in-time recipes without modifying user-authored notes.

### Modified Capabilities

None.

## Impact

- Dish aggregate persistence and query models gain an optional recipe snapshot.
- New application commands/provider abstractions and Azure Function endpoints support recipe extraction and snapshot lifecycle operations.
- Infrastructure gains safe remote-page retrieval, deterministic recipe extraction, and an LLM-backed extraction implementation using the existing Anthropic integration.
- The dish repository client, TypeScript models, localized strings, and dish notes UI gain import, preview, refresh, and snapshot presentation behavior.
- Backend unit tests and frontend Vitest component/repository tests cover compatibility, note preservation, confirmation flows, and failure handling.
