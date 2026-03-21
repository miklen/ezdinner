## Requirements

### Requirement: Family member can archive a dish
The system SHALL allow a family member to archive a dish, removing it from suggestions and hiding it from the active catalog while preserving all historical dinner records that reference it.

#### Scenario: Dish is archived successfully
- **WHEN** a family member archives a dish
- **THEN** the dish SHALL be marked as archived
- **AND** it SHALL no longer appear in the active catalog listing
- **AND** it SHALL no longer appear as a suggestion candidate

#### Scenario: Archived dish still appears in dinner history
- **WHEN** a dish is archived
- **THEN** past dinner records that reference that dish SHALL remain unchanged and the dish name SHALL still be visible in history

#### Scenario: Already archived dish cannot be archived again
- **WHEN** a family member attempts to archive a dish that is already archived
- **THEN** the system SHALL return a 409 Conflict response

### Requirement: Family member can reactivate an archived dish
The system SHALL allow a family member to reactivate an archived dish, restoring it to full active status in the catalog and suggestion pool.

#### Scenario: Dish is reactivated successfully
- **WHEN** a family member reactivates an archived dish
- **THEN** the dish SHALL be marked as active
- **AND** it SHALL appear in the catalog listing again
- **AND** it SHALL be eligible as a suggestion candidate again

#### Scenario: Active dish cannot be reactivated
- **WHEN** a family member attempts to reactivate a dish that is not archived
- **THEN** the system SHALL return a 409 Conflict response

### Requirement: Archived dishes are hidden from the catalog by default
The system SHALL exclude archived dishes from catalog queries unless the caller explicitly opts in to include them.

#### Scenario: Default catalog listing excludes archived dishes
- **WHEN** a family member retrieves the dish catalog without specifying includeArchived
- **THEN** the response SHALL not include any archived dishes

#### Scenario: Catalog listing includes archived dishes when opted in
- **WHEN** a family member retrieves the dish catalog with includeArchived=true
- **THEN** the response SHALL include both active and archived dishes
- **AND** each archived dish SHALL be identifiable as archived in the response
