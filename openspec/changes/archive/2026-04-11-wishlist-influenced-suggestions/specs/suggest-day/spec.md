## ADDED Requirements

### Requirement: Single-day suggestion boosts wished dishes
The suggest-day query SHALL include active wish data in the suggestion context so that `WishlistBoostRule` can apply a vote-proportional score bonus to wished candidates.

#### Scenario: Wished dish appears higher in single-day suggestion
- **WHEN** a family member requests a single-day suggestion
- **AND** one eligible dish has an active wish with 2 votes while an otherwise equal-scoring dish does not
- **THEN** the wished dish SHALL score higher and be returned as the suggestion

#### Scenario: Wish boost reason appears in single-day suggestion
- **WHEN** a single-day suggestion returns a dish that has an active wish
- **THEN** the suggestion's `reasons` array SHALL contain a wish-boost explanation string
