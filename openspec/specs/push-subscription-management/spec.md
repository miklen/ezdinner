# Push subscription management

## Purpose

Allow family members to manage browser push subscriptions and their notification opt-in state.

## Requirements

### Requirement: User can subscribe to dinner push notifications
The system SHALL allow an authenticated family member to subscribe to daily dinner push notifications. Subscribing stores the user's Web Push subscription (endpoint + keys) in the backend associated with their userId and familyId.

#### Scenario: User subscribes successfully
- **WHEN** the user enables the notification toggle and grants browser notification permission
- **THEN** the frontend registers a push subscription using the VAPID public key and sends it to `POST /api/push/subscriptions`
- **THEN** the backend saves the subscription document to the `PushSubscriptions` container
- **THEN** the toggle shows as active and a confirmation snackbar is shown

#### Scenario: Notification permission denied by browser
- **WHEN** the user enables the notification toggle but denies the browser permission prompt
- **THEN** the toggle reverts to off
- **THEN** a snackbar message instructs the user to enable notifications in browser settings

#### Scenario: Push notifications not supported
- **WHEN** the user's browser does not support the Web Push API or Service Workers
- **THEN** the notification opt-in toggle SHALL NOT be rendered
- **THEN** no permission prompt is shown

#### Scenario: User subscribes on iOS PWA not installed to home screen
- **WHEN** the user attempts to subscribe in a Safari browser tab (not installed PWA)
- **THEN** the system SHALL display a message explaining that push notifications require installing the app to the home screen

### Requirement: User can unsubscribe from dinner push notifications
The system SHALL allow an authenticated family member to unsubscribe from daily dinner push notifications. Unsubscribing removes the stored subscription from the backend and unregisters the push subscription in the browser.

#### Scenario: User unsubscribes successfully
- **WHEN** the user disables the notification toggle
- **THEN** the frontend calls `DELETE /api/push/subscriptions`
- **THEN** the backend removes the subscription document from `PushSubscriptions`
- **THEN** the browser push subscription is unregistered
- **THEN** the toggle shows as inactive

#### Scenario: Subscription not found on unsubscribe
- **WHEN** the user disables the toggle but no subscription exists in the backend (e.g., already removed)
- **THEN** the backend returns 204 No Content
- **THEN** the frontend treats this as a successful unsubscribe

### Requirement: VAPID public key is available to the frontend
The system SHALL expose the VAPID public key via an unauthenticated endpoint so the frontend can create a push subscription without requiring a signed-in state at that point.

#### Scenario: Frontend fetches VAPID public key
- **WHEN** the frontend initialises the notification subscription flow
- **THEN** `GET /api/push/vapid-public-key` returns the VAPID public key as a plain string with 200 OK

### Requirement: Notification opt-in state is reflected on UI load
The system SHALL indicate on page load whether the current user has an active push subscription, so the toggle reflects the correct state.

#### Scenario: User has an active subscription
- **WHEN** the settings/profile page loads and the user has a stored subscription in the backend
- **THEN** the notification toggle is shown in the ON state

#### Scenario: User has no active subscription
- **WHEN** the settings/profile page loads and the user has no stored subscription
- **THEN** the notification toggle is shown in the OFF state

### Requirement: Rating reminder preference is independent and opt in
The system SHALL offer an independently persisted "Rating reminders" preference for the authenticated user alongside existing notification controls. It SHALL default to off for users without a recorded preference, including existing push subscribers. Enabling or disabling it SHALL NOT alter dinner or wish notification preferences, ratings, plans, or the availability of the Home reminder card. A push subscription alone SHALL NOT imply consent to rating reminders.

#### Scenario: Existing subscriber opens notification settings
- **WHEN** a user with an existing push subscription but no recorded rating-reminder preference opens settings
- **THEN** existing notifications remain enabled and rating reminders are off

#### Scenario: User enables rating reminders
- **WHEN** a subscribed user enables rating reminders and persistence succeeds
- **THEN** the preference is on across reloads and devices
- **AND** subsequent scheduled runs can deliver eligible reminders

#### Scenario: User disables rating reminders
- **WHEN** the user disables rating reminders
- **THEN** subsequent delivery checks suppress rating pushes
- **AND** existing notification categories and the Home card remain available

### Requirement: Rating reminder preference respects push availability
The system SHALL allow effective rating push delivery only while both the persisted rating preference and a usable browser push subscription are enabled. The rating toggle SHALL reuse existing notification support, browser permission, and iOS installation guidance. It SHALL explain when the existing notification opt-in is required, rather than silently subscribing or requesting browser permission on Home entry. Unsubscribing from browser push SHALL suppress delivery without deleting personal dismissals or changing the Home card.

#### Scenario: Browser subscription is disabled
- **WHEN** a user without an active browser push subscription opens the rating-reminder setting
- **THEN** the UI explains how to enable notifications and the rating push control is inactive
- **AND** no permission prompt is triggered automatically

#### Scenario: Push subscription is replaced
- **WHEN** the same user registers a replacement subscription or changes its family association
- **THEN** the saved rating preference, dismissals, and daily delivery limits are preserved

#### Scenario: Browser notifications are unsubscribed
- **WHEN** the user disables the existing notification subscription
- **THEN** rating push delivery stops
- **AND** personal dismissal history and Home rating reminders remain intact

### Requirement: Preference updates are personal and failures remain visible
The system SHALL authenticate preference reads and writes and derive their owner from the authenticated identity. A failed update SHALL leave the toggle reflecting the last confirmed persisted state and provide a localized error. Account changes SHALL clear stale preference/subscription UI state before loading the next account.

#### Scenario: Preference save fails
- **WHEN** enabling or disabling rating reminders fails
- **THEN** the toggle returns to its last confirmed value and a localized error is displayed

#### Scenario: Another account signs in
- **WHEN** the signed-in account changes
- **THEN** the previous account's rating preference is not displayed or changed for the new account
