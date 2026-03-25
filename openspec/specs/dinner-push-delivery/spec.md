## ADDED Requirements

### Requirement: Daily dinner notification is sent at 16:00
The system SHALL send a push notification to all subscribed family members at 16:00 Europe/Copenhagen time each day. The notification SHALL include the name of the dish planned for that evening.

#### Scenario: Dinner is planned for tonight
- **WHEN** the delivery function is triggered at 16:00 Europe/Copenhagen
- **THEN** for each family that has at least one subscribed member, the system resolves tonight's dinner using the family's dinner plan
- **THEN** a push notification is sent to each subscribed member of that family
- **THEN** the notification title is the app name and the body contains the dish name for tonight

#### Scenario: No dinner is planned for tonight
- **WHEN** the delivery function runs and a family has no dinner recorded for today's date
- **THEN** no notification is sent to the members of that family

#### Scenario: Family has no subscribed members
- **WHEN** the delivery function runs and no members of a family have an active push subscription
- **THEN** the delivery function skips that family silently

### Requirement: Stale push subscriptions are cleaned up on delivery failure
The system SHALL detect expired or invalidated push subscriptions during delivery and remove them from storage to prevent repeated delivery failures.

#### Scenario: Push service returns 410 Gone
- **WHEN** the backend receives HTTP 410 from the push service for a subscription endpoint
- **THEN** the subscription document is deleted from `PushSubscriptions`
- **THEN** delivery continues for other subscriptions

#### Scenario: Push service returns 400 or other client error
- **WHEN** the backend receives HTTP 400 from the push service
- **THEN** the subscription document is deleted from `PushSubscriptions`
- **THEN** delivery continues for other subscriptions

### Requirement: Notification tap opens the app
The system SHALL configure push notifications so that tapping the notification on any platform opens or focuses the EzDinner app.

#### Scenario: User taps the notification
- **WHEN** a push notification is received and displayed by the Service Worker
- **THEN** tapping the notification opens (or focuses) the EzDinner web app
- **THEN** the notification is dismissed

### Requirement: Delivery is triggered by an external scheduler
The system SHALL expose an HTTP endpoint `POST /api/push/send-tonight` protected by a shared secret header that triggers the delivery function. This endpoint is called by an external scheduler (Azure Logic App) at 16:00 Europe/Copenhagen.

#### Scenario: Delivery endpoint called with valid secret
- **WHEN** `POST /api/push/send-tonight` is called with the correct `X-Push-Secret` header value
- **THEN** the delivery function runs and returns 200 OK after processing all families

#### Scenario: Delivery endpoint called with invalid or missing secret
- **WHEN** `POST /api/push/send-tonight` is called without or with an incorrect `X-Push-Secret` header
- **THEN** the endpoint returns 401 Unauthorized without executing delivery
