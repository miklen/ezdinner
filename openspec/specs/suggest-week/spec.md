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

#### Scenario: Archived dish is not suggested
- **WHEN** a dish is archived
- **THEN** it SHALL NOT appear in any suggestion

### Requirement: Family can reroll the full-week suggestion
The system SHALL accept a list of previously suggested dish IDs to exclude and return a new set of suggestions that avoids those dishes wherever possible.

#### Scenario: Reroll excludes previous suggestions
- **WHEN** a family member rerolls the week suggestion with an exclusion list
- **THEN** the response SHALL NOT include any dish from the exclusion list unless no other eligible dish exists

#### Scenario: Reroll falls back when exclusion list exhausts candidates
- **WHEN** the exclusion list covers all eligible dishes for a given day
- **THEN** the system SHALL return the best-scoring dish regardless of the exclusion list for that day

### Requirement: Suggestion candidates are filtered to main-role dishes when roles are populated
The system SHALL exclude dishes from week suggestion candidates if their `Roles` field is populated and does not include `Main`. Dishes with no roles set SHALL remain eligible.

#### Scenario: Side dish excluded from week suggestion
- **WHEN** a dish has `Roles = [Side]`
- **THEN** it SHALL NOT appear in any day of the week suggestion

#### Scenario: Main dish included in week suggestion
- **WHEN** a dish has `Roles = [Main]`
- **THEN** it SHALL remain eligible as a week suggestion candidate

#### Scenario: Dish with Main and Dessert roles included
- **WHEN** a dish has `Roles = [Main, Dessert]`
- **THEN** it SHALL remain eligible as a week suggestion candidate

#### Scenario: Dish with no roles set remains eligible
- **WHEN** a dish has no roles assigned
- **THEN** it SHALL remain eligible as a week suggestion candidate

### Requirement: Week suggestion accepts per-day effort preferences
The suggest-week endpoint SHALL accept an optional `effortPreferences` parameter that maps individual dates within the week to an effort preference value. Each day's preference is applied independently when scoring candidates for that day.

#### Scenario: Per-day effort preference applied to correct day
- **WHEN** a family member requests a week suggestion with `effortPreferences` specifying `Quick` for Friday
- **THEN** Friday's suggestion SHALL be scored with `EffortMatchRule` using `Quick`
- **AND** other days in the week SHALL be scored without effort preference (unless also specified)

#### Scenario: Days without effort preference score without effort bias
- **WHEN** a family member requests a week suggestion and provides effort preference for only some days
- **THEN** days without a specified preference SHALL not have effort-based scoring applied

### Requirement: Week suggestion response includes reasons per day
The suggest-week response SHALL include a `reasons` string array in each day's suggestion object, containing human-readable explanations for why the dish was chosen for that day.

#### Scenario: Reasons field present in each day of week suggestion
- **WHEN** a week suggestion is returned
- **THEN** each day's suggestion object SHALL include a `reasons` field of type string array
- **AND** the array SHALL contain zero or more plain-language explanation strings
