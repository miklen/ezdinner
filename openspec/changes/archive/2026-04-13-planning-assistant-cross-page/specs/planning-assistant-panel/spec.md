## MODIFIED Requirements

### Requirement: Planning Assistant panel is available on the dishes page
The Planning Assistant panel SHALL be accessible from the dishes index page. On medium and larger screens it SHALL appear as a sidebar alongside the dish catalog. On small screens it SHALL be accessible via a FAB button that opens a bottom sheet — matching the existing pattern on the plan page.

#### Scenario: Panel displays as sidebar on dishes page desktop
- **WHEN** the dishes page is loaded on a medium or larger screen
- **THEN** the Planning Assistant panel SHALL be visible in a sidebar to the right of the dish catalog
- **AND** the dish catalog SHALL occupy the remaining width

#### Scenario: FAB and bottom sheet on dishes page mobile
- **WHEN** the dishes page is loaded on a small screen
- **THEN** a FAB button SHALL be visible in the bottom-right corner
- **AND** tapping the FAB SHALL open a bottom sheet containing the Planning Assistant panel

#### Scenario: Assigning a dish from the dishes page
- **WHEN** the planner assigns a dish to a day using the panel on the dishes page
- **THEN** the assignment SHALL be saved via the standard dinner mutation
- **AND** the panel SHALL refresh the dinners list for the selected week
- **AND** the dish catalog grid SHALL remain unchanged (it has no planned-status display)

### Requirement: Planning Assistant panel displays all active dishes sorted by freshness
The Planning Assistant panel SHALL display all active (non-archived) dishes for the family, sorted by last-used date descending (longest since last use at the top). Dishes that have never been used SHALL appear at the top of the list, visually distinguished. The panel SHALL be visible on medium and larger screens on the plan page.

#### Scenario: Panel shows dishes sorted by last-used
- **WHEN** the Planning Assistant panel is displayed
- **THEN** dishes SHALL be ordered with the longest-since-last-use first
- **AND** dishes with no usage history SHALL appear above all used dishes

#### Scenario: Never-used dishes are visually distinct
- **WHEN** a dish has no recorded usage history
- **THEN** it SHALL display "Never used" or equivalent instead of a days-ago figure
- **AND** it SHALL be visually differentiated from dishes with usage history

#### Scenario: Panel is not shown on mobile without FAB interaction
- **WHEN** the viewport is small (mobile breakpoint)
- **THEN** the Planning Assistant panel SHALL NOT be rendered inline
- **AND** the plan week list or dish catalog SHALL occupy the full width
- **AND** a FAB SHALL be available to open the panel in a bottom sheet
