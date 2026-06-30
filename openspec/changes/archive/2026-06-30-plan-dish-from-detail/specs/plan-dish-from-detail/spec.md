## ADDED Requirements

### Requirement: Plan dish action on dish detail page
The dish detail page SHALL display a "Plan dish" action button in the existing action row (alongside Archive and Wish List). The button SHALL only be shown when the dish is not archived.

#### Scenario: Button visible for active dish
- **WHEN** user views a non-archived dish detail page
- **THEN** a "Plan dish" button is shown in the action row

#### Scenario: Button hidden for archived dish
- **WHEN** user views an archived dish detail page
- **THEN** the "Plan dish" button is NOT shown

### Requirement: Week picker opens on plan action
When the user activates the "Plan dish" button, the system SHALL open a week picker showing the planning window for the coming week. The picker SHALL load dinner data lazily (on open, not on page load).

#### Scenario: Week picker opens
- **WHEN** user clicks the "Plan dish" button
- **THEN** a dialog (desktop) or bottom sheet (mobile) opens showing the planning window
- **THEN** dinner data for the window is fetched

#### Scenario: Loading state while fetching dinners
- **WHEN** the week picker is open and dinner data is loading
- **THEN** a loading indicator is shown in place of the day list

### Requirement: Planning window covers coming weekend and next full week
The planning window SHALL show the upcoming Saturday and Sunday plus the full Monday–Sunday of the following week — a 9-day window. When today is already Saturday or Sunday, today and tomorrow are included as the "coming weekend". Days in the past SHALL NOT be shown.

#### Scenario: Planning on a Saturday
- **WHEN** today is Saturday
- **THEN** the window shows today (Sat) through the Sunday 8 days later

#### Scenario: Planning on a weekday
- **WHEN** today is a weekday (Mon–Fri)
- **THEN** the window shows the coming Saturday and Sunday, then Monday through Sunday of the following week

#### Scenario: No past days shown
- **WHEN** the planning window is computed
- **THEN** no dates before today are shown

### Requirement: Existing dishes shown per day as context
Each day in the picker SHALL display the names of dishes already planned for that day. This is informational only — it does not prevent adding more dishes.

#### Scenario: Day with existing dishes
- **WHEN** a day already has one or more dishes planned
- **THEN** the dish names are shown on that day's row

#### Scenario: Day with no dishes
- **WHEN** a day has no dishes planned
- **THEN** the day row shows a visual indicator that it is free

### Requirement: User can plan the dish on any day in the window
The user SHALL be able to add the current dish to any day in the planning window by selecting that day. Multiple dishes per day are explicitly allowed — the system MUST NOT block adding a dish to a day that already has dishes planned.

#### Scenario: Add dish to a free day
- **WHEN** user selects a day with no dishes
- **THEN** the dish is added to that day's dinner menu

#### Scenario: Add dish to a day that already has dishes
- **WHEN** user selects a day that already has one or more dishes planned
- **THEN** the current dish is added alongside the existing dishes

#### Scenario: Add dish to multiple days
- **WHEN** user selects multiple days in sequence
- **THEN** the dish is added to each selected day independently

### Requirement: Confirmation feedback on successful planning
After a dish is added to a day, the system SHALL show a snackbar confirmation message. The picker SHALL remain open so the user can plan the dish on additional days or close it manually.

#### Scenario: Success snackbar
- **WHEN** dish is successfully added to a day
- **THEN** a success snackbar is shown with the dish name and the planned date
- **THEN** the picker stays open

#### Scenario: Picker stays open after assignment
- **WHEN** user plans the dish on a day
- **THEN** the week picker remains visible

### Requirement: Responsive presentation — dialog on desktop, sheet on mobile
The week picker SHALL render as a dialog on desktop screens (md and above) and as a bottom sheet on mobile screens (sm and below), consistent with the app's native-app-feel conventions on mobile.

#### Scenario: Desktop layout
- **WHEN** screen width is md or above
- **THEN** week picker renders as a centered dialog

#### Scenario: Mobile layout
- **WHEN** screen width is sm or below
- **THEN** week picker renders as a bottom sheet with handle and safe-area padding
