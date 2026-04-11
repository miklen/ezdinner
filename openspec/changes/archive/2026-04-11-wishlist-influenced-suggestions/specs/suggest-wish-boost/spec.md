## ADDED Requirements

### Requirement: Wished dishes receive a score boost proportional to vote count
The suggestion engine SHALL apply a positive score bonus to any candidate dish that has an active (non-expired) wish entry for the requesting family. The bonus SHALL scale linearly with the number of votes on the wish: `score += voteCount × 0.3`.

#### Scenario: Dish with one vote receives base boost
- **WHEN** a dish has an active wish with 1 vote
- **THEN** the dish's total score SHALL be increased by 0.3

#### Scenario: Dish with three votes receives proportional boost
- **WHEN** a dish has an active wish with 3 votes
- **THEN** the dish's total score SHALL be increased by 0.9

#### Scenario: Dish with no wish entry is unaffected
- **WHEN** a dish has no active wish entry for the family
- **THEN** the wish-boost rule SHALL contribute 0 to that dish's score

#### Scenario: Expired wish does not boost the dish
- **WHEN** a dish has a wish entry whose `expiresAt` is in the past
- **THEN** the wish-boost rule SHALL contribute 0 to that dish's score

### Requirement: Wish boost produces a reason string
The wish-boost rule SHALL implement `IExplainableScoringRule` and return a human-readable explanation when the boost is non-zero. The explanation SHALL be included in the suggestion response's `reasons` array.

#### Scenario: Reason emitted for boosted dish
- **WHEN** a dish receives a wish-boost score contribution
- **THEN** the suggestion's `reasons` array SHALL contain a string of the form `"Wished for by the family (N votes)"` where N is the vote count

#### Scenario: No reason emitted for unboosted dish
- **WHEN** a dish receives no wish-boost contribution
- **THEN** the wish-boost rule SHALL return null from `Explain`, and no wish-related string SHALL appear in the `reasons` array for that dish

### Requirement: Wish context is carried in SuggestionContextValueObject
`SuggestionContextValueObject` SHALL include a `WishedDishIds` property of type `IReadOnlyDictionary<Guid, int>` mapping each wished dish ID to its vote count. Rules MAY read this property to apply wish-aware scoring.

#### Scenario: Context populated with active wishes
- **WHEN** the family has two active wished dishes with 1 and 3 votes respectively
- **THEN** `SuggestionContextValueObject.WishedDishIds` SHALL contain exactly those two dish IDs with their corresponding vote counts

#### Scenario: Context empty when no active wishes
- **WHEN** the family has no active wish entries
- **THEN** `SuggestionContextValueObject.WishedDishIds` SHALL be an empty dictionary

### Requirement: Suggestion queries inject wish context before scoring
Both the single-day and week suggestion queries SHALL fetch the family's active (non-expired) wish entries and include them in the `SuggestionContextValueObject` before invoking the scoring engine.

#### Scenario: Suggest-day query includes wish context
- **WHEN** a family member requests a single-day suggestion and the family has active wishes
- **THEN** the scoring engine SHALL receive a context where `WishedDishIds` reflects the current active wish state

#### Scenario: Suggest-week query includes wish context
- **WHEN** a family member requests a week suggestion and the family has active wishes
- **THEN** each day's scoring SHALL use the same wish context reflecting the current active wish state
