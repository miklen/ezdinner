# Implementation verification - 2026-10-03

Verified tasks: 1.1-1.3, 2.1-2.5, 3.1-3.2, 4.1-4.4, 5.1-5.4, 6.1-6.3, and 7.1 (22/25). Tasks 6.4, 7.2 and 7.3 remain open for real browser acceptance.

## Automated checks

- Backend feature suite: 55 passed. Covers eligibility, missing history/rating, leftovers/median recurrence, bounded resurfacing, canonical evidence and optional snapshots, semantic precedence, provider validation, roles, no-match/exhaustion, cancellation/timeouts, authorization, undo inverses, opt-out restoration, and conditional Cosmos conflicts.
- Backend full suite: 279 passed, 1 failed (280 total). The unchanged `AggregateRootTests.Instance_IsInitialized_CanBeDeserialized` fails at line 28 because the deserialized aggregate ID differs. Its parameterless fixture constructor generates a new GUID and the serialized ID is not restored.
- Backend Functions build passed with zero errors and 22 warnings.
- Frontend full suite: 92 passed across 12 files. Feature coverage includes 10 repository, 13 composable, 13 workspace interaction, 2 navigation, and 1 locale tests. Existing dinner repository/store and recipe component tests also pass.
- Nuxt prepare passed. Lint: zero errors, one existing `DishMetadataCard.vue` self-closing-input warning.
- Additional vue-tsc check reports errors in unchanged DishDetailHeader, DishFamilyRatings, LanguageSwitcher, Plan/PlannedDinnerDetails, formatting/render-markdown, nuxt.config, pages/dishes/[id], and Vite/Vitest configuration. It reported no errors in the new planning components, composables, repository, or types.

The user explicitly instructed "leave it and continue" after the unrelated backend serialization and frontend type failures were reported. They remain unchanged; they have not been represented as passing checks.

## Component acceptance evidence

Public workspace interactions with controlled repositories verify independent catalog/request roles, all four sort modes, combined filters and wishes, cumulative/editable requests, dismissal/exclusion/reset, exhaustion, retained results on provider failure, explicit keep/new day scope, and no automatic day advance. Inspector tests verify sanitized notes/optional snapshot content, missing-recipe copy, source links, return to the triggering element, retained search, and exact scroll restoration.

Assignment and overview fixtures verify the same nine dates, both weekends and week navigation, multiple saved names (including archived dishes), empty days, opt-outs, occupied-day additions, leftovers, duplicates, opt-out confirmation, removal, undo and conflict feedback while exploration remains open. Composable fixtures verify stale family/account responses and save failures. Navigation fixtures expose all five entries on both navigation surfaces. English/Danish locale key coverage passes.

These tests use lightweight Vuetify stubs and controlled repositories. They do not establish real browser layout, keyboard behavior with actual Vuetify dialogs, live provider quality, Cosmos persistence, or saved-dinner alternation with the existing Plan.

## Browser acceptance blocker

The browser tool failed before initialization when executing `await cua.getState()`:

```text
node_repl kernel exited unexpectedly
windows sandbox failed: orchestrator_helper_launch_failed
setup refresh failed to launch helper
helper=codex-windows-sandbox-setup.exe
cwd=C:\code\ezdinner
error=program not found
```

Consequently narrow mobile spacing/navigation fit, real keyboard/focus behavior, the complete desktop/mobile English/Danish session, and shared persisted dinners with existing Plan remain unverified. Continue tasks 6.4, 7.2 and 7.3 once the browser runtime is available. Do not archive the change yet.

The additive rollback path is documented in `api/src/EzDinner.Query.Core/DishRecommendationQueries/README.md`.

### Browser runtime repair attempt

The current helper exists at `C:\Users\mikke\AppData\Local\OpenAI\Codex\bin\faa963e871dd422c\codex-windows-sandbox-setup.exe`. The configured browser backend reports `0.158.0-alpha.2.1`; the shell backend is `0.160.0`. The documented `windowsSandbox/setupStart` recovery on the browser backend completed successfully in elevated mode. A subsequent browser retry still reported failure to launch the helper by its bare filename.

Added the matching helper directory to `[mcp_servers.node_repl.env].PATH` in the user Codex configuration. The configuration parses, and a fresh process resolves the correct helper. Original configuration was backed up to `C:\Users\mikke\.codex\config.toml.browser-helper-20261003.bak`. No sandbox policies were disabled or changed.

The active browser server still reports `trusted Node process exited unexpectedly` after a kernel reset. This reset does not restart the MCP server with its revised launch environment. Reload Codex/server configuration, then rerun `await cua.getState()` to verify the repair before attempting visual acceptance. The browser tasks remain unchecked.

After the user restarted Codex, `await cua.getState()` succeeded with no helper or kernel error. The inventory returned no browsers. Attempts to open the in-app browser or Chrome returned `Browser is not available`. The Browser plugin's elevated read-only diagnostics found Microsoft Edge installed and running, its native-host registration correct, and its ChatGPT extension missing from the Default profile. Chrome was not reported as installed. Visual acceptance is now blocked by browser connectivity rather than the sandbox helper. The required browser extension must be installed/enabled and connected before continuing.

### Live acceptance progress after extension installation

Edge connected successfully after the user installed the extension. Started the frontend on localhost:3000 and the API on localhost:7071 with the HTTP functions needed for acceptance. The normal Functions build loaded 18 functions; starting from the source directory with `--no-build` had loaded none. The user signed in through the normal authentication flow.

Observed the desktop workspace with real family/catalog/history data, nine dates, multiple recommendation cards and a long dish name. Opened Foobar's inspector, verified missing notes/recipe copy, and used Escape to close it and return focus to the originating dish button. English and Danish navigation were inspected at a 360x800 viewport; all five entries were visible and the page had no horizontal overflow. Opened the compact Danish overview and navigated to the following week, exposing both adjoining weekends. Later screenshots also exposed the existing layout's loading overlay left active when its family request failed during API downtime; a full reload with a healthy API is still required before final mobile interaction acceptance.

Automatic approval review initially rejected real dinner writes and transmission to Anthropic. The user then explicitly approved temporary Foobar assignments on 3-4 October 2026 with restoration, and the potato-pairing/no-fish/side-dish requests using family dishes, notes, snapshots, ratings, usage, wishes and the nine-day plan sent to Anthropic. Added both temporary dinners through the new picker; duplicate actions became disabled. Navigated to existing Plan and verified both saved names there, then returned to the new workspace. Removed the Saturday dish, successfully undid the removal through conditional undo, and removed both temporary assignments. Both days were verified empty again. The request preferences and prior recommendation cards remained visible through removal and undo.

Live inspection found inherited full-range statistics labeling a future assignment as last served, and missing catalog ratings because the list contract did not include rating counts. Added an optional exclusive `before` statistics boundary used only by the workspace, preserving legacy unbounded callers, and added list rating-count metadata without changing its five-point rating scale. Regression tests were observed failing before the fixes. Updated checks: frontend full suite **94 passed**, lint **zero errors / one existing warning**, focused backend feature suite **58 passed**. A restarted API visibly showed past-only history and correctly scaled catalog ratings.

The approved live potato-pairing request returned 502. Safe diagnostics established `MALFORMED_PROVIDER_RESPONSE` with an inner `System.Text.Json.JsonException`, rather than an HTTP authentication/connection failure. Previous results and editable preference remained visible. Additional parse-location diagnostics were added without logging family evidence, raw completions, exception messages or credentials.

The subsequent build compiled, but Windows application control blocked loading `api/src/EzDinner.Functions/bin/output/EzDinner.Functions.dll`: error `0x800711C7`. Code Integrity events 3077/3033 confirm Enterprise signing-level rejection under policy `{0283ac0f-fff1-49ae-ada1-8a933130cad6}`. The failing host was stopped. No application-control settings were disabled or bypassed. The latest parse-location diagnostic cannot be exercised until this locally built assembly is permitted through the machine's normal application-control process. Live potato/no-fish/side completion, full mobile/desktop sessions and remaining edge-case acceptance remain pending; tasks 6.4, 7.2 and 7.3 are still open.

On the next continuation, retried the existing assembly with `func start --no-build` from its generated `bin/output` directory. At 15:21 UTC on 3 October 2026, the worker again rejected the same DLL with `0x800711C7`; the host was stopped. Read-only `CiTool.exe --list-policies --json` returned access denied (`-2147024891`), so no policy display name was established. No code or security-policy changes were made during this retry. Progress remains 22/25 tasks.

After the user ran `dotnet build`, confirmed that Debug output was newer than the Functions `bin/output` assembly. Ran normal `func start` from the source project to rebuild the host output through the standard startup process. At 15:24 UTC, Windows again blocked loading the rebuilt Functions assembly with `0x800711C7`. Stopped the failed host; a successful compile does not establish permission to load the assembly.

### Runtime recovery and continued acceptance

The user demonstrated successful `dotnet run`. Starting the project with that standard command loaded all 18 selected HTTP functions on port 7071, using its Debug output. The previous conclusion that the code could not run was too broad: the rejection affected the separate `func start` output. No application-control policies were changed.

With the working host, safe diagnostics identified the live provider response as `MarkdownFence`, failing JSON parsing at line 0 / byte 0. Added narrowly bounded unwrapping of one complete plain/JSON Markdown code fence. Three acceptance cases failed before the fix; all provider and backend feature tests then passed (64). Prose outside fences, malformed contracts, numeric enums, canonical IDs and evidence validation remain strict. No provider content or credentials were logged.

Live English and Danish requests exercised potato pairings, cumulative no-fish intent and independent Side role. Prompt refinement removed opaque-title filler while permitting culinary inferences for recognizable dishes. The final English pairing response contained Hotdogs, Tortillas and Tærte with labelled inferences and no fish or opaque-title filler. The Side request returned a localized no-match for this catalog, which has no classified Side candidates. Model prose remains nondeterministic; these observations do not establish general recommendation accuracy.

At 360x800, both navigation locales fit. Found and fixed a clipped Danish reset action by wrapping request/scope action rows. Found and fixed truncated long dish names in recipe and assignment dialog titles. Actual mobile Enter opened the long-name inspector; Escape closed it and returned focus to its dish button. Screenshots confirmed the full wrapped title. Selecting Saturday then Sunday exposed Keep/New scope controls before another request. Keeping preferences retained the request; resetting cleared it explicitly.

The approved temporary Foobar assignment on 4 October was repeated on mobile: add, remove, keyboard undo, remove again. The final overview verified 3-4 October empty. No temporary assignments remain. Next-week navigation showed both adjoining weekends and retained the selected date without automatic advance. Previous desktop acceptance established shared saved dinners with existing Plan. Empty-catalog English/Danish component fixtures now additionally verify that nine days and unavailable saved-dish labels remain visible without any writes. Existing fixtures establish provider failure, pending-family/account response isolation and storage conflicts; those edge cases were not deliberately induced against live family data.

### Independent review and unresolved mutation provenance

Independent read-only review identified three material issues. Fixed assignment being enabled before the current family/window was successfully loaded: mutation availability now depends on a successful current-scope read, with loading/read failure disabling assignment/removal controls while inspection and navigation remain available. Fixed retry reusing obsolete day/language/preferences: it now sends current accepted scope, locale, edited constraints and role, retaining appropriate mode/exclusions. Regression tests were observed failing before both fixes. Updated frontend full suite: **100 passed**; lint **zero errors / one existing warning**.

Remaining issue: legacy add/remove return empty 200 even when the dish membership was already changed by another member. The workspace currently builds its undo from cached state and can therefore undo another member's action after a server no-op. Undo-time ETag checking does not prove that the original mutation happened. Task 6.3 is reopened; final acceptance remains pending.

Proposed extension, pending scope approval: add an opt-in mutation contract on the existing add/remove routes for this workspace. Compare expected canonical state (including opt-out) on the server; return Changed/NoOp/Conflict, with actual before/after only after a successful conditional save. Legacy callers retain their existing empty-200 contract. Add conditional creation for absent days, using a stable family/date identity for newly created Dinner aggregates across creation callers; existing persisted IDs are retained. Random new IDs cannot prevent concurrent first-day creation, so receipt-only or random-ID CreateItem would leave that race unresolved. Update domain/application/infrastructure/Functions, typed workspace repository/state and concurrency regression tests together. This shared creation change is beyond the current design and has not been implemented or silently accepted as a limitation.

Final backend recheck after the prompt refinements compiled, but xUnit could not load the refreshed test assembly because Windows application control rejected `EzDinner.UnitTests.dll` with `0x800711C7`. The adapter returned exit 0 while reporting no matching tests; this is **not a passing test run**. The last executed feature suite remains 64 passed before those final prompt refinements. The API itself restarted successfully via `dotnet run` with all 18 HTTP functions and remains running. Frontend final checks executed 100 tests successfully and lint reported zero errors / one existing warning. No policy bypass or output-path workaround was attempted.

Tasks 6.4 and 7.3 are complete based on the browser interactions and explicitly distinguished controlled edge-case evidence above. Task 6.3 was reopened for mutation provenance; task 7.2 remains open until the server-confirmed mutation flow can be implemented and the final experience rechecked. Progress is **23/25**; do not archive.

### Approved extension and final verification — 3 October 2026

The user approved the mutation-provenance/shared-creation extension with Continue. Implemented opt-in conditional add/remove responses on the existing routes, server expected-state comparison including opt-out, Changed receipts containing actual before/after states only after successful persistence, and NoOp/Conflict without receipts. Workspace undo uses those server states. Wishes clear locally only for Changed; assignment wishes are granted server-side only after a successful conditional assignment. Result state limits are checked before persistence. New dinner identities are stable for family/date; historical IDs remain unchanged. Legacy absent-day add/remove/set-opt-out/remove-opt-out writers use bounded create/ETag retries that reload and reapply intent, preserving the existing HTTP responses. Documented the extension and mixed-version rollout/rollback limits in design and the query README.

Independent read-only review found the legacy creation race, local wish clearing on NoOp/Conflict, and after-state validation timing; all three were corrected and regression-covered. The subsequent review reported no remaining material findings. First-day creation races, no-op membership, unseen opt-outs, conflicting saves, oversized resulting menus, canonical receipts, authorization and legacy response compatibility are controlled tests, not deliberately induced races against live family data.

Final backend execution: **110 passed / zero failed / zero skipped** for `DishRecommendationTests|DinnerTests|WishlistTests`, including compilation of the Functions application. The API restarted successfully via `dotnet run` with the same 18 HTTP functions. This replaces the earlier blocked feature-test run as current evidence. The unrelated full-suite aggregate serialization failure and dependency vulnerability warnings remain as previously documented and as the user instructed. This filtered result does not claim that the full backend suite passes.

Final frontend execution: **111 passed across 12 files**. Lint: **zero errors / one existing warning** at `DishMetadataCard.vue:135`. A plain npx type-check attempt tried registry access and failed under network restrictions; the existing cached vue-tsc executable then ran successfully as a tool and reported the same baseline project type errors (Dish detail/ratings, LanguageSwitcher, existing Plan, Markdown formatting, Nuxt config, dishes page and Vite/Vitest/Node types). No errors were reported in the new feature files. Type checking is not a passing project check.

Final live desktop Danish session: inspected Foobar (missing notes/recipe represented honestly), returned to the same exploration, assigned the approved temporary dish on both 3 and 4 October, removed only 4 October and undid that removal while preserving 3 October. Existing Plan showed both persisted assignments. Removed both through existing Plan, verifying its legacy mutation path. Final English 360x800 session repeated assignment on 4 October and undo; the saved overview confirmed 3–4 October empty. No temporary assignments remain.

Live English mobile potato/no-fish request returned Hotdogs, Tacos, Tortillas and Tærte, with culinary inference labels and no fish result. Explicit Side scope with a cumulative side-dish request returned NoMatch, appropriate to the supplied catalog. Changed the selected day after a scoped request: Keep preferences/Start new request controls appeared, and keeping retained both preferences. Navigated the 5 October window with 3–4 and 10–11 October weekends visible. Earlier desktop/mobile Danish provider and keyboard evidence above remains applicable.

Visual checks found and fixed the mobile sticky week button showing underlying text while scrolling: it now has an opaque surface background, confirmed in the scrolled screenshot. Locale switching previously left explanations in the prior language: now it cancels pending work and clears prior-language results while retaining editable intent; the regression was observed failing before the fix and passed afterwards. Live locale switching verified cleared results, and the browser returned to Danish at its normal viewport. Independent review found no material issues in either final change.

Saved screenshots: [English mobile](evidence/mobile-en.png) and [Danish desktop, both temporary dates empty](evidence/desktop-da.png). A final optional Refresh ideas action was declined by automatic browser approval review because it could send family context to the recommendation provider; it was omitted. Cleanup, locale restoration, empty-date verification and screenshot capture completed without that action. No approval remains necessary for the completed acceptance work.

Tasks 6.3 and 7.2 are now complete with the above server-confirmed mutation and experience evidence. **25/25 tasks complete.** No commit or archive was performed. The local API remains running for inspection.
### Backend/frontend review fixes � 3 October 2026

All five findings from the backend DDD and frontend Vue reviews are fixed. Undo receipts now carry persisted change provenance and reject stale reassignment/opt-out restoration while preserving unrelated menu edits. Recommendation role/name filters reduce candidates before evidence budgeting; budgeting uses the exact provider payload with aggregate history. Returning to the cached workspace refreshes saved dinners/catalog without reranking ideas. Family/account changes clear unsubmitted drafts. Recipe return preserves draft/search and restores focus to the originating dish link; the persistent Nuxt KeepAlive wrapper caches only the named planning page.

Final checks: frontend 134 tests passed across 14 files; production build passed; lint zero errors and one existing DishMetadataCard.vue warning. Backend full suite compiled and ran: 321 passed, one existing AggregateRootTests.Instance_IsInitialized_CanBeDeserialized failure (322 total). Provenance serialization, HTTP receipt/undo roundtrips, stale receipts, filtering/budgeting, identity resets and activation refresh are regression-covered.

Live browser verification with the backend started using dotnet run confirmed recipe-return draft/search preservation and keyboard focus restoration. Desktop/mobile layouts were inspected in English and Danish; the viewport and Danish preference were restored. No dinner assignments were changed during this review-fix check. Temporary frontend/backend servers were stopped. Concurrent live writes were not induced; their behavior is covered by domain, command, HTTP and persistence tests.
