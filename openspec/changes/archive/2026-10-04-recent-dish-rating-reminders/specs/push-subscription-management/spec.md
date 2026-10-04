# Spec Delta

## ADDED Requirements

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
