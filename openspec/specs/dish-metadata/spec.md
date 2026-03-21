## ADDED Requirements

### Requirement: Dish has structured metadata fields
The system SHALL store four structured metadata fields on each dish: `Roles` (set of role values), `EffortLevel`, `SeasonAffinity`, and `Cuisine`. Each field SHALL have an associated confirmed flag indicating whether the value was set by the user (`true`) or is an AI suggestion pending review (`false`).

Valid values:
- `Roles`: one or more of `Main`, `Side`, `Dessert`, `Other`
- `EffortLevel`: `Quick`, `Medium`, `Elaborate`
- `SeasonAffinity`: `Summer`, `Winter`, `Spring`, `Autumn`, `AllYear`
- `Cuisine`: free-form string (e.g., "Danish", "Italian")

#### Scenario: Metadata fields default to unset
- **WHEN** a dish is created without metadata
- **THEN** all metadata fields SHALL be null/empty and all confirmed flags SHALL be false

#### Scenario: Metadata fields are returned with dish
- **WHEN** a family member retrieves a dish
- **THEN** the response SHALL include all metadata fields and their confirmed flags

### Requirement: Family member can update and confirm dish metadata
The system SHALL allow a family member to set or update individual metadata fields on a dish. Setting a field via this command SHALL mark it as confirmed, locking it against future AI re-enrichment.

#### Scenario: User sets and confirms a metadata field
- **WHEN** a family member updates a dish's `EffortLevel` to `Quick`
- **THEN** the dish's `EffortLevel` SHALL be `Quick` and `EffortLevelConfirmed` SHALL be `true`

#### Scenario: Confirmed field is not overwritten by re-enrichment
- **WHEN** a dish's `EffortLevel` is confirmed
- **AND** the enrich endpoint is called for that dish
- **THEN** `EffortLevel` SHALL remain unchanged

#### Scenario: Unconfirmed field is overwritten by re-enrichment
- **WHEN** a dish's `SeasonAffinity` is not confirmed
- **AND** the enrich endpoint is called for that dish
- **THEN** `SeasonAffinity` SHALL be updated to the AI-inferred value

### Requirement: Dish roles support multiple values
The system SHALL allow a dish to have more than one role assigned simultaneously.

#### Scenario: Dish assigned both Main and Dessert roles
- **WHEN** a family member sets a dish's roles to `[Main, Dessert]`
- **THEN** the dish SHALL appear as eligible in contexts that filter by the `Main` role
- **AND** the dish SHALL also be identifiable as a `Dessert`
