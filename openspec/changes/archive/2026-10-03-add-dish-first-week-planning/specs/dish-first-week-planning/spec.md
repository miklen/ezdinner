# Spec Delta

## Purpose

Let families plan through continuous dish exploration while keeping their saved dinner choices visible in a supporting, navigable nine-day overview.

## ADDED Requirements

### Requirement: Independent dish-first planning entry
The system SHALL provide a separate Plan your week page and navigation entry on desktop and mobile. Dishes SHALL be the primary exploration surface, with no required day selection. Existing Plan and Dishes workflows SHALL remain available with their existing behavior.

#### Scenario: Enter the new workspace
- **WHEN** a family member opens Plan your week
- **THEN** dish exploration is immediately available alongside access to the saved week overview
- **AND** no day must be selected before browsing or requesting recommendations

#### Scenario: Existing entry points remain usable
- **WHEN** a family member opens the existing Plan or Dishes page
- **THEN** its existing browsing, assistant, and assignment interactions remain available

### Requirement: Browse dishes with planning signals
The workspace SHALL support title search, metadata filters, sorting by name, rating, usage count, and last used, and access to active family wishes. Dish presentations SHALL show available ratings, last-served information, wish indicators, and planned dates. Main-course browsing SHALL exclude known side-only dishes while including dishes with unknown roles visibly identified as unclassified. Users SHALL be able to change the role filter.

#### Scenario: Main-course browsing
- **WHEN** the default main-course filter is active
- **THEN** Main-role and unclassified dishes appear, Side-only dishes do not, and missing role information is distinguishable

#### Scenario: Browse family wishes
- **WHEN** the member chooses to explore wished dishes
- **THEN** active wished dishes and vote counts are available with the same inspect and assignment actions as other dishes

### Requirement: One nine-day planning window
The overview and assignment picker SHALL cover exactly nine consecutive calendar dates: the Saturday and Sunday before the selected Monday, Monday-Friday, and the following Saturday-Sunday. Previous/next week controls SHALL move the selected Monday seven days and update both surfaces together. Past dates within a selected window SHALL remain visible.

#### Scenario: Both weekends are visible
- **WHEN** the selected Monday is 2026-10-12
- **THEN** both surfaces cover 2026-10-10 through 2026-10-18 inclusive

#### Scenario: Navigate to the next week
- **WHEN** the member activates next week
- **THEN** the overview and assignment targets both cover the next nine-day window
- **AND** dish filters, browsing position, and conversation are preserved
- **AND** an optional selected day is cleared if it falls outside that window

### Requirement: Overview shows family decisions without judgments
The overview SHALL display dish names for each day, including multiple dishes, empty days, and existing opt-out reasons. It SHALL remain supporting context rather than the primary page content. It SHALL NOT assess balance, completeness of meals, nutrition, or variety, or mark a day with a side dish as a nutritionally complete meal.

#### Scenario: Multiple dishes on a day
- **WHEN** Monday has a main and potatoes planned
- **THEN** both dish names are visible and the family can inspect or remove either independently

#### Scenario: Family chooses similar meals
- **WHEN** the family plans similar dishes throughout the week
- **THEN** the overview shows their choices without warnings, quality scores, or unsolicited dietary advice

### Requirement: Assign freely through existing saved dinners
Every explored or recommended dish SHALL be assignable to any day in the window using the existing dinner persistence. The picker SHALL show existing dish names and opt-outs before selection. Adding SHALL preserve other menu items, prevent duplicates on the same date, and allow the dish on multiple dates. An optional selected day SHALL provide a direct-add action with access to other dates. Adding to an opted-out day SHALL explicitly disclose and require confirmation that the opt-out will be cleared.

#### Scenario: Add to an occupied day
- **WHEN** the member adds a main to a day that already has a side
- **THEN** both items are saved and shown without replacement

#### Scenario: Repeat for leftovers
- **WHEN** the member assigns an already-planned dish to a different day
- **THEN** the dish appears on both dates

#### Scenario: Assignment is already present
- **WHEN** the member selects a date already containing that dish
- **THEN** the existing assignment is identified and no duplicate or destructive undo is created

#### Scenario: Add to an opted-out day
- **WHEN** the member chooses a date marked Eating out
- **THEN** the change to that decision is explained and requires confirmation before the dish is added

#### Scenario: Shared dinner visibility
- **WHEN** an assignment is saved in the new workspace and the member opens existing Plan
- **THEN** the same assignment is visible without a separate publish or reconciliation step

### Requirement: Preserve the explore-decide loop
Recipe inspection, assignment, removal, and undo SHALL preserve search, filters, sort, results, conversation, and browsing position. Assignment SHALL update the week and planned-date indicators without closing exploration, advancing days, or automatically reranking results. Recipe inspection SHALL return focus to its triggering control. Suggestions SHALL be refreshable explicitly.

#### Scenario: Inspect and then assign
- **WHEN** the member opens recipe details, returns, and assigns the dish
- **THEN** exploration returns to the same position and the saved week updates without reordering the candidate list

#### Scenario: Continue on another day
- **WHEN** the member assigns to Tuesday and then selects Thursday
- **THEN** exploration remains open and the assignment action identifies Thursday without forcing a new search

### Requirement: Explicit removal and safe undo
The workspace SHALL support removing individual menu items and undoing successful additions and removals. Undo SHALL reverse only the action represented by its feedback and SHALL preserve unrelated menu items. If an addition cleared an opt-out, undo SHALL restore that prior decision when the day has not subsequently changed; otherwise it SHALL report that the prior state cannot safely be restored.

#### Scenario: Undo an added dish
- **WHEN** the member undoes a successful new assignment
- **THEN** only that assignment is removed and existing dishes are preserved

#### Scenario: Failed save
- **WHEN** a mutation fails
- **THEN** the member receives actionable retry feedback, unsaved work is not represented as saved, and exploration remains intact

### Requirement: Responsive accessible continuity
Desktop SHALL keep the week overview visible while dishes scroll. Mobile SHALL open on dish exploration with an accessible persistent week control showing the selected week; its full nine-day overview SHALL be reachable without route navigation. Previous/next week controls SHALL be available in the mobile overview. Controls SHALL support keyboard operation, visible focus, meaningful labels, and touch use. All new UI and recommendation explanations SHALL follow the selected English or Danish language.

#### Scenario: Mobile assignment loop
- **WHEN** a member on mobile views the week, returns to dishes, inspects a recipe, and assigns it
- **THEN** the original exploration position is retained and the week remains accessible above the bottom navigation

#### Scenario: Keyboard operation
- **WHEN** a member uses only the keyboard to inspect and assign a dish
- **THEN** all actions are reachable, dialogs manage focus, and closing returns focus to the initiating action

### Requirement: Isolate family and account state
The workspace SHALL clear conversation, results, selected day, pending feedback, and undo state when the active family or account changes. Late responses for another family, window, or superseded request SHALL NOT overwrite current state.

#### Scenario: Family changes during a request
- **WHEN** a recommendation request is pending and the member switches families
- **THEN** the new family receives fresh state and the old response is discarded
