## Requirements

### Requirement: Dish catalog displays metadata as visual tags on each card
The system SHALL render dish metadata fields (roles, effort level, season affinity, cuisine) as compact tag pills on each DishCard in the dish catalog. Only metadata fields that are set SHALL be shown. Cards without any metadata SHALL show no tag row.

#### Scenario: Dish with full metadata shows all tags
- **WHEN** a dish has roles, effortLevel, seasonAffinity, and cuisine set
- **THEN** the DishCard SHALL display a tag row with one pill per set field
- **AND** each pill SHALL show an icon and the value label

#### Scenario: Dish with partial metadata shows only set tags
- **WHEN** a dish has effortLevel set but no roles, seasonAffinity, or cuisine
- **THEN** the DishCard SHALL display only the effort tag
- **AND** no empty placeholder tags SHALL be shown

#### Scenario: Dish with no metadata shows no tag row
- **WHEN** a dish has no metadata fields set
- **THEN** the DishCard SHALL not render a tag row

### Requirement: Dish catalog supports filtering by metadata values
The system SHALL allow users to filter the dish catalog by selecting one or more metadata values. Filtering SHALL be client-side on the already-loaded dish list and SHALL work in combination with the existing text search and sort.

#### Scenario: User filters by a single metadata value
- **WHEN** a user selects the "Quick" effort filter chip
- **THEN** only dishes with effortLevel === "Quick" SHALL be visible
- **AND** dishes without effortLevel set SHALL be hidden

#### Scenario: Multiple filters within the same dimension act as OR
- **WHEN** a user selects both "Main" and "Side" role filter chips
- **THEN** dishes with role "Main" OR "Side" SHALL be visible

#### Scenario: Filters across dimensions act as AND
- **WHEN** a user selects "Quick" effort AND "Summer" season filters
- **THEN** only dishes matching both conditions SHALL be visible

#### Scenario: Filter chips can be toggled off
- **WHEN** a user clicks an active filter chip
- **THEN** that filter SHALL be deactivated
- **AND** the dish list SHALL update accordingly

#### Scenario: Clear all filters restores full list
- **WHEN** one or more filters are active and the user clicks "Clear filters"
- **THEN** all active filters SHALL be removed
- **AND** the full unfiltered dish list SHALL be shown (subject to text search)

### Requirement: Filter chips show available metadata values from loaded dishes
The system SHALL derive filter options from the distinct metadata values present in the loaded dish list. Cuisine values SHALL be deduplicated case-insensitively. Filter chips SHALL only show values that exist in at least one dish.

#### Scenario: Cuisine filter options reflect loaded dishes
- **WHEN** the dish catalog is loaded and contains dishes with cuisines "Danish" and "Italian"
- **THEN** the filter area SHALL show "Danish" and "Italian" as cuisine filter options
- **AND** cuisine values not present in any dish SHALL NOT appear as filter options
