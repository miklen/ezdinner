# Spec Delta

## ADDED Requirements

### Requirement: Catalog view preferences survive navigation and browser sessions

The system SHALL automatically remember text search, selected role/effort/season/cuisine filters, archived-only view, sort key, and sort direction for the dishes catalog in the current browser. It SHALL restore these preferences when returning to the page, refreshing it, or reopening the app in a later browser session. Filtering and sorting SHALL apply to freshly loaded dishes rather than a saved catalog snapshot.

#### Scenario: Return from another page
- **WHEN** a user selects multiple metadata filters, enters a search, chooses a sort key and direction, navigates to another app page, and returns to dishes
- **THEN** the controls SHALL retain all those selections
- **AND** the loaded dish list SHALL use the retained filtering and sorting

#### Scenario: Refresh or reopen the app
- **WHEN** a user refreshes the dishes page or reopens the app in the same browser with the same signed-in account and family
- **THEN** the catalog SHALL restore the last saved view preferences

#### Scenario: Restore the archived view
- **WHEN** a user returns to dishes with archived-only view saved
- **THEN** the catalog SHALL load the data needed to display archived dishes and apply the saved filters and sorting
- **AND** active-dish pickers and planning suggestions SHALL continue to exclude archived dishes

#### Scenario: First visit has no saved preferences
- **WHEN** no preferences exist for the signed-in account and active family
- **THEN** the catalog SHALL start with empty search, no metadata selections, active-only view, and name sorting ascending

### Requirement: Catalog preferences are isolated by account and family

The system SHALL remember independent catalog preferences for each signed-in account and family in the current browser. Switching families or signing in with a different account SHALL restore that scope's preferences or its defaults without copying selections from the previous scope. Preferences SHALL NOT be saved under an unidentified account or family.

#### Scenario: Switch families and return
- **WHEN** a user selects filters in family A, switches to family B, changes B's sort order, and switches back to A
- **THEN** family A's view SHALL be restored
- **AND** family B's filters and sort order SHALL remain independently remembered

#### Scenario: Different account uses the same browser
- **WHEN** another account opens the dishes page for the same family
- **THEN** it SHALL receive its own saved preferences or defaults
- **AND** it SHALL NOT receive the previous account's selections

### Requirement: Clearing and changing controls updates remembered preferences

The system SHALL update remembered preferences automatically when the user changes a catalog control. "Clear filters" SHALL remove metadata selections and return to active-only view while retaining the current search, sort key, and sort direction. Filter-panel expansion, creation-dialog state, and assistant-panel state SHALL remain temporary.

#### Scenario: Clear filters and revisit
- **WHEN** a user clears active metadata filters and archived view, then leaves and returns to the catalog
- **THEN** metadata filters SHALL remain cleared and active-only view SHALL remain selected
- **AND** the previous search and sorting SHALL remain selected

#### Scenario: Clear the search separately
- **WHEN** a user clears the search field and later reopens the catalog
- **THEN** the search SHALL remain empty
- **AND** metadata selections and sorting SHALL remain as last selected

#### Scenario: Sort interactions retain their existing behavior
- **WHEN** a user selects the current sort key again or selects another sort key
- **THEN** selecting the current key SHALL toggle its direction
- **AND** selecting another key SHALL choose ascending for name or descending for rating, times used, and last used
- **AND** the resulting key and direction SHALL be remembered

### Requirement: Saved preferences are safe to restore

The system SHALL validate saved preferences before applying them. Missing, malformed, or unsupported saved data SHALL fall back to valid defaults without blocking the catalog. Unavailable browser storage SHALL NOT prevent in-memory filtering, searching, or sorting. After a successful catalog load, selected metadata values absent from the loaded filter options SHALL be removed and the corrected selections SHALL be remembered when storage is available. Failed or pending loads SHALL NOT remove selections.

#### Scenario: Invalid saved data
- **WHEN** the saved record is malformed, has an unsupported format version, or contains invalid field values
- **THEN** the catalog SHALL use valid defaults for the unusable record or fields
- **AND** the user SHALL be able to change filters and sorting

#### Scenario: Browser storage is unavailable
- **WHEN** reading or writing browser preferences fails
- **THEN** the catalog SHALL remain usable with in-memory view preferences
- **AND** controls and dish results SHALL continue to respond to user changes

#### Scenario: Previously selected metadata no longer exists
- **WHEN** a successful catalog load provides no filter option matching a remembered metadata selection
- **THEN** that selection SHALL be removed from the controls and filtering
- **AND** other valid selections, search, archived view, and sorting SHALL be retained

#### Scenario: Catalog loading fails
- **WHEN** a catalog load fails before its available filter options are known
- **THEN** the remembered metadata selections SHALL remain intact for a later successful load
