# Proposal

## Why

Family members can forget to rate dishes after dinner, leaving personal preferences incomplete. A small reminder for recent planned dinners makes rating a quick action on Home, with an optional push that opens the existing rating control directly.

## What Changes

- Show one compact Home card for the current user's most recent unrated dish from the previous seven calendar dates. Selecting the existing heart rating saves immediately; successful rating or "Not now" reveals the next eligible dish.
- Use past planned dinners as evidence, without requiring meal confirmation. Deduplicate dishes and persist personal dismissal across devices; a later dinner occurrence makes an unrated dish eligible again.
- Add separately opt-in rating push reminders, evaluated daily the following afternoon and limited to one per user per day and once per dinner occurrence. Rating and dismissal suppress subsequent delivery.
- Open notification targets on the normal dish detail page, bringing the user's editable rating row into view, including after sign-in and family resolution.
- Evaluate retroactive menu edits from their dinner date: replace removed candidates, include newly added recent dishes, and defer push until the next scheduled run.
- Keep the existing visual language, heart ratings, navigation, dinner/wish notifications, and English/Danish localization.

## Capabilities

### New Capabilities

- `recent-dish-rating-reminders`: Personal eligibility, seven-day window, deduplication, immediate Home rating, persistent occurrence dismissal, and retroactive menu changes.
- `rating-reminder-push`: Daily optional delivery of eligible personal rating reminders, durable delivery limits, and direct entry into the user's rating row.

### Modified Capabilities

- `push-subscription-management`: Add an independent persisted rating-reminder opt-in alongside the existing browser push subscription controls.
- `dinner-push-delivery`: Extend notification-click behavior to honor a safe in-app destination while preserving existing notifications without a destination.

## Impact

- Frontend: `web/pages/home.vue`, dish rating components and detail route, repository/composable boundaries, profile notification settings, auth return navigation, and `web/public/sw.js`; new text in both locale files.
- Backend: a pure reminder-selection domain service and a per-user reminder aggregate; CQRS query/commands; Cosmos persistence with conditional writes for dismissal and push attempts; authenticated reminder/preference endpoints; a secret-protected delivery endpoint.
- Operations: provision a reminder-state container and a daily external scheduler using the existing push delivery approach. Proposed default: 15:00 Europe/Copenhagen, DST-aware; no new timer that prevents scale-to-zero.
- Validation: domain boundaries and lifecycle tests, repository/command/HTTP tests, Vue interaction and navigation tests, service-worker tests, and desktop/mobile visual review during implementation.
- Spec compatibility: normalize the two existing push main specs' legacy section structure, preserving all existing requirements, so the new deltas can later sync/archive. Validation identifies this as an archive prerequisite; this proposal turn changes only its own planning artifacts.
- No new package is required by the planned design. Meal confirmation, historical rating backlogs, re-rating prompts, and a notification inbox are outside scope.
