## ADDED Requirements

### Requirement: Family can request a single-day dinner suggestion
The system SHALL return one suggested dish for a specific date, scored using the same heuristics as the week suggestion (usage history, rating, leftover patterns).

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

### Requirement: Family can reroll the single-day suggestion
The system SHALL accept a list of previously suggested dish IDs to exclude for that date and return a new suggestion that avoids those dishes wherever possible.

#### Scenario: Reroll returns a different dish
- **WHEN** a family member rerolls a single-day suggestion and provides the previous suggestion as an exclusion
- **THEN** the response SHALL NOT include any dish from the exclusion list unless no other eligible dish exists

#### Scenario: Reroll falls back when all dishes are excluded
- **WHEN** the exclusion list covers all eligible dishes for the requested date
- **THEN** the system SHALL return the best-scoring dish regardless of the exclusion list

### Requirement: Scoring produces a deterministic ranking
The suggestion engine SHALL apply scoring rules in a consistent, repeatable order so that the same inputs always produce the same ranked list. Variation between rerolls comes solely from the exclusion list, not from randomness.

#### Scenario: Same inputs produce same top suggestion
- **WHEN** the same dish catalog, dinner history, and date are provided with an empty exclusion list
- **THEN** the system SHALL return the same top-ranked dish on every call

#### Scenario: Exclusion list shifts the selected dish
- **WHEN** the top-ranked dish is in the exclusion list
- **THEN** the system SHALL return the next-highest-ranked dish not in the exclusion list
