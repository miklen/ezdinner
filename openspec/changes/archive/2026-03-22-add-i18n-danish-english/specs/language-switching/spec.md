## ADDED Requirements

### Requirement: Default language detection
The system SHALL auto-detect the user's preferred language from the browser/OS locale on first visit. If the detected locale starts with `da`, the UI language SHALL be set to Danish. All other locales SHALL default to English.

#### Scenario: Danish browser opens app for the first time
- **WHEN** a user with a browser locale of `da`, `da-DK`, or `da-*` opens the app with no saved preference
- **THEN** the UI is displayed in Danish

#### Scenario: Non-Danish browser opens app for the first time
- **WHEN** a user with any non-Danish browser locale (e.g. `en-US`, `de`, `fr`) opens the app with no saved preference
- **THEN** the UI is displayed in English

### Requirement: Language switcher UI
The system SHALL provide a language switcher accessible on both desktop and mobile layouts. The switcher SHALL show a compact globe icon button that opens a menu with "English" and "Dansk" options.

#### Scenario: Desktop user opens language menu
- **WHEN** a desktop user clicks the globe icon in the TopbarProfile user menu
- **THEN** a menu appears with two options: "English" and "Dansk"
- **THEN** the currently active language is visually indicated (e.g. checkmark or highlighted)

#### Scenario: Mobile user opens language menu
- **WHEN** a mobile user taps the globe icon in the TopbarSmall bar
- **THEN** a menu appears with two options: "English" and "Dansk"
- **THEN** the currently active language is visually indicated

### Requirement: Language switch updates UI immediately
The system SHALL update all visible UI strings immediately when the user selects a language, without a page reload.

#### Scenario: User switches from English to Danish
- **WHEN** a user currently viewing the app in English selects "Dansk"
- **THEN** all UI labels, button text, headings, and status messages switch to Danish immediately

#### Scenario: User switches from Danish to English
- **WHEN** a user currently viewing the app in Danish selects "English"
- **THEN** all UI labels, button text, headings, and status messages switch to English immediately

### Requirement: Language preference persistence
The system SHALL persist the user's language choice in `localStorage` so that returning to the app or reloading the page restores the same language.

#### Scenario: User reloads after switching language
- **WHEN** a user switches the language and reloads the page
- **THEN** the previously selected language is restored

#### Scenario: User returns in a new session
- **WHEN** a user closes the browser and reopens the app
- **THEN** the language they last selected is used

### Requirement: Complete Danish translation
The system SHALL provide Danish translations for all user-visible strings in the application, covering navigation, all pages (Home, Plan, Dishes, Families, Dish detail), dialogs, and error/status messages.

#### Scenario: Danish user navigates the app
- **WHEN** the active language is Danish
- **THEN** all navigation labels, page headings, form labels, button text, empty states, and inline messages are displayed in Danish

#### Scenario: No untranslated strings in Danish
- **WHEN** the active language is Danish
- **THEN** no English fallback strings are visible in the UI (no missing translation keys)

### Requirement: Date formatting respects active locale
The system SHALL format displayed dates using the active locale so that date strings (day names, month names) appear in the correct language.

#### Scenario: Date display in Danish
- **WHEN** the active language is Danish
- **THEN** dates on the Home page hero card display Danish day and month names (e.g. "mandag 17. marts")

#### Scenario: Date display in English
- **WHEN** the active language is English
- **THEN** dates on the Home page hero card display English day and month names (e.g. "Monday, March 17")
