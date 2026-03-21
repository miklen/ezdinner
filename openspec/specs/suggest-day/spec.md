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

#### Scenario: Archived dishes are excluded
- **WHEN** a dish is archived
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

### Requirement: Suggestion candidates are filtered to main-role dishes when roles are populated
The system SHALL exclude dishes from suggestion candidates if their `Roles` field is populated and does not include `Main`. Dishes with no roles set (empty/null) SHALL remain eligible as candidates.

#### Scenario: Side dish excluded when roles are set
- **WHEN** a dish has `Roles = [Side]`
- **THEN** it SHALL NOT appear as a single-day suggestion

#### Scenario: Main dish included
- **WHEN** a dish has `Roles = [Main]`
- **THEN** it SHALL remain eligible as a single-day suggestion

#### Scenario: Dish with Main and Dessert roles included
- **WHEN** a dish has `Roles = [Main, Dessert]`
- **THEN** it SHALL remain eligible as a single-day suggestion

#### Scenario: Dish with no roles set remains eligible
- **WHEN** a dish has no roles assigned (metadata not yet enriched)
- **THEN** it SHALL remain eligible as a single-day suggestion

### Requirement: Single-day suggestion accepts optional effort preference
The suggest-day endpoint SHALL accept an optional `effortPreference` query parameter (`Quick`, `Medium`, or `Elaborate`) and pass it through to the suggestion engine context so that `EffortMatchRule` can score candidates accordingly.

#### Scenario: Effort preference passed through to scoring
- **WHEN** a family member requests a suggestion for a date with `effortPreference=Quick`
- **THEN** the engine SHALL apply `EffortMatchRule` with `Quick` preference when scoring candidates
- **AND** dishes with `EffortLevel = Quick` SHALL be scored higher than equivalent dishes with `EffortLevel = Elaborate`

#### Scenario: Suggestion without effort preference is unaffected
- **WHEN** a family member requests a suggestion with no `effortPreference` parameter
- **THEN** the engine SHALL not apply any effort-based scoring adjustment

### Requirement: Single-day suggestion response includes reasons
The suggest-day response SHALL include a `reasons` string array in the suggestion object, containing human-readable explanations for why the dish was chosen. This is in addition to the existing `dishId`, `dishName`, `rating`, and `daysSinceLast` fields.

#### Scenario: Reasons field present in day suggestion
- **WHEN** a single-day suggestion is returned
- **THEN** the suggestion object SHALL include a `reasons` field of type string array
- **AND** the array SHALL contain zero or more plain-language explanation strings
