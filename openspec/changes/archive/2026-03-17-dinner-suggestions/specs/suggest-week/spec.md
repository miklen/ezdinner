## ADDED Requirements

### Requirement: Family can request a full-week dinner suggestion
The system SHALL return one suggested dish per unplanned day for a 7-day period starting on a given Monday, based on each dish's usage history, rating, and leftover patterns.

#### Scenario: Week suggestion returned for unplanned week
- **WHEN** a family member requests a week suggestion for a week with no dinners planned
- **THEN** the system returns one suggested dish ID and name for each of the 7 days

#### Scenario: Already-planned days are excluded from suggestion
- **WHEN** a family member requests a week suggestion and some days already have dishes planned
- **THEN** the system returns suggestions only for the unplanned days; planned days are absent from the response

#### Scenario: High-rated dishes are preferred
- **WHEN** two dishes have equal usage history
- **THEN** the dish with a higher average rating SHALL receive a higher score and be more likely to appear in the suggestion

#### Scenario: Overdue dishes are preferred
- **WHEN** a dish has not been used in longer than its typical rotation interval
- **THEN** it SHALL receive a higher score than a dish used recently

#### Scenario: Recently used dishes are penalised
- **WHEN** a dish was used within the last 7 days
- **THEN** it SHALL receive a significant score penalty

#### Scenario: Leftover dishes score higher the day after they appear
- **WHEN** a dish historically appears on consecutive days at a rate of ≥ 30% of its appearances
- **AND** that dish is suggested or planned for the preceding day
- **THEN** it SHALL receive a leftover bonus on the following day

#### Scenario: Dish deleted from catalog is not suggested
- **WHEN** a dish is soft-deleted
- **THEN** it SHALL NOT appear in any suggestion

### Requirement: Family can reroll the full-week suggestion
The system SHALL accept a list of previously suggested dish IDs to exclude and return a new set of suggestions that avoids those dishes wherever possible.

#### Scenario: Reroll excludes previous suggestions
- **WHEN** a family member rerolls the week suggestion with an exclusion list
- **THEN** the response SHALL NOT include any dish from the exclusion list unless no other eligible dish exists

#### Scenario: Reroll falls back when exclusion list exhausts candidates
- **WHEN** the exclusion list covers all eligible dishes for a given day
- **THEN** the system SHALL return the best-scoring dish regardless of the exclusion list for that day
