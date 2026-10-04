# Design

## Context

See `proposal.md` for motivation and `specs/` for behavior contracts. The approach is grounded in these existing paths:

- `web/pages/home.vue:84` composes the dinner hero, tomorrow preview, and quick stats in a vertical main column. A reminder can reuse that hierarchy rather than introduce another navigation destination.
- `web/components/Dish/DishRating.vue:2` is the existing read-only five-heart presentation. `DishFamilyRatings.vue:35` saves ratings through the dish repository; its current-user row is highlighted at line 75, and its independent accounts are read-only at line 109.
- `web/pages/dishes/[id].vue:28` loads details using the active family. It currently loads on mount and redirects to the catalog whenever the active family changes; notification navigation must deliberately resolve family before loading and react to route changes.
- `Dish.SetRating` (`api/src/EzDinner.Core/Aggregates/DishAggregate/Dish.cs:80`) stores one rating per family member. Zero is a valid recorded value; it cannot be used as a proxy for absence.
- `Dinner` exposes calendar date, menu, and opt-out (`api/src/EzDinner.Core/Aggregates/DinnerAggregate/Dinner.cs:20`). There is no eaten/completed-meal state.
- `web/public/sw.js:80` displays push without storing a destination; its click handler at line 87 focuses any existing window or opens `/`. `web/middleware/auth.global.ts` redirects signed-out users to `/`, while `web/layouts/landing.vue` redirects signed-in users to `/home`.
- Push subscription save replaces the existing subscription for a user; `PushSubscriptionRepository.GetByUserIdAsync` returns one subscription. Preference, dismissal, and delivery history therefore cannot live solely on an ephemeral subscription document.
- Existing specs describe an externally scheduled, secret-protected `push/send-tonight` endpoint. Source also retains `PushSendTonightTimer`; this proposal adds no additional timer or assumption about its deployment. The current timer comment/schedule and spec differ; changing tonight's timing is outside this change.
- `web/assets/global.scss` is authoritative for current tokens, including the green primary and `--color-heart`. Use the actual token values rather than older prose descriptions in project instructions.
- Existing push main specs use an older delta-style layout without a Purpose section, so filtered JSON reads reject them. Their complete requirement blocks were read from disk; only change-local deltas are authored here.

## Goals / Non-Goals

**Goals:**

- One deterministic eligibility policy shared by Home queries and push delivery.
- A rich per-user aggregate that owns persistent dismissal, preference, and delivery limits, independent of subscription replacement.
- Thin HTTP entry points and Vue route pages; explicit state/action contracts and conditional persistence.
- Extend established dish components and navigation while retaining desktop/mobile parity and localized interaction feedback.

**Non-Goals:**

- No meal confirmation model, per-meal rating history, generic notification platform, or new notification inbox.
- No changes to legacy root Nuxt code, Graph-backed user storage, dinner notification schedule, multi-device push subscription policy, or recipe UI.
- No global frontend redesign or broad conversion of existing Vue components.

## Decisions

### 1. Derive candidates from the current plan with a single pure domain service

Create `Core/DomainServices/RatingReminders/RatingReminderSelectionService`. It receives already-loaded dinners, dishes, the authenticated user's reminder aggregate, and an explicit `LocalDate today`. It does not read repositories, clocks, authorization, HTTP, or logging. Its output uses immutable `RatingReminderOccurrenceValueObject` values containing family ID, dish ID, and dinner date; query-layer results add dish names and navigation information.

Selection order is significant:

1. Restrict dinners to `[today - 7, today - 1]` and current planned menu items.
2. Join only accessible, active, nondeleted dishes from that family; exclude any dish with a rating entry for this user, including zero.
3. Group by dish ID and retain the latest occurrence before applying dismissal or push history.
4. Exclude occurrences at or before the personal dismissal cutoff for that family/dish.
5. Sort newest date first, then by a fixed ordinal representation of dish identity. Home receives the whole bounded distinct-dish queue but displays only the first.
6. For push, additionally exclude already attempted occurrences and enforce preference/daily limits through the aggregate. Sending a push does not affect Home eligibility.

Query orchestration in `Query.Core/RatingReminderQueries` loads the seven-day range using the existing dinner range repository, loads the family dish catalog once, loads the user's state, invokes the service, and returns materialized results. Both query and send command use an injected `IClock` and the same Copenhagen date conversion outside Core. No all-history usage-stat scan is required.

**Alternative:** frontend-only filtering from Home's existing stats cannot distinguish personal absence reliably, preserve cross-device dismissal, or enforce push rules. Separate frontend/backend rules would drift.

### 2. Model per-user reminder state as an aggregate

Create `Core/Aggregates/RatingRemindersAggregate/RatingReminders`, identified by the authenticated user ID. Its private state consists of:

- A global `PushEnabled` preference, default false.
- Immutable `DismissedRatingReminderValueObject` values: family ID, dish ID, and dismissed-through dinner date.
- Immutable `RatingReminderPushAttemptValueObject` values: occurrence and attempted-on Copenhagen calendar date, plus a unique attempt identity if needed for correlation.
- The latest push-attempt day across all families, to enforce the global per-user daily limit.

`DismissThroughOccurrence`, `SetPushEnabled`, `TryRecordPushAttempt`, and `PruneExpiredOccurrences` enforce invariants. Explicit factory construction creates a new default state; a dedicated persistence hydration path reconstructs existing state without using it for new users. No repository or transport enters these methods. Existing `Dish` remains the source of truth for ratings; do not copy ratings into the reminder aggregate.

Dismissal stores a cutoff rather than removing only one candidate: when a dish was planned Tuesday and Thursday, dismissing Thursday must not show Tuesday next. A newer dinner date passes the cutoff. Removing/restoring the same dish on the same date retains dismissal; replacing it with another dish has another identity. Adding an older occurrence retroactively does not bypass a newer dismissal. This is a documented default resolving duplicate-history behavior.

An absent aggregate represents default state on reads without writing. Persist the aggregate when preference, dismissal, or a delivery attempt changes. Prune occurrence state older than the eligible window during successful writes; keep the preference and daily limit. Calendar-day retirement must not reset today's cap.

**Alternative:** storing suppression on `Dish` mixes personal reminder lifecycle into family catalog invariants; storing it on `PushSubscription` loses it on resubscribe. A separate aggregate gives these rules one consistency boundary, including the cross-family daily limit. The aggregate contains no copy of Dinner or Dish, only IDs and dates.

### 3. Use conditional Cosmos writes for the aggregate consistency boundary

Add a `RatingReminders` Cosmos container with `/id` partition key, where document ID and partition key are the authenticated user ID. Full-aggregate point reads avoid cross-partition history searches. `IRatingRemindersRepository` loads the complete root with an opaque revision and supports create-if-absent and save-if-unchanged, implemented using direct Cosmos create and ETag-guarded replace. Follow the existing conditional dinner repository conventions; do not use EF existence checks or unconditional upsert for reminder mutations.

Commands reload and reapply the domain action on bounded conflict retries. Concurrent dismissal and preference writes must preserve each other's changes. Repeated dismissal is idempotent. Duplicate scheduler invocations contend on the same user document, so only one can record an attempt on a date, even if the transport's family association changes.

DI registers repositories, query assembly, clock/date boundary, commands, and push transport dependencies. New commands are constructor-injected into Functions, rather than constructed inside HTTP methods. Provision the new container through the existing migration/provisioning path.

**Alternative:** one document per occurrence permits simple uniqueness but cannot atomically enforce a global daily limit plus dismissal/preference updates. The small, pruned per-user aggregate avoids a second coordination mechanism.

### 4. Preserve CQRS and existing rating authorization

Planned API contracts (all calendar dates are ISO `yyyy-MM-dd` strings):

| Endpoint | Responsibility |
| --- | --- |
| `GET /api/families/{familyId}/rating-reminders` | Authenticated query returning `{ today, reminders: [{ dishId, dishName, dinnerDate }] }` for the caller. |
| `PUT /api/families/{familyId}/rating-reminders/{dishId}/dismiss` with `{ dinnerDate }` | Idempotent personal dismissal, returning 204 after persistence. |
| `GET /api/rating-reminders/preferences` | Return the caller's `{ pushEnabled }`, default false when absent. |
| `PUT /api/rating-reminders/preferences` with `{ pushEnabled }` | Persist the caller's global preference and return confirmed state. |
| `POST /api/push/send-rating-reminders` | Secret-protected external scheduler entry point; no user identity from its body. |

Use existing `Resources.Family/Read` and `Resources.Dish/Read` or `Update` permissions for family access and reminder actions, together with actual family membership and dish-family validation. Preference APIs use authenticated self identity. No new Casbin resource/policy migration is required. Never accept a reminder owner from a client payload. The send flow confirms membership, autonomy/account identity, and current access before sending.

Home rates through the existing `PUT /api/dishes/{dishId}/rating` contract and `Dish.SetRating`, using the current account's family-member ID. No parallel rating endpoint or per-meal rating is introduced. Existing HTTP rating code remains functionally unchanged; reminder decisions never enter it.

Functions parse/authenticate/map only. Dismissal and preference commands in `Application/Commands/RatingReminders` load roots, invoke methods, and conditionally save. Query-layer response models live in `Query.Core`, not Core. The scheduled send command coordinates query context, domain selection, aggregate reservation, and transport.

Validate ISO dates before domain invocation. A new dismissal must reference a past occurrence within the eligible window and a dish in the requested family; never allow a client to pre-dismiss a future dinner. Already dismissed requests remain idempotent. If the referenced menu occurrence was removed while its card was visible, acknowledge the stale action without creating a new suppression entry and refresh the queue. Invalid IDs/dates return localized mapped validation errors without state writes.

**Alternative:** separate authorization resources would add a policy rollout without granting meaningful new permissions; a dedicated rating command rewrite is unnecessary to this behavior.

### 5. Compose a small Vue feature and reuse the heart control

Use Composition API and `<script setup lang="ts">`. The feature map is:

| Unit | Responsibility and public contract |
| --- | --- |
| `web/pages/home.vue` | Compose `<HomeRatingReminder>` after the dinner hero and before tomorrow; retain existing layout/data responsibilities. |
| `web/components/Home/RatingReminder.vue` | Container: connect account/family, composable, notifications of success/error, and one card. |
| `web/components/Home/RatingReminderCard.vue` | Presentation: typed `reminder`, `pendingAction`, and selection props; emit `rate(value)` and `dismiss()`. No repositories or business filtering. |
| `web/composables/useRatingReminders.ts` | Own readonly queue/request state and explicit `refresh`, `rate`, `dismiss` actions. Derive current reminder with `computed`. |
| `web/repository/rating-reminders-repository.ts` | Typed API calls and Zod validation of reminder/preference responses, wired through `useRepositories`. |
| `web/components/Dish/DishRating.vue` | Extend the existing control with opt-in editable/disabled/accessibility props and its normal model update event; keep existing callers read-only by default. |
| `web/components/Dish/DishFamilyRatings.vue` | Reuse `DishRating` for editable/read-only rows and provide a stable current-user focus target; retain family-member permissions and layout. |
| `web/components/NotificationsToggle.vue` | Retain browser subscription control and compose a focused rating-reminder preference control below it. |

Use immutable queue replacement and discriminated request/action states instead of unrelated loading/saving/dismissing booleans that allow overlapping mutations. Reactive account/family inputs are normalized inside effects; `watch(..., { immediate: true })` handles entry and identity changes. Cancel superseded reads and use generation/identity checks for mutations that finish after a family switch. A successful PUT stays successful even if the follow-up GET fails: remove the resolved dish locally, invalidate/refetch, and report refresh failure separately without claiming the saved action failed.

Refresh on visibility/focus and Copenhagen midnight, cleaning up listeners/timers on unmount. The server's returned `today` supplies the authoritative window; client scheduling uses Luxon with Copenhagen zone. No module-level reminder queue persists between users. Use a local composable rather than a new Pinia cache because Home owns the queue; use existing app/family stores for selection, retaining store reactivity.

Reuse `DishPill` for linked dish naming and extend `DishRating` instead of copying raw `v-rating` markup. Preserve five hearts, half increments, current heart accent, direct inline gap, and existing compact button wrappers. Keep the touch-height rule from `global.scss`. `DishRating`'s model uses the modern Vue model contract while preserving existing `modelValue` callers. Share control styles and preserve the established family row styling. Localize existing rating feedback touched by this integration, which is currently hardcoded in `DishFamilyRatings`.

**Alternative:** placing fetch/write/queue logic in `home.vue` would add another independent responsibility. A large catalog `DishCard` carries unnecessary metadata/actions for a single quick rating; reusing its established primitives fits the existing Home context.

### 6. Keep the reminder visually quiet and action-oriented

Desktop sketch (copy is illustrative; implementation uses translation keys):

```text
+--------------------------------------------------------------+
| Tonight's dinner hero                                        |
+--------------------------------------------------------------+
| How was the lasagne?                                         |
| [Lasagne -> dish]  On your menu yesterday                     |
| Your rating  [five existing heart controls]       [Not now]   |
+--------------------------------------------------------------+
| Tomorrow preview                                             |
+--------------------------------------------------------------+
```

Mobile uses the same card, stacking context and the heart/action row as needed. Only dish-name text navigates; the outer card is not a link. Use existing `text-card-title`, body/caption roles, `--color-surface`, `--color-border`, spacing/radius tokens, and `--color-heart`; no new palette or typography. Give long dish names a safe truncation/wrapping strategy while retaining the full accessible name. Provide a localized personal-rating label, keyboard-accessible half-heart control, pending action feedback, and a quiet live announcement when the next dish replaces the card. Restore focus within the replacement card without taking focus away from unrelated user activity; respect reduced motion.

Loading reserves a compact card/skeleton area without delaying the hero. No candidates means no card, count, guilt message, or historical backlog view. Initial load failure offers a small localized retry, separate from the rest of Home. A failed rating/dismissal retains the card and selection for retry.

English and Danish keys cover heading, menu date/yesterday context, "Your rating", "Not now", pending/success/error/retry states, preference/hint text, accessibility announcements, unavailable destinations, and push copy. Format date labels with the active locale; service-worker push translations extend its existing English/Danish dictionary using the subscription language. Existing normal dish browsing and catalog return context remain untouched.

**Alternative:** an automatic modal interrupts the main mobile task of viewing tonight's plan. A banner that only links to a detail page adds friction to an action that fits inline.

### 7. Record the push attempt before transport and use an external scheduler

Proposed default schedule: 15:00 Europe/Copenhagen each day, configured in the external scheduler with DST support. This is an implementation default for the agreed following-afternoon timing. Add a separate secret-protected HTTP endpoint using the existing configured push secret/header convention; do not add a new Azure Functions TimerTrigger. Document configuration rather than silently changing the existing nightly delivery schedule.

For each subscribed family, load recent dinner/dish context once. For each distinct subscription user, load reminder state and current membership/access. Invoke the shared selection service, choose the first push-eligible occurrence, then revalidate and conditionally persist `TryRecordPushAttempt`. A revision conflict requires a reload and policy reevaluation. Recheck current plan, rating, dismissal, preference, and subscription immediately before dispatch; if now ineligible, skip the send. An already recorded attempt consumes the day's budget even when a subsequent check cancels it.

Pass a typed payload with `type: 'rating_reminder'`, `dishName`, `dinnerDate`, `lang`, and a recognized relative destination such as `/dishes/{dishId}?familyId={familyId}&ratingReminderDate={date}#my-rating`. Use the existing WebPush client behind an injected transport boundary, preserve invalid-subscription cleanup, and log attempted/succeeded/skipped/failed outcomes without logging endpoints, keys, or secrets. Set notification expiry/TTL so a delayed notification is not retained past the dinner's seven-day eligibility window.

The aggregate records attempts, not guaranteed delivery receipts. If a process dies after reservation or a network result is uncertain, do not retry the occurrence. This favors the agreed no-repeat behavior over at-least-once delivery: a push can be missed, but Home still provides the action. Durable conditional state handles concurrency, process restarts, and subscription replacement. Do not claim exactly-once browser display.

**Alternative:** writing history after the transport allows duplicate pushes on retries; long-lived timer triggers conflict with scale-to-zero. An outbox with retries would add infrastructure and still cannot guarantee exactly-once Web Push presentation without transport cooperation.

### 8. Route through the existing detail page with safe deferred intent

Store a validated relative destination in notification `data` when displaying it. The service worker accepts only same-origin recognized dish-rating destinations, rejects external/scheme-relative paths, navigates and focuses an existing same-origin app client, or opens one. Legacy destinationless notifications keep their current focus/root behavior. Ignore windows outside the app origin.

At the app boundary, parse notification family/dish/date intent with typed validation. Before an unauthenticated route is redirected, retain only the validated relative destination in session storage through the sign-in round trip. Centralize return-intent handling in a small routing composable or plugin integrated with auth middleware and landing redirect, so the existing unconditional `/home` redirect cannot win. Ordinary logins without a pending intent still go to Home. Clear consumed intent, and clear identity-bound stale state on sign-out or account changes.

After authentication, load accessible family selectors and validate the intended family. Resolve/switch the family before mounting/loading the dish detail feature; add a focused route middleware for this detail entry if required. Coordinate it with the existing family-change watcher so it does not redirect an intentional notification family selection back to `/dishes`. Never select a family on the strength of a URL alone.

Watch the relevant route params/query and readiness instead of relying only on mount. After the intended dish and member row render, use a typed template ref/`nextTick` to scroll and focus the current user's row and apply a short token-based highlight. Make the row focusable without changing rating selection. Normal page visits do not scroll. Recognized reminder links can be reused from Home without introducing another screen. Already resolved/expired reminders can still open the normal dish but must not create a pending reminder or auto-write anything. Missing/deleted/inaccessible targets use localized unavailable feedback and an accessible Home link.

**Alternative:** a new rating popup duplicates the established detail workflow. Merely focusing a window or passing a fragment without loading/family/auth coordination does not reliably reach the intended editable control.

## Risks / Trade-offs

- A rating or dismissal can finish after the final eligibility check while a push is already in flight. Mitigation: final revalidation narrows that race; clicks always revalidate access and never re-create pending state. Previously delivered OS notifications cannot be reliably recalled across devices.
- Reserving before sending can lose a push on transport failure or a crash. Mitigation: explicitly use at-most-once attempts, record outcomes, retain the Home action, and test failure/restart behavior.
- Per-user state is a concurrency hotspot for simultaneous reminder actions. Mitigation: small bounded state, point reads, ETags, bounded conflict retries, and localized retry feedback without silent overwrites.
- Some browsers cannot expose Web Push. Mitigation: reuse current support/permission/install guidance; Home rating works independently.
- Current routing has unconditional redirects and mount-only dish loading. Mitigation: focused return-intent and route-reuse tests, including different dish/family and signed-out entry.
- Copenhagen is currently the app's delivery timezone, not a configurable family timezone. Mitigation: use it consistently and document this scope; do not derive calendar eligibility from a device's different timezone.
- Existing push main specs are structurally invalid for archive: they start with delta headers and omit the main Purpose/Requirements structure. Mitigation: a dedicated baseline task normalizes those two main spec structures while preserving every existing requirement/scenario, then verifies full main-spec reads and archive compatibility. Current change validation passes but reports this prerequisite as informational; do not treat that as archive readiness.
- Visual consistency has been assessed from source, not a rendered new feature. Mitigation: require desktop/mobile browser comparisons during implementation; passing tests is not visual verification.

## Migration Plan

0. Before implementation/archive workflow completion, normalize only the structure of `openspec/specs/dinner-push-delivery/spec.md` and `openspec/specs/push-subscription-management/spec.md`, retaining all existing behavior; verify the pending deltas no longer produce archive-refusal diagnostics.
1. Add reminder aggregate persistence/DI and provision the `RatingReminders` container through the established migration path. Existing users require no backfill; absent state means push off and no dismissal.
2. Deploy authenticated queries/commands and the secret-protected send endpoint before enabling the frontend feature. Preserve current push endpoints/payloads.
3. Deploy the Home card, shared heart-control extension, preference control, both locales, and service-worker/auth routing support. Ensure normal detail/catalog navigation and notification behavior remain compatible.
4. Verify new service-worker activation, then configure the external scheduler at the proposed afternoon time. Verify the valid/invalid-secret paths and duplicate-run protection with controlled fixtures.
5. Run backend unit tests/build, frontend tests/lint/static build, and desktop/mobile visual/action checks. Cosmos emulator integration verification covers partitioning, conditional creation/replacement, conflicts, and persisted reload state.
6. Rollback: disable the new scheduler first, then revert the frontend feature or endpoint deployment. Leave persisted preference/dismissal/attempt documents intact for a safe later rollout; no dinner or rating schema is changed.
