## ADDED Requirements

### Requirement: AssistantPanel operates standalone with internal week navigation
When `weekStart` is not provided as a prop, the Planning Assistant panel SHALL render its own week navigation control and manage the selected week internally. The internal default SHALL follow the same rule as the plan page: if today is Saturday or Sunday, default to next week's Monday; otherwise default to the current week's Monday.

#### Scenario: Panel defaults to current week on a weekday
- **WHEN** the panel is rendered without a `weekStart` prop on a weekday (Monday–Friday)
- **THEN** the panel SHALL display the current week starting from Monday of the current week

#### Scenario: Panel defaults to next week on weekend
- **WHEN** the panel is rendered without a `weekStart` prop on Saturday or Sunday
- **THEN** the panel SHALL display the next week starting from the following Monday

#### Scenario: Panel renders week navigation controls when standalone
- **WHEN** `weekStart` is not provided
- **THEN** the panel SHALL render previous/next week navigation controls above the mode tabs
- **AND** the user can navigate to any adjacent week

#### Scenario: Panel hides week navigation when week is controlled externally
- **WHEN** `weekStart` is provided as a prop
- **THEN** the panel SHALL NOT render its own week navigation controls
- **AND** week navigation is the responsibility of the parent component

### Requirement: AssistantPanel on dishes page loads its required data
When embedded on the dishes page, the Planning Assistant panel SHALL trigger loading of dinners for the selected week and wishlist data. The dishes catalog data (already loaded by the dishes page) SHALL be reused — no duplicate fetch.

#### Scenario: Desktop sidebar triggers data load on mount
- **WHEN** the dishes page is loaded on a medium or larger screen
- **THEN** dinners for the default week and wishlist data SHALL be fetched once after mount

#### Scenario: Mobile panel triggers data load on first open
- **WHEN** the user taps the FAB on the dishes page for the first time
- **THEN** dinners for the default week and wishlist data SHALL be fetched before the panel is shown

#### Scenario: Navigating to a different week refreshes dinner data
- **WHEN** the user navigates to a different week using the panel's internal week controls
- **THEN** dinners for the newly selected week SHALL be fetched
- **AND** the dish list and AI draft (if any) SHALL reflect the new week context

### Requirement: useWeekNav composable encapsulates week navigation state
A `useWeekNav` composable SHALL encapsulate the week-start `ref` and the Sat/Sun defaulting logic, so the plan page and the standalone panel share identical behaviour without code duplication.

#### Scenario: Composable returns a reactive weekStart ref
- **WHEN** `useWeekNav()` is called
- **THEN** it SHALL return a `weekStart` ref initialized to the correct default week Monday

#### Scenario: Composable is used in plan.vue
- **WHEN** the plan page initializes
- **THEN** it SHALL use `useWeekNav()` for its `weekStart` ref instead of inlining the defaulting logic
