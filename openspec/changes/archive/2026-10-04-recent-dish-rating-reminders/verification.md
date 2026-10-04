# Implementation verification

## Status — 2026-10-04

25/26 tasks complete. Tasks 0–5 and checks 6.1–6.2 are implemented and verified within the limits below. A live authenticated browser smoke test now verifies rating, dismissal, preference persistence and reminder-link focus. Task 6.3 remains open for the rest of the release lifecycle, including actual push dispatch and signed-out notification entry. The user requested archiving with that task still unchecked. The production scheduler must remain disabled until release verification is complete.

The implementation includes recent-dish selection, per-user conditional Cosmos state, authorized CQRS APIs, the Home quick rating/dismissal card, a separate opt-in preference, durable at-most-once push attempts, localized payloads, and safe notification navigation through sign-in to the existing personal rating row. Existing catalog-preference and archived recipe-snapshot work is preserved.

Archive follow-up: the user selected main-spec synchronization. The four delta capabilities are synced: one notification-navigation requirement modified, three preference requirements added, and two new reminder main specs created with thirteen requirements. Every delta requirement matches its main spec, and all four touched main specs pass strict validation. Repository-wide `openspec validate --specs --strict` still fails for eighteen unrelated legacy specs missing required Purpose/Requirements structure; all eighteen failures were confirmed against their versions in `HEAD`.

## Checks passed

- Both normalized push main specs parse using `openspec show --type spec --json`. `openspec validate recent-dish-rating-reminders --strict` passes; their existing requirement/scenario content is preserved.
- Backend unit suite: `dotnet test api/test/EzDinner.UnitTests/EzDinner.UnitTests.csproj --no-restore` passes. Direct policy/aggregate tests, Cosmos serialization/storage-boundary tests, query/command/HTTP tests, and 21 delivery tests cover eligibility, conflict retries, current access, duplicate/concurrent reservations, crash/ambiguous send, subscription replacement, final-check races, expiry and independent recipient failures.
- `dotnet build api/src/EzDinner.Functions/EzDinner.Functions.csproj --no-restore` passes. Generated WorkerExtensions restore requires approved network access even with `--no-restore`. Existing NuGet advisory warnings remain.
- The user-approved existing AggregateRoot test fixture repair uses a JSON constructor accepting the persisted ID; production aggregate behavior is unchanged.
- Frontend `npx nuxt prepare`, full `npm test` (190 tests), `npm run lint` (zero errors; one existing DishMetadataCard warning), and `npm run build` pass. The rating-control regression test uses the real Vue i18n adapter, matching the Nuxt app.
- `git diff --check` passes.

## Browser review

Edge reviewed the actual Home hero, reminder container/card, DishPill, DishRating, family rating rows and notification settings in a controlled local Vite harness. It loads the existing global tokens, app typography, Vuetify theme and the same icon font. It substitutes controlled repositories/authentication; it does not represent a live backend/sign-in test.

Reviewed at 1280×1000 and 375×1000 in English/Danish. The long dish name uses the existing pill truncation/navigation pattern; Danish preference text wraps completely. Mobile has no horizontal overflow (document width 360 within a 375px viewport). Hearts remain compact, separate from the name and dismissal action. Existing family-row presentations and permissions are preserved.

Verified controlled failure retains the card and selected rating; successful retry advances to Tacos and focuses the container; dismissing the final candidate hides the card. Dish-name activation reports the normal dish route without saving. Preference activation persists the controlled confirmed state. Reminder entry focuses `my-rating` without changing a rating. Slow-load and localized read-error states leave the hero/tomorrow/settings usable. Keyboard behavior is additionally tested with the real Vuetify rating component.

Evidence: [desktop English](evidence/desktop-en.jpg), [mobile Danish](evidence/mobile-da.jpg), [mobile read error](evidence/mobile-load-error-da.jpg). Harness: `web/tests/visual/rating-reminders.html`; `?lang=da`, `?slow=1`, and `?fail=1` control review scenarios.

Limitations: the browser viewport override does not emulate a coarse pointer or a physical touchscreen; actual touch-device behavior remains unverified. The harness does not exercise authentication or live services. The additional actual-app smoke test below exercises the authenticated Nuxt shell and local backend.

## Live browser smoke test requested for review

Started `dotnet run` in `api/src/EzDinner.Functions` (http://localhost:7071) and `npm run dev` in `web` (http://localhost:3000). The existing Edge session was already authenticated. NuGet restore required approved network access. The local Cosmos endpoint works when the backend runs with that access; the earlier sandbox port inventory was not evidence that Cosmos was unavailable.

Home initially loaded before the Functions host was ready. After host readiness and reload, normal authenticated family/dish/dinner/reminder queries succeeded. The new container was absent: the first dismissal returned Cosmos 404 for `EzDinner/RatingReminders`. Running the specified existing local `PUT /api/migrate` returned 200 and provisioned the container; retry then succeeded. The UI retained the candidate and exposed localized retry during that failure.

Smoke checks passed using the existing local sample catalog and temporary menu assignments for 3 October:

- Assigning the long-name sample dish and Tacos through the real planning UI produced one recent reminder on Home.
- Clicking the dish name opened the existing detail page and retained an unrated personal row.
- Saving a rating advanced to Tacos, announced success and restored focus to the reminder container without navigation. A keyboard half-step was saved and confirmed as 3.5 hearts after a fresh detail-page load.
- Dismissing Tacos hid the card. Reloading Home confirmed both rated and dismissed candidates remained resolved.
- The existing subscriber's separate reminder preference started off. Turning it on persisted across reload; it was then restored off. No browser notification permission prompt was triggered.
- A recognized reminder URL opened the accessible dish and focused `my-rating` after async data loading; the saved personal rating remained 3.5. This checked direct-link entry with an authenticated session, not an actual notification tap or fresh sign-in.
- Desktop (1280×900) and mobile (375×900) layouts were reviewed in the actual shell. Mobile document width was 360px within the 375px viewport. Danish settings wrap; English/Danish heart labels update correctly.

The live app exposed a localization defect missed by the first isolated harness: pretranslated `{0}` placeholders were reinterpreted as translation keys by Vuetify's Vue i18n adapter. `DishRating` now passes a translation key with positional values in both locale files. The regression test and review harness use the same adapter. Browser verification confirmed `3.5 af 5 hjerter` / `3.5 of 5 hearts`, without literal placeholders.

Evidence: [live desktop Home](evidence/live-home-desktop-da.jpg), [live mobile Home](evidence/live-home-mobile-da.jpg), [live mobile settings](evidence/live-settings-mobile-da.jpg), [live reminder entry](evidence/live-reminder-entry-mobile-da.jpg), [English mobile detail](evidence/live-detail-mobile-en.jpg).

Cleanup: removed both temporary menu assignments and a temporary Hotdogs assignment used for the final settled desktop/mobile screenshots; restored Danish and reminder preference off. The local sample dish retains the deliberate 3.5-heart personal test rating; the Tacos dismissal cutoff remains persisted for 3 October. Both dev servers were left running after review; the frontend subsequently exited at Node's roughly 4GB heap limit, while the backend remains running. The controlled Vite harness server was stopped.

The existing tonight timer listener reports unavailable Azurite at port 10000; HTTP APIs work. No live notification send, new scheduler or production configuration was exercised.

## Vote confirmation refinement

After the user's live review, the Home card now retains the submitted hearts while saving and refreshing. A confirmed save shows the existing localized success text with a check mark for 1.2 seconds, then fades out over the existing 300ms motion token before the next candidate appears. Reduced-motion preferences disable the fade through the existing token override. Controls stay disabled during confirmation and departure, preventing a second tap from voting on the next dish. Failed saves retain the selection and retry action; switching identity or unmounting cancels the confirmation timer. Focus is restored only when the user has not moved outside the reminder.

The full frontend suite passes with 190 tests. Component regressions cover the confirmation interval, delayed persistence, last-card removal, repeat-action protection, failed-save retry, family changes, timer disposal and focus moving elsewhere. Lint and production generation also pass.

Edge reviewed the same components in the controlled preview at 1280x900 in English and 375x900 in Danish. The saved four-heart selection and confirmation remained visible; the next dish appeared afterwards, and the final voted card entered the actual 0.3s CSS leave transition before removal. Mobile document width remained 360px within the 375px viewport. Screenshots: [desktop saved vote](evidence/vote-confirmation-desktop-en.jpg), [mobile saved vote](evidence/vote-confirmation-mobile-da.jpg). This refinement review used controlled repositories and did not add further persisted sample ratings. Physical touchscreen behavior remains unverified.

## Open live verification (task 6.3)

The new Cosmos integration test covers persisted reload and competing ETag writes. Running that suite is currently blocked before execution by the existing integration project's Casbin.NET 2.5.1 dependency: restore reports NU1605 because Infrastructure requires 2.19.1. Its local development configuration is also absent; committed configuration only names `EzDinnerTests`, with no Cosmos connection string or Graph client secret. The actual-app smoke test above confirms live Cosmos persistence through the running API, but does not replace the competing-write integration test.

Approval to repair the separate integration suite remains pending. Further live verification must complete later-date reappearance, retroactive replacement, seven-day expiry, duplicate delivery and actual notification entry through sign-in/different family. Confirm persisted reminder state and transport counts; do not substitute controlled UI mocks for these checks.

`api/src/EzDinner.Functions/RATING_REMINDER_DELIVERY.md` documents container provisioning, the proposed external 15:00 Copenhagen DST-aware scheduler, secured shared-secret header, at-most-once retry semantics and rollback. No scheduler or deployment was performed as part of browser verification. Keep the scheduler disabled until live verification is complete.
