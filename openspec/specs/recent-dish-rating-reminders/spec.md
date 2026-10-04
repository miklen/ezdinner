# recent-dish-rating-reminders Specification

## Purpose

Help family members rate recent planned dishes through a small immediate action on Home, without creating a historical backlog or repeatedly prompting after dismissal.

## Requirements

### Requirement: Eligibility uses personal ratings and recent past dinner dates
The system SHALL consider a dish eligible for the authenticated user only when it exists on a past planned dinner in the selected family, is neither deleted nor archived, and has no rating entry for that user. The eligible date range SHALL be inclusive from today minus seven days through yesterday, using Europe/Copenhagen calendar dates. Today, future dates, and older dates SHALL be excluded. A rating entry with value zero SHALL count as a rating. Confirmation that the meal was eaten SHALL NOT be required.

#### Scenario: Seven-day boundary
- **WHEN** today is 2026-10-12 in Europe/Copenhagen and an unrated active dish is planned for 2026-10-05
- **THEN** the dish is eligible
- **AND** occurrences on 2026-10-04, 2026-10-12, and later dates are excluded

#### Scenario: Another family member rated the dish
- **WHEN** another family member has rated a recent dish and the current user has not
- **THEN** the dish remains eligible for the current user

#### Scenario: Current user already rated the dish
- **WHEN** the current user has a rating entry for the dish, including a zero-valued entry
- **THEN** the dish is excluded from reminders regardless of other family members' ratings

#### Scenario: Empty or opted-out dinner
- **WHEN** a past date has no planned dishes or is opted out
- **THEN** that date produces no rating reminder

### Requirement: Home presents one deduplicated reminder at a time
Home SHALL display at most one reminder card for the most recent eligible dish. For each dish, the system SHALL select its latest occurrence within the eligible window before applying dismissal; earlier occurrences SHALL NOT appear as fallback reminders for that dish. Dishes SHALL be ordered by dinner date descending, with a stable dish-identity tie-break when multiple dishes share a date. Resolving the visible card SHALL reveal the next eligible dish, and no eligible dishes SHALL hide the card.

#### Scenario: Several recent unrated dishes
- **WHEN** three different dishes are eligible and Home loads
- **THEN** only the most recent dish is displayed
- **AND** successfully rating or dismissing it reveals the next eligible dish

#### Scenario: Repeated dish within the window
- **WHEN** the same dish appears on two past dinners within seven days
- **THEN** it appears once, associated with the later dinner date
- **AND** dismissing that card does not reveal its earlier occurrence

#### Scenario: Nothing remains
- **WHEN** the user has rated or dismissed all eligible dishes
- **THEN** the reminder card is hidden without an overdue-rating empty state

### Requirement: User can rate immediately on the Home card
The reminder card SHALL provide the established five-heart rating control with half increments, saving a selection immediately as the authenticated user's existing dish rating without a separate save step. It SHALL advance only after confirmed persistence and SHALL prevent overlapping rating and dismissal actions while saving. A failed save SHALL retain the reminder and provide a localized retryable error. Dish-name navigation SHALL remain separate from rating and dismissal actions.

#### Scenario: Successful immediate rating
- **WHEN** the user selects three and a half hearts on the Home card and persistence succeeds
- **THEN** their dish rating is saved as 3.5 on the existing UI scale
- **AND** the card advances to the next eligible dish
- **AND** the rated dish is excluded from subsequent Home and push reminders

#### Scenario: Rating save fails
- **WHEN** a selected rating cannot be saved
- **THEN** the same reminder remains available with a localized error and enabled retry after the request finishes
- **AND** the UI does not claim the rating was saved

#### Scenario: Distinct actions
- **WHEN** the user selects rating hearts or "Not now"
- **THEN** the app performs that action without navigating to the dish
- **WHEN** the user selects the dish name
- **THEN** the app opens the normal dish detail page

### Requirement: Not now persists dismissal until a later occurrence
The system SHALL persist "Not now" for the authenticated user and the selected family, dish, and dinner date. Dismissal SHALL suppress that occurrence and earlier occurrences of the same dish, across Home reloads and devices, without changing the plan or any rating. A later dinner occurrence SHALL become eligible once its date has passed, if the dish remains unrated and the date is within seven days. Dismissal SHALL advance the card only after persistence succeeds.

#### Scenario: Dismissal survives reload
- **WHEN** the user dismisses a dish planned yesterday and reloads Home or uses another device
- **THEN** that occurrence and earlier occurrences of that dish do not appear again

#### Scenario: Later planned dinner
- **WHEN** the dismissed dish is later planned for a new date and that date has passed
- **THEN** it becomes eligible again if the user has not rated it and the new occurrence remains within seven days

#### Scenario: Dismissal is personal
- **WHEN** one family member dismisses a dish reminder
- **THEN** another member's reminder eligibility and ratings are unchanged

#### Scenario: Dismissal fails
- **WHEN** the dismissal cannot be persisted
- **THEN** the current card remains available with a localized retryable error
- **AND** no successful dismissal is reported

### Requirement: Retroactive edits follow the dinner date
The system SHALL derive reminders from the current dinner plan and the dinner's date, rather than the time the menu was edited. A replacement dish SHALL have independent eligibility and dismissal from the removed dish. Menu changes SHALL NOT trigger immediate push delivery.

#### Scenario: Yesterday's dish is replaced
- **WHEN** yesterday's lasagne is replaced with unrated tacos and reminders are refreshed
- **THEN** lasagne is no longer a candidate from that dinner and tacos is eligible
- **AND** dismissing lasagne does not dismiss tacos
- **AND** tacos is considered at the next scheduled push run

#### Scenario: Old dinner is edited
- **WHEN** a dish is added to a dinner older than seven days
- **THEN** that occurrence produces neither a Home reminder nor a push reminder

#### Scenario: Same occurrence is removed and restored
- **WHEN** a dismissed dish is removed and later restored on the same dinner date
- **THEN** its dismissal remains effective for that occurrence

### Requirement: Reminders refresh without leaking stale family or date state
The system SHALL refresh reminder eligibility on Home entry, after a successful reminder action, when the active family or authenticated account changes, and when the page becomes active again. An open Home view SHALL retire dates outside the seven-day window after calendar rollover. A response for a previous family or account SHALL NOT replace the current user's card.

#### Scenario: Family switches during loading
- **WHEN** a reminder request for family A finishes after the user switches to family B
- **THEN** the family A result is discarded and only family B reminders can appear

#### Scenario: Window expires while Home is open
- **WHEN** a displayed occurrence becomes older than seven days at Copenhagen midnight
- **THEN** it is removed from eligibility and the card advances or disappears

### Requirement: Reminder presentation follows existing responsive and localized UI
The Home card SHALL use the existing dish presentation, heart-rating appearance, typography, spacing, colors, and navigation conventions. Its wording SHALL refer to the dish being on the menu rather than claim the meal was eaten. All labels, dates, accessibility text, success/error messages, and notification text SHALL support English and Danish. Rating and dismissal SHALL work with keyboard and touch on desktop and mobile without horizontal overflow. Loading and errors SHALL NOT block the rest of Home.

#### Scenario: Mobile and desktop use the same quick action
- **WHEN** the card is viewed at mobile or desktop widths
- **THEN** the dish title, menu date, rating hearts, and "Not now" remain usable
- **AND** selecting a heart saves directly in both layouts

#### Scenario: Locale changes
- **WHEN** the user switches between Danish and English
- **THEN** reminder wording, date formatting, and accessible control labels update to the selected language

### Requirement: Reminder access is authenticated and family scoped
The system SHALL authenticate reminder reads and dismissal writes, verify access to the requested family and dishes, and derive the reminder owner from the authenticated identity. It SHALL reject attempts to read or dismiss another user's reminders or use a dish from another family.

#### Scenario: Unauthorized reminder access
- **WHEN** an unauthenticated user or a user without access to the requested family requests reminders or dismissal
- **THEN** access is denied and no reminder state is read or changed for that family

#### Scenario: Another user's identity is supplied
- **WHEN** a client supplies another user's identity in a reminder request
- **THEN** it cannot act on that user's reminders
