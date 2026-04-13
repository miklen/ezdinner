## ADDED Requirements

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

### Requirement: Planning Assistant panel shows combined planning signals per dish row
Each dish row in the panel SHALL display: dish name, effort level badge, days since last use, and wish vote indicator. This allows the planner to assess freshness, family demand, and effort fit in a single glance.

#### Scenario: Effort level displayed as badge
- **WHEN** a dish has an effort level set (Quick, Medium, or Elaborate)
- **THEN** a color-coded badge SHALL be shown inline with the dish name
- **AND** the badge SHALL use a consistent color per effort level across the app

#### Scenario: Effort level absent for unenriched dish
- **WHEN** a dish has no effort level set
- **THEN** no effort badge SHALL be shown (no placeholder or empty badge)

#### Scenario: Wish indicator shows vote count
- **WHEN** a dish has one or more active wish votes
- **THEN** a wish indicator SHALL be shown with the vote count
- **AND** the indicator SHALL be visually prominent relative to other metadata

#### Scenario: No wish indicator for unwished dish
- **WHEN** a dish has no active wish
- **THEN** no wish indicator SHALL be shown in its row

### Requirement: Planning Assistant panel supports search and effort filter
The panel SHALL include a search field that filters the dish list by name, and an effort filter that narrows to a specific effort level. Both filters combine (AND logic). Clearing all filters restores the full sorted list.

#### Scenario: Name search filters dish list
- **WHEN** the planner types in the search field
- **THEN** only dishes whose names contain the search string (case-insensitive) SHALL be displayed

#### Scenario: Effort filter narrows list
- **WHEN** the planner selects an effort level filter (e.g., Quick)
- **THEN** only dishes with that effort level SHALL be displayed
- **AND** dishes with no effort level set SHALL be excluded

#### Scenario: Search and effort filter combine
- **WHEN** both a name search and an effort filter are active
- **THEN** only dishes matching both criteria SHALL be displayed

#### Scenario: Clearing filters restores full list
- **WHEN** the planner clears the search field and removes the effort filter
- **THEN** all active dishes SHALL be shown in last-used order

### Requirement: Planner can assign a dish to a day directly from the panel
Each dish row in the Planning Assistant panel SHALL include an assign action. Activating it reveals the 7 days of the currently viewed week. The planner clicks a day to assign the dish to that dinner. The assignment uses the same mechanism as the existing dinner-card dish picker.

#### Scenario: Assign action reveals day picker
- **WHEN** the planner activates the assign action on a dish row
- **THEN** the 7 days of the current week SHALL be shown as selectable targets
- **AND** each day SHALL display its short weekday name and date

#### Scenario: Assigning a dish to an empty day
- **WHEN** the planner selects a day that has no dishes planned
- **THEN** the dish SHALL be added to that dinner's menu
- **AND** the day SHALL show a visual indicator that it now has a planned dish

#### Scenario: Assigning a dish to a day that already has dishes
- **WHEN** the planner selects a day that already has one or more dishes planned
- **THEN** the dish SHALL be added as an additional menu item for that day (supporting main + side patterns)

#### Scenario: Assigning same dish twice (leftovers)
- **WHEN** the planner selects a day for a dish that is already assigned to another day in the same week
- **THEN** the assignment SHALL proceed normally
- **AND** the dish SHALL appear on both days in the week plan

#### Scenario: Day picker closes after assignment
- **WHEN** the planner selects a day and the assignment completes
- **THEN** the day picker SHALL close
- **AND** the dish row SHALL reflect the updated assignment state

### Requirement: Planning Assistant panel has a Wishlist mode toggle
The panel SHALL provide a toggle between two modes: Plan (default) and Wishlist. In Plan mode the dish list is shown. In Wishlist mode the existing wishlist management UI is shown. The active wish count SHALL be displayed as a badge on the Wishlist toggle when there are active wishes.

#### Scenario: Switching to Wishlist mode
- **WHEN** the planner activates the Wishlist toggle
- **THEN** the dish list SHALL be replaced by the wishlist management UI
- **AND** the planner can add, upvote, and remove wishes as before

#### Scenario: Switching back to Plan mode
- **WHEN** the planner activates the Plan toggle
- **THEN** the wishlist management UI SHALL be replaced by the dish list

#### Scenario: Active wish count badge on Wishlist toggle
- **WHEN** the family has one or more active wishes
- **THEN** the Wishlist toggle SHALL display the count of active wishes as a badge
- **AND** the badge SHALL update when wishes are added or removed

#### Scenario: No badge when wish list is empty
- **WHEN** the family has no active wishes
- **THEN** no count badge SHALL be shown on the Wishlist toggle

### Requirement: Dish picker in dinner cards is sorted by last-used
The autocomplete dish picker inside expanded dinner cards SHALL sort its list by last-used descending (longest ago first). Wished dishes continue to float above all others. The sort order SHALL apply both when the search field is empty and when a search query is active.

#### Scenario: Empty search shows dishes sorted by last-used
- **WHEN** the dinner card is expanded and the search field is empty
- **THEN** dishes SHALL be listed in last-used descending order
- **AND** wished dishes SHALL appear at the top before all others

#### Scenario: Search results maintain last-used order
- **WHEN** the planner types in the dish picker search field
- **THEN** filtered results SHALL be sorted by last-used descending within each group (wished / non-wished)

### Requirement: Dish picker in dinner cards shows effort level
The `PlanDishRow` component SHALL display an effort level badge alongside the dish name, consistent with the Planning Assistant panel display.

#### Scenario: Effort badge shown in dinner card picker
- **WHEN** a dish with an effort level is shown in the dinner card autocomplete
- **THEN** the effort badge SHALL be visible in the row

#### Scenario: No effort badge for unenriched dish in picker
- **WHEN** a dish has no effort level set
- **THEN** no effort badge SHALL be shown in the row

### Requirement: AI week planner generates a full-week draft plan
The system SHALL provide an AI-powered week plan generation feature in the Planning Assistant panel. The planner provides optional freetext context about the week, and the system returns a draft assignment of dishes to unplanned days. The planner reviews the draft and accepts or skips individual days before committing.

#### Scenario: AI planner generates suggestions for unplanned days
- **WHEN** the planner triggers week plan generation
- **THEN** the system SHALL return one dish suggestion per unplanned day in the current week
- **AND** already-planned days SHALL be excluded from the draft

#### Scenario: AI planner respects freetext context
- **WHEN** the planner provides context such as "busy Monday, relaxed Friday"
- **THEN** the AI SHALL use this to inform effort matching (Quick for busy days, Elaborate for relaxed days)

#### Scenario: AI planner prefers fresh and wished dishes
- **WHEN** the AI generates suggestions
- **THEN** dishes not used in a long time SHALL be preferred over recently used ones
- **AND** dishes with active wishes SHALL be preferred over equal-freshness unwished dishes

#### Scenario: AI planner returns a reason per day
- **WHEN** the draft plan is returned
- **THEN** each suggested day SHALL include a brief plain-language reason for the dish choice

#### Scenario: Planner can accept individual day suggestions
- **WHEN** the draft plan is shown
- **THEN** the planner can accept each day's suggestion independently
- **AND** accepting a day assigns the suggested dish to that dinner via the standard assignment mechanism

#### Scenario: Planner can skip a day suggestion
- **WHEN** the draft plan is shown
- **THEN** the planner can skip a day's suggestion without assigning it
- **AND** skipping does not affect other days in the draft

#### Scenario: AI returns only dishes from the family catalog
- **WHEN** the AI generates suggestions
- **THEN** all suggested dish IDs SHALL correspond to active dishes in the family's catalog
- **AND** any dish ID not found in the catalog SHALL be silently dropped from the response

#### Scenario: AI planner backend endpoint requires family membership
- **WHEN** a request is made to the AI week plan endpoint
- **THEN** the caller SHALL be authenticated and a member of the specified family
- **AND** unauthenticated or unauthorized requests SHALL return 401/403

### Requirement: Suggestion bar is removed from the plan page
The per-day suggestion bar (`PlanSuggestionBar`) SHALL be removed from the plan page. Its function is superseded by the Planning Assistant panel.

#### Scenario: Suggestion bar no longer rendered
- **WHEN** the plan page is loaded
- **THEN** the suggestion bar SHALL NOT be present in the DOM
- **AND** no horizontal space SHALL be reserved for it above the week list
