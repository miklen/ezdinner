# Spec Delta

## MODIFIED Requirements

### Requirement: Notification tap opens the app
The system SHALL configure push notifications so that tapping the notification on any platform opens or focuses the EzDinner app. When a notification supplies a recognized same-origin in-app destination, the system SHALL navigate the focused or newly opened app window to that destination. Notifications without a destination SHALL preserve the existing behavior of focusing an existing app window or opening the app's root. Unrecognized, malformed, or external destinations SHALL NOT navigate outside the app. Tapping SHALL dismiss the notification.

#### Scenario: User taps the notification
- **WHEN** a push notification is received and displayed by the Service Worker
- **THEN** tapping the notification opens or focuses the EzDinner web app
- **AND** the notification is dismissed

#### Scenario: Notification has a valid app destination
- **WHEN** the notification supplies a recognized same-origin in-app destination
- **THEN** an existing app window is navigated to that destination and focused, or a new app window is opened at it

#### Scenario: Existing notification has no destination
- **WHEN** an existing dinner or wish notification without a destination is tapped
- **THEN** an existing app window is focused or a new app window is opened at the app root

#### Scenario: Destination is invalid or external
- **WHEN** a notification contains an external or unrecognized destination
- **THEN** it opens or focuses the app using the existing root behavior
- **AND** it does not navigate to the supplied unsafe destination
