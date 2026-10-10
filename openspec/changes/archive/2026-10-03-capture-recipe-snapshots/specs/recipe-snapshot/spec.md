# Spec Delta

## Purpose

Preserve a usable point-in-time copy of a linked recipe while keeping family-authored notes and adjustments under exclusive user control.

## ADDED Requirements

### Requirement: Existing dishes remain readable without migration
The system SHALL treat a missing recipe snapshot as no captured recipe and SHALL continue to read and update existing dish documents without requiring a data migration.

#### Scenario: Existing dish has no snapshot property
- **WHEN** the system reads a dish document created before recipe snapshots were introduced
- **THEN** the dish SHALL be returned successfully with no captured recipe
- **AND** its existing URL and user-authored notes SHALL remain unchanged

### Requirement: Family member can preview a recipe import
The system SHALL allow an authorized family member to extract a recipe from the dish's saved source URL and review the candidate snapshot before it is persisted.

#### Scenario: Recipe is extracted successfully
- **WHEN** an authorized family member requests an import preview for a dish with a supported public HTTP or HTTPS source URL
- **THEN** the system SHALL return a candidate containing the source URL, recipe content, source-content hash, and extraction warnings
- **AND** the dish SHALL remain unchanged until the family member confirms the candidate

#### Scenario: Dish has no source URL
- **WHEN** a family member requests an import preview for a dish without a source URL
- **THEN** the system SHALL reject the request with an actionable error
- **AND** the dish and its notes SHALL remain unchanged

#### Scenario: Source cannot produce a trustworthy recipe
- **WHEN** the source cannot be retrieved or no recipe can be extracted and validated
- **THEN** the system SHALL report that no recipe snapshot can be created
- **AND** SHALL NOT invent missing ingredients or instructions
- **AND** the dish SHALL remain unchanged

### Requirement: Recipe extraction is safe and bounded
The system SHALL treat remote pages as untrusted input and SHALL retrieve and process only supported public web resources within configured redirect, response-size, and timeout limits.

#### Scenario: Source resolves to a non-public network destination
- **WHEN** a source URL or any redirect resolves to a loopback, link-local, private, or otherwise prohibited network destination
- **THEN** the system SHALL refuse to retrieve it
- **AND** SHALL return a safe import failure without changing the dish

#### Scenario: Source exceeds a processing limit
- **WHEN** retrieval exceeds the configured timeout, redirect count, or response-size limit
- **THEN** the system SHALL stop processing
- **AND** SHALL return a safe import failure without changing the dish

### Requirement: Extraction prefers source facts over generated content
The system SHALL use machine-readable recipe data when it is sufficiently complete and SHALL use an LLM only to extract recipe facts from supplied source content when deterministic extraction is insufficient. Remote content SHALL be treated as data rather than instructions.

#### Scenario: Complete structured recipe data is available
- **WHEN** the source contains valid structured recipe data with a usable ingredient list and instructions
- **THEN** the candidate SHALL be derived from that structured data without an LLM call

#### Scenario: Structured recipe data is absent or incomplete
- **WHEN** deterministic extraction cannot produce a usable recipe
- **THEN** the system SHALL provide bounded source content to the configured extraction provider
- **AND** SHALL validate the provider result before returning a candidate

#### Scenario: Extracted output adds unsupported facts
- **WHEN** extracted content cannot be supported by the retrieved source
- **THEN** the unsupported content SHALL NOT be included in the candidate
- **AND** the candidate SHALL include a warning or the import SHALL fail

### Requirement: Confirmed import preserves user-authored notes
The system SHALL store a confirmed candidate as an optional recipe snapshot separate from the dish's user-authored notes.

#### Scenario: Family member confirms an initial import
- **WHEN** an authorized family member confirms a valid recipe candidate
- **THEN** the dish SHALL store the captured recipe content, source URL, capture time, and source-content hash
- **AND** the dish's user-authored notes SHALL remain byte-for-byte unchanged

#### Scenario: Candidate source no longer matches the dish URL
- **WHEN** a family member attempts to confirm a candidate after the dish's source URL has changed
- **THEN** the system SHALL reject the stale candidate
- **AND** SHALL preserve the current snapshot and user-authored notes

### Requirement: Refresh requires informed confirmation
The system SHALL preview a refreshed recipe and warn that confirming it replaces the current captured recipe before any existing snapshot is changed.

#### Scenario: Source content has changed
- **WHEN** a refresh preview has a different source-content hash from the stored snapshot
- **THEN** the system SHALL identify the candidate as changed
- **AND** SHALL require explicit confirmation before replacing the snapshot

#### Scenario: Source content has not changed
- **WHEN** a refresh preview has the same source-content hash as the stored snapshot
- **THEN** the system SHALL inform the family member that no source change was detected
- **AND** SHALL leave the existing snapshot unchanged by default

#### Scenario: Family member confirms a changed refresh
- **WHEN** an authorized family member confirms a changed refresh candidate
- **THEN** the current recipe snapshot SHALL be replaced with the candidate
- **AND** the dish's user-authored notes SHALL remain byte-for-byte unchanged

#### Scenario: Family member cancels refresh
- **WHEN** the family member cancels a refresh preview
- **THEN** the existing recipe snapshot and user-authored notes SHALL remain unchanged

### Requirement: URL changes do not silently discard snapshots
The system SHALL manage the dish source URL independently from the captured recipe snapshot.

#### Scenario: Source URL is changed
- **WHEN** a family member saves a different source URL
- **THEN** the existing snapshot SHALL remain available
- **AND** the system SHALL indicate that the snapshot was captured from a different URL until a new import is confirmed

#### Scenario: Source URL is removed
- **WHEN** a family member removes the dish's source URL
- **THEN** the existing snapshot SHALL remain available
- **AND** refresh SHALL be unavailable until a source URL is supplied

### Requirement: Snapshot removal is explicit
The system SHALL allow an authorized family member to remove a captured recipe only after explicit confirmation.

#### Scenario: Family member removes a snapshot
- **WHEN** an authorized family member confirms snapshot removal
- **THEN** the system SHALL remove only the recipe snapshot
- **AND** SHALL preserve the dish's URL and user-authored notes

### Requirement: Dish details distinguish notes from captured recipe
The system SHALL present family-authored notes and the captured recipe as distinct sections within the dish details experience on supported desktop and mobile layouts.

#### Scenario: Dish has notes and a snapshot
- **WHEN** a family member views a dish with user-authored notes and a recipe snapshot
- **THEN** both sections SHALL be visible with distinct headings
- **AND** the recipe section SHALL show its source and capture time

#### Scenario: Dish has a source URL but no snapshot
- **WHEN** a family member views a dish with a source URL and no recipe snapshot
- **THEN** the interface SHALL offer an import action
- **AND** SHALL continue to display and edit user-authored notes normally

### Requirement: New recipe snapshot interface text is localized
All user-visible text introduced for recipe capture, preview, warnings, errors, refresh, and removal SHALL be available in English and Danish.

#### Scenario: Family member changes language
- **WHEN** a family member views the recipe snapshot interface in either supported language
- **THEN** headings, actions, statuses, warnings, and errors SHALL use the selected language

### Requirement: Captured recipe informs dish metadata enrichment
The system SHALL include captured recipe content as additional evidence when enriching dish metadata, while preserving the existing protection for confirmed metadata fields.

#### Scenario: Dish has a captured recipe
- **WHEN** enrichment is requested for a dish with a recipe snapshot
- **THEN** the system SHALL infer metadata from the dish name, user-authored notes, and captured recipe content
- **AND** confirmed metadata fields SHALL NOT be updated

#### Scenario: Dish has no captured recipe
- **WHEN** enrichment is requested for a dish without a recipe snapshot
- **THEN** enrichment SHALL continue to use the dish name and available user-authored notes
- **AND** SHALL behave compatibly with dishes created before recipe snapshots were introduced
