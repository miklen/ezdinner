## Context

EzDinner is a family dinner planning app used on desktop and as an iOS PWA. Families plan weekly dinners from a dish catalog. The active frontend is a Nuxt 3 app (TypeScript, Pinia, Vuetify 3) backed by .NET 10 Azure Functions v4 with CosmosDB.

Push notifications will use the W3C Web Push protocol with VAPID authentication. The backend sends signed HTTP requests to browser/OS push services. The frontend registers a Service Worker that listens for push events and displays system notifications.

iOS PWA push notifications require iOS 16.4+ and the app must be installed to the home screen. Desktop browsers (Chrome, Edge, Firefox, Safari 16.4+) work without installation.

## Goals / Non-Goals

**Goals:**
- Daily opt-in push notification at 16:00 showing what's for dinner tonight
- Per-member opt-in/out toggle; default is off
- Works on iOS PWA (home screen install) and desktop browsers
- Graceful degradation when notifications are unsupported or denied

**Non-Goals:**
- Real-time notifications for plan changes (scope creep)
- Configurable notification time per user (fixed 16:00 in this iteration)
- Multi-timezone per-family support (hardcoded Europe/Copenhagen; add timezone field to Family aggregate later)
- Rich notification media (images, actions buttons) beyond a tap-to-open link

## Decisions

### 1. VAPID key pair stored as Azure Function app settings

VAPID (Voluntary Application Server Identification) is required by the Web Push standard. A single VAPID key pair is generated once and stored as `WebPush:VapidPublicKey` and `WebPush:VapidPrivateKey` app settings. The public key is exposed via `GET /api/push/vapid-public-key` so the frontend can subscribe.

**Alternatives considered**: Per-family keys add unnecessary complexity with no security benefit.

### 2. WebPush.NET (`WebPush` NuGet package) for VAPID signing

`WebPush` (NuGet: `WebPush`) is the standard .NET library for VAPID signing and sending push messages. It handles elliptic curve key generation and the HTTP message signing spec.

**Alternatives considered**: Rolling our own VAPID signing is error-prone and not justified.

### 3. Manual Service Worker (not `@vite-pwa/nuxt`)

A minimal hand-written service worker (`public/sw.js`) registered via a Nuxt plugin is sufficient. `@vite-pwa/nuxt` adds full offline caching which is out of scope and complicates the setup (e.g., SWA routing, auth intercepts).

The SW needs to handle only two events: `push` (show notification) and `notificationclick` (open app).

### 4. PushSubscriptions CosmosDB container, partitioned by `familyId`

Each subscription document: `{ id, userId, familyId, endpoint, p256dh, auth, createdAt }`. Partitioning by `familyId` allows efficient batch reads during delivery (load all subscriptions for a family in one query).

**Alternatives considered**: Partitioning by `userId` is natural for writes but requires cross-partition fan-out during delivery. Partitioning by `familyId` optimizes the read-heavy delivery path.

### 5. HTTP-triggered delivery function called by Azure Logic App

The CLAUDE.md notes that `TimerTrigger` on Consumption plan keeps instances alive and prevents scale-to-zero. Instead, a Logic App (Recurrence trigger at 16:00 Europe/Copenhagen) calls `POST /api/push/send-tonight` with a shared secret header. This keeps the Function on Consumption (scale-to-zero), avoids always-on cost, and handles timezone correctly via the Logic App scheduler.

**Alternatives considered**: `[TimerTrigger("0 0 16 * * *")]` is simpler but runs continuously and burns CosmosDB reads at idle. Logic App adds ~$0 at low volume (first 4,000 runs/month are free).

### 6. Notification opt-in stored on the PushSubscription document

The presence of a valid `PushSubscription` document for a user implies consent. Unsubscribing deletes the document. This avoids adding a boolean flag to the `Family` aggregate or `UserProfile`.

### 7. Timezone fixed to Europe/Copenhagen for notification scheduling

`LocalDate` (NodaTime) has no timezone. The family's dinner calendar is date-only. The delivery function resolves today's `LocalDate` in `Europe/Copenhagen` before querying dinner plans. A future improvement: add `TimeZoneId` to the `Family` aggregate.

## Risks / Trade-offs

- **iOS 16.4+ requirement** → Users on older iOS will not see the opt-in toggle (feature-detect before rendering). Document minimum iOS version in app store description.
- **Notification permission denial** → Browser shows a system prompt; if user denies, we cannot retry. Show a friendly message guiding them to re-enable in settings.
- **Logic App delay/failure** → Notifications could arrive late or not at all if the Logic App run fails. Low severity (non-critical feature). Logic App run history provides visibility.
- **Push service TTL / expiry** → Push subscriptions can expire or be invalidated by the browser. The delivery function should catch `410 Gone` responses and delete the stale subscription document automatically.
- **No dinner planned** → If no dinner is recorded for tonight, skip the notification rather than sending an empty or confusing message.
- **Multiple family memberships** → A user belongs to one family in the current model. If multi-family is added later, subscriptions need to be per (user, family) pair — the schema already supports this.

## Migration Plan

1. Generate VAPID key pair (`web-push generate-vapid-keys`) and add to Function app settings (`WebPush:VapidPublicKey`, `WebPush:VapidPrivateKey`, `WebPush:Subject`).
2. Create `PushSubscriptions` CosmosDB container (partition key: `/familyId`).
3. Deploy backend with new endpoints and delivery function.
4. Deploy frontend with Service Worker and opt-in toggle.
5. Create Logic App with Recurrence trigger (16:00, Europe/Copenhagen timezone) → HTTP action → `POST /api/push/send-tonight`.

**Rollback**: Disable the Logic App. No data migration needed — subscription documents are additive.

## Open Questions

- Should notifications include the dish's effort/season details, or just the name?
- Is 16:00 always appropriate, or should it vary by day (e.g., weekend earlier)?
- Should we notify when no dinner is planned ("Nothing planned for tonight — check the app")?
