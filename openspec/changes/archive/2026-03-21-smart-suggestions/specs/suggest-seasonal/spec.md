## ADDED Requirements

### Requirement: Seasonal affinity rule boosts season-matching dishes
The suggestion engine SHALL include a `SeasonalAffinityRule` that applies a positive score to dishes whose `SeasonAffinity` matches the season of the suggestion target date. Dishes with `SeasonAffinity = AllYear` or with no affinity set SHALL receive a neutral score of zero from this rule.

Season mapping from target date month:
- Winter: December, January, February
- Spring: March, April, May
- Summer: June, July, August
- Autumn: September, October, November

#### Scenario: Dish matching current season receives a boost
- **WHEN** the suggestion target date falls in a summer month (June, July, or August)
- **AND** a dish has `SeasonAffinity = Summer`
- **THEN** the dish SHALL receive a positive score from `SeasonalAffinityRule`

#### Scenario: Dish not matching current season receives a penalty
- **WHEN** the suggestion target date falls in a summer month
- **AND** a dish has `SeasonAffinity = Winter`
- **THEN** the dish SHALL receive a negative score from `SeasonalAffinityRule`

#### Scenario: AllYear dish scores neutrally
- **WHEN** a dish has `SeasonAffinity = AllYear`
- **THEN** the dish SHALL receive a score of zero from `SeasonalAffinityRule` regardless of the target date

#### Scenario: Dish with no season affinity scores neutrally
- **WHEN** a dish has no `SeasonAffinity` set (unenriched)
- **THEN** the dish SHALL receive a score of zero from `SeasonalAffinityRule`

### Requirement: Season is derived from the suggestion target date
The `SeasonalAffinityRule` SHALL derive the current season from the suggestion target date, not the system clock or request date.

#### Scenario: Season derived from target date month
- **WHEN** the suggestion target date is in December
- **THEN** the season SHALL be Winter regardless of when the request is made
