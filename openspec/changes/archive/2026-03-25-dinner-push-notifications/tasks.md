## 1. Backend Infrastructure

- [x] 1.1 Add `WebPush` NuGet package to `EzDinner.Functions` and `EzDinner.Application` projects
- [x] 1.2 Create `PushSubscriptions` CosmosDB container (partition key: `/familyId`) in local Azurite and document in infrastructure setup notes
- [x] 1.3 Add `WebPush:VapidPublicKey`, `WebPush:VapidPrivateKey`, and `WebPush:Subject` to `local.settings.json` (generate keys with `web-push generate-vapid-keys`)
- [x] 1.4 Add `WebPush:SendTonightSecret` shared secret to `local.settings.json` for delivery endpoint auth

## 2. Backend Domain & Infrastructure Layer

- [x] 2.1 Create `PushSubscription` entity in `EzDinner.Core/Aggregates/PushSubscriptionAggregate/` with fields: `Id`, `UserId`, `FamilyId`, `Endpoint`, `P256dh`, `Auth`, `CreatedAt`
- [x] 2.2 Create `IPushSubscriptionRepository` interface in `EzDinner.Core` with `Save`, `Delete`, `GetByFamilyId`, `GetByUserId` methods
- [x] 2.3 Implement `PushSubscriptionRepository` in `EzDinner.Infrastructure` using CosmosDB, mapped to the `PushSubscriptions` container
- [x] 2.4 Register `IPushSubscriptionRepository` and `WebPushClient` in DI in `EzDinner.Functions`

## 3. Backend Application Layer (Commands & Queries)

- [x] 3.1 Create `SavePushSubscriptionCommand` in `EzDinner.Application` (inputs: userId, familyId, endpoint, p256dh, auth)
- [x] 3.2 Create `DeletePushSubscriptionCommand` in `EzDinner.Application` (inputs: userId)
- [x] 3.3 Create `GetPushSubscriptionQuery` in `EzDinner.Query.Core` to check if a user has an active subscription
- [x] 3.4 Create `SendTonightNotificationsCommand` in `EzDinner.Application` that: resolves today's `LocalDate` in Europe/Copenhagen, loads all families with subscriptions, looks up tonight's dinner per family, sends push notifications, deletes stale subscriptions on 410/400 responses

## 4. Backend HTTP Functions

- [x] 4.1 Create `GET /api/push/vapid-public-key` function (anonymous auth) returning the VAPID public key string
- [x] 4.2 Create `POST /api/push/subscriptions` function (authenticated) that maps request body to `SavePushSubscriptionCommand`
- [x] 4.3 Create `DELETE /api/push/subscriptions` function (authenticated) that calls `DeletePushSubscriptionCommand` for the current user
- [x] 4.4 Create `GET /api/push/subscriptions/me` function (authenticated) that returns whether the current user has an active subscription
- [x] 4.5 Create `POST /api/push/send-tonight` function (validates `X-Push-Secret` header, returns 401 if invalid) that calls `SendTonightNotificationsCommand`

## 5. Frontend Service Worker

- [x] 5.1 Create `public/sw.js` with `push` event handler that reads the notification payload (title + body) and calls `self.registration.showNotification()`
- [x] 5.2 Add `notificationclick` handler to `sw.js` that opens/focuses the app URL and closes the notification
- [x] 5.3 Create `plugins/service-worker.client.ts` Nuxt plugin that registers `sw.js` on startup (only if `'serviceWorker' in navigator`)

## 6. Frontend Notification Composable

- [x] 6.1 Create `composables/usePushNotifications.ts` that exposes: `isSupported`, `isSubscribed` (ref), `subscribe()`, `unsubscribe()`
- [x] 6.2 In `subscribe()`: fetch VAPID public key, call `registration.pushManager.subscribe()`, POST subscription to backend; handle permission denial gracefully
- [x] 6.3 In `unsubscribe()`: call `pushManager.unsubscribe()`, DELETE from backend
- [x] 6.4 On composable init, call `GET /api/push/subscriptions/me` to hydrate `isSubscribed` state

## 7. Frontend Settings UI

- [x] 7.1 Add a "Notifications" section to the user profile/settings page with a toggle switch bound to `usePushNotifications`
- [x] 7.2 Hide the toggle entirely when `isSupported` is false
- [x] 7.3 Show a message when on iOS Safari (not installed PWA) explaining home screen install is required
- [x] 7.4 Show a snackbar on successful subscribe/unsubscribe
- [x] 7.5 Show a snackbar with browser-settings guidance when permission is denied
- [x] 7.6 Add i18n keys for all notification UI strings to both `en.json` and `da.json`

## 8. Azure Logic App & Production Config

- [x] 8.1 Create Azure Logic App with Recurrence trigger (frequency: Day, interval: 1, at: 16:00, timezone: Europe/Copenhagen)
- [x] 8.2 Add HTTP action in Logic App: `POST https://<func-app>.azurewebsites.net/api/push/send-tonight` with `X-Push-Secret` header
- [x] 8.3 Add VAPID keys and send-tonight secret to `func-ezdinner-prod-02` app settings via Azure Portal
- [x] 8.4 Create `PushSubscriptions` container in production CosmosDB (partition key: `/familyId`)
