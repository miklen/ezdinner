# Verification — 2026-10-03

Implementation tasks 1–7 and specification verification 8.3 are complete (21/23).
The change contains one capability delta, `recipe-snapshot`; its requirements were checked against the implementation. Task 8.3's reference to two deltas does not correspond to an additional artifact in this change.

## Passing checks

- Frontend: 53 tests pass; lint passes with one existing `DishMetadataCard.vue` warning; production generation passes.
- Backend: Functions build passes. The initial full test run passed 222 tests with one pre-existing failure. Legacy and snapshot-bearing document reads, projection, updates, and note preservation are covered.
- `openspec validate capture-recipe-snapshots --strict` passes.

## Remaining verification

- 8.1: `AggregateRootTests.Instance_IsInitialized_CanBeDeserialized` fails on unchanged HEAD as well: its parameterless fixture generates a new ID when deserialized. Approval was requested for a test-only JSON constructor correction. The latest run has 225 tests: 220 pass; this existing failure remains, and Windows Smart App Control blocks four tests loading a newly built assembly (`0x800711C7`). Code Integrity events confirm the operating-system policy block. No policy change was made.
- 8.2: Manual desktop/mobile English/Danish verification is pending because the computer-use session reports no available apps or browsers. A reusable fixture with actual Vue/Vuetify components and simulated responses is in `web/tests/visual/`; start it with `npx vite --config tests/visual/vite.config.ts` from `web`, then open `http://127.0.0.1:3035/tests/visual/recipe-snapshots.html`.

The change is not ready to archive while these checks remain unresolved.
