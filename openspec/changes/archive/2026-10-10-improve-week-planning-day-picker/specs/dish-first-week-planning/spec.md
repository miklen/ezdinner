# Spec Delta

## ADDED Requirements

### Requirement: Weekday and weekend initial week
On a fresh entry to Plan your week, the selected week SHALL start on the current calendar week's Monday when today is Monday-Friday and on the following week's Monday when today is Saturday-Sunday. Today SHALL use the user's local calendar date and weeks SHALL start on Monday. Opening or reopening an assignment picker and returning to a cached workspace SHALL retain the explicitly selected week rather than resetting it.

#### Scenario: Open on a weekday
- **WHEN** a member freshly opens Plan your week on Friday 2026-10-09
- **THEN** the selected Monday is 2026-10-05
- **AND** the overview and assignment picker cover 2026-10-03 through 2026-10-11 inclusive

#### Scenario: Open on Saturday
- **WHEN** a member freshly opens Plan your week on Saturday 2026-10-10
- **THEN** the selected Monday is 2026-10-12
- **AND** both planning surfaces cover 2026-10-10 through 2026-10-18 inclusive

#### Scenario: Open on Sunday
- **WHEN** a member freshly opens Plan your week on Sunday 2026-10-11
- **THEN** the selected Monday is 2026-10-12
- **AND** the preceding Saturday remains visible in the nine-day window

#### Scenario: Weekend spans a year boundary
- **WHEN** a member freshly opens Plan your week on Saturday 2027-01-02
- **THEN** the selected Monday is 2027-01-04
- **AND** the window covers 2027-01-02 through 2027-01-10 inclusive

#### Scenario: Reopen after manual navigation
- **WHEN** a member navigates to a different week, closes the picker, and opens it again or returns from dish details to the cached workspace
- **THEN** the selected week is retained
- **AND** no weekday/weekend default overrides that selection

## MODIFIED Requirements

### Requirement: One nine-day planning window
The overview and assignment picker SHALL cover exactly nine consecutive calendar dates: the Saturday and Sunday before the selected Monday, Monday-Friday, and the following Saturday-Sunday. Previous/next week controls SHALL be available in both surfaces, move the selected Monday seven days, and update both surfaces together. Past dates within a selected window SHALL remain visible. Navigation inside the picker SHALL keep it open with the same dish. Week labels, date ranges, and the return-to-current-week interaction SHALL match the established planning navigation; returning to current week SHALL select the actual current Monday even during the weekend.

#### Scenario: Both weekends are visible
- **WHEN** the selected Monday is 2026-10-12
- **THEN** both surfaces cover 2026-10-10 through 2026-10-18 inclusive

#### Scenario: Navigate to the next week
- **WHEN** the member activates next week
- **THEN** the overview and assignment targets both cover the next nine-day window
- **AND** dish filters, browsing position, and conversation are preserved
- **AND** an optional selected day is cleared if it falls outside that window

#### Scenario: Navigate from an open assignment picker
- **WHEN** the picker is open for a dish in the week starting 2026-10-12 and the member activates next week
- **THEN** the picker remains open for that dish
- **AND** its week label and targets and the workspace overview change to the Monday 2026-10-19 window
- **AND** assignment uses the chosen date in that window

#### Scenario: Navigate to previous weeks
- **WHEN** the member activates previous week from the picker with selected Monday 2026-10-12
- **THEN** both planning surfaces cover 2026-10-03 through 2026-10-11 inclusive
- **AND** past dates remain available
- **AND** the member can move forward again without closing the picker

#### Scenario: Return to actual current week on a weekend
- **WHEN** today is Saturday 2026-10-10, the selected Monday is 2026-10-12, and the member activates return to current week
- **THEN** both planning surfaces select Monday 2026-10-05
- **AND** the picker remains open for the same dish

#### Scenario: Navigate while dinner data is loading
- **WHEN** the member changes week and that window's dinner data has not loaded
- **THEN** the picker displays loading feedback and prevents assignment
- **AND** saved decisions from the old week are not presented as decisions for the new dates
- **AND** a late response for a superseded window cannot overwrite the selected window

#### Scenario: New week fails to load
- **WHEN** loading the selected window fails
- **THEN** the picker shows localized retry feedback and stays open
- **AND** assignment remains unavailable until the selected window loads successfully

### Requirement: Responsive accessible continuity
Desktop SHALL keep the week overview visible while dishes scroll. Mobile SHALL open on dish exploration with an accessible persistent week control showing the selected week; its full nine-day overview SHALL be reachable without route navigation. Previous/next week controls SHALL be available in the mobile overview and assignment picker. The assignment picker SHALL match the established dish planning presentation: a centered dialog at desktop widths and a bottom sheet at mobile widths, with consistent header, close action, week controls, grouped day rows, weekend tinting, existing-dish presentation, spacing, typography, and loading states. Its mobile sheet SHALL have a handle, safe-area padding, and a scrollable day list that keeps week controls reachable. Controls SHALL support keyboard operation, visible focus, meaningful labels, and touch use. All new UI and recommendation explanations SHALL follow the selected English or Danish language.

#### Scenario: Mobile assignment loop
- **WHEN** a member on mobile views the week, returns to dishes, inspects a recipe, and assigns it
- **THEN** the original exploration position is retained and the week remains accessible above the bottom navigation

#### Scenario: Keyboard operation
- **WHEN** a member uses only the keyboard to inspect and assign a dish
- **THEN** all actions are reachable, dialogs manage focus, and closing returns focus to the initiating action

#### Scenario: Desktop planning picker consistency
- **WHEN** a member opens assignment from Plan your week on a desktop screen
- **THEN** the dialog follows the existing dish planning visual presentation
- **AND** its week navigation, empty days, saved dishes, opt-outs, and loading states use established planning styles
- **AND** long dish names remain readable without displacing navigation or close controls

#### Scenario: Mobile week navigation in the picker
- **WHEN** a member opens assignment on a mobile screen and moves backward and forward between weeks
- **THEN** the bottom sheet stays open for the same dish
- **AND** its week controls remain usable while the day list scrolls
- **AND** all nine dates are reachable without horizontal overflow or obscured actions

#### Scenario: Language switches while picker is open
- **WHEN** a member switches between English and Danish with the picker open
- **THEN** its title, week labels, date labels, close and navigation labels, and loading or error feedback update to the selected language
- **AND** the selected dish and week are retained
