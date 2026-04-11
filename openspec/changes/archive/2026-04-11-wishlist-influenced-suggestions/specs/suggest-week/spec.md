## ADDED Requirements

### Requirement: Week suggestion boosts wished dishes
The suggest-week query SHALL include active wish data in the suggestion context so that `WishlistBoostRule` can apply a vote-proportional score bonus to wished candidates for each day.

#### Scenario: Wished dish appears in week suggestion
- **WHEN** a family member requests a week suggestion
- **AND** one eligible dish has an active wish with 2 votes while an otherwise equal-scoring dish does not
- **THEN** the wished dish SHALL score higher and appear in the week suggestion

#### Scenario: Wish boost reason appears in week suggestion day
- **WHEN** a week suggestion returns a day whose dish has an active wish
- **THEN** that day's `reasons` array SHALL contain a wish-boost explanation string

#### Scenario: Same wish context used for all days in a week suggestion
- **WHEN** a week suggestion is generated
- **THEN** all days SHALL be scored with the same wish context (fetched once per request, not per day)
