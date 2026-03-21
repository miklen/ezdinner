## ADDED Requirements

### Requirement: Family member can trigger AI enrichment for a dish
The system SHALL provide an endpoint that infers dish metadata from the dish's name and notes using the Claude API and writes the results as AI-suggested (unconfirmed) values. Only fields that are not already confirmed SHALL be updated.

#### Scenario: Enrichment infers metadata from name and notes
- **WHEN** the enrich endpoint is called for a dish with a name and notes
- **THEN** the system SHALL call the Claude API with the dish name and notes
- **AND** SHALL write inferred values for `Roles`, `EffortLevel`, `SeasonAffinity`, and `Cuisine` where they are not already confirmed
- **AND** all written fields SHALL have their confirmed flag set to `false`

#### Scenario: Enrichment from name only when notes are absent
- **WHEN** the enrich endpoint is called for a dish with no notes
- **THEN** the system SHALL infer metadata from the dish name alone
- **AND** SHALL write the inferred values for unconfirmed fields

#### Scenario: Confirmed fields are not overwritten
- **WHEN** the enrich endpoint is called for a dish with one or more confirmed metadata fields
- **THEN** confirmed fields SHALL NOT be updated
- **AND** only unconfirmed fields SHALL receive new AI-suggested values

#### Scenario: Enrichment failure does not affect dish usability
- **WHEN** the Claude API call fails (timeout, error, or invalid response)
- **THEN** the dish SHALL remain unchanged
- **AND** the endpoint SHALL return an appropriate error response without corrupting existing metadata

### Requirement: Enrichment endpoint is idempotent for confirmed fields
Calling the enrich endpoint multiple times SHALL never change a confirmed field, regardless of what the AI infers.

#### Scenario: Repeated enrichment calls do not alter confirmed fields
- **WHEN** the enrich endpoint is called twice for the same dish
- **AND** the user confirmed a field between calls
- **THEN** the confirmed field SHALL have the user's value after both calls

### Requirement: AI enrichment is provider-agnostic
The enrichment capability SHALL be implemented behind an `IDishEnrichmentProvider` interface in the Application layer. The concrete provider implementation SHALL live in Infrastructure and SHALL be injectable via DI. The Application layer SHALL NOT reference any AI vendor SDK directly.

#### Scenario: Provider can be replaced without changing application or domain logic
- **GIVEN** the system has an `IDishEnrichmentProvider` registered in DI
- **WHEN** a different provider implementation is registered instead
- **THEN** the enrichment endpoint SHALL work identically with no changes to command handlers, domain objects, or API functions

#### Scenario: Provider result is mapped to a model-agnostic value type
- **WHEN** the provider returns enrichment results
- **THEN** the result SHALL be expressed as `DishEnrichmentResult` (defined in Application layer)
- **AND** `DishEnrichmentResult` SHALL contain no SDK-specific types or vendor references

### Requirement: Frontend triggers enrichment automatically after dish save
The frontend SHALL call the enrich endpoint immediately after a successful dish create or update, and SHALL display a visible loading state during enrichment.

#### Scenario: Enrichment triggered after dish creation
- **WHEN** a family member creates a new dish and it saves successfully
- **THEN** the frontend SHALL immediately call the enrich endpoint for the new dish
- **AND** SHALL display an "Analyzing dish..." indicator while enrichment is in progress
- **AND** SHALL update the displayed metadata fields when enrichment completes

#### Scenario: Enrichment triggered after dish edit
- **WHEN** a family member edits a dish (name or notes changed) and saves
- **THEN** the frontend SHALL call the enrich endpoint to refresh unconfirmed fields

### Requirement: Notes field provides enrichment guidance to the user
The dish form SHALL display a hint near the notes field explaining what information improves enrichment quality.

#### Scenario: Notes hint is visible when editing a dish
- **WHEN** a family member opens the dish form (create or edit)
- **THEN** a hint SHALL be visible near the notes field
- **AND** the hint SHALL mention prep time, season, and main ingredients as examples of useful notes
