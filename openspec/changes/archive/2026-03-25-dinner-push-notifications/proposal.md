## Why

Family members using EzDinner as an iOS PWA or desktop app have no way to know what's for dinner tonight without opening the app. A daily push notification at 16:00 removes this friction, keeping the plan visible without requiring any active engagement.

## What Changes

- Add a Service Worker to the Nuxt 3 frontend that handles incoming push events and displays notifications
- Add a per-member opt-in toggle in the app (e.g., profile/settings) to subscribe or unsubscribe from dinner notifications
- Store Web Push subscriptions (PushSubscription objects) in the backend per user
- Add a backend timer that runs at 16:00 daily, looks up tonight's dinner for each subscribed family member's family, and sends a push notification via the Web Push protocol (VAPID)
- Notification content: dish name for tonight, with a tap/click action that opens the app

## Capabilities

### New Capabilities

- `push-subscription-management`: User can subscribe to or unsubscribe from dinner push notifications; their PushSubscription endpoint is stored in the backend
- `dinner-push-delivery`: Backend timer fires at 16:00, resolves tonight's dinner per family, and delivers a Web Push notification to all subscribed members of that family

### Modified Capabilities

- none

## Impact

- **Frontend**: New Service Worker (`sw.js`) registered via Nuxt plugin; notification permission request flow; opt-in toggle UI in user profile/settings; VAPID public key config
- **Backend API**: New endpoint `POST /api/push-subscriptions` (save subscription), `DELETE /api/push-subscriptions` (remove); new timer-triggered function for daily delivery; new CosmosDB container `PushSubscriptions`; VAPID key pair configuration
- **Dependencies**: `web-push` NuGet package (or equivalent) on backend for VAPID signing; `@vite-pwa/nuxt` or manual service worker for frontend
- **Platform constraints**: iOS requires PWA installed to home screen (iOS 16.4+) for Web Push; Android Chrome and desktop browsers work without installation
