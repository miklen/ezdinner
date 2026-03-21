## MODIFIED Requirements

### Requirement: Family can request a single-day dinner suggestion
The system SHALL return one suggested dish for a specific date, scored using the same heuristics as the week suggestion (usage history, rating, leftover patterns). Archived and soft-deleted dishes SHALL be excluded from candidates.

#### Scenario: Single-day suggestion returned for an unplanned day
- **WHEN** a family member requests a suggestion for a date with no dinner planned
- **THEN** the system returns one suggested dish ID and name for that date

#### Scenario: Single-day suggestion respects context of adjacent planned days
- **WHEN** a dish with a leftover pattern is already planned for the day before the requested date
- **THEN** that dish SHALL receive a leftover bonus and be scored accordingly

#### Scenario: No suggestion returned when day is already planned
- **WHEN** a family member requests a suggestion for a date that already has a dish planned
- **THEN** the system SHALL return an empty suggestion (no dish), indicating nothing to suggest

#### Scenario: Deleted dishes are excluded
- **WHEN** a dish is soft-deleted
- **THEN** it SHALL NOT appear as a suggestion for any date

#### Scenario: Archived dishes are excluded
- **WHEN** a dish is archived
- **THEN** it SHALL NOT appear as a suggestion for any date
