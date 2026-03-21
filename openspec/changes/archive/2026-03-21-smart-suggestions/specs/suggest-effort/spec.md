## ADDED Requirements

### Requirement: Effort match rule boosts dishes matching the day's effort preference
The suggestion engine SHALL include an `EffortMatchRule` that applies a positive score to dishes whose `EffortLevel` matches the `EffortPreference` set on the suggestion context. The rule SHALL only fire when an effort preference is present on the context. Dishes with no `EffortLevel` set SHALL score neutrally regardless of whether an effort preference is set.

#### Scenario: Matching effort level receives a boost
- **WHEN** the suggestion context has `EffortPreference = Quick`
- **AND** a dish has `EffortLevel = Quick`
- **THEN** the dish SHALL receive a positive score from `EffortMatchRule`

#### Scenario: Non-matching effort level receives a penalty
- **WHEN** the suggestion context has `EffortPreference = Quick`
- **AND** a dish has `EffortLevel = Elaborate`
- **THEN** the dish SHALL receive a negative score from `EffortMatchRule`

#### Scenario: Rule does not fire when no effort preference is set
- **WHEN** the suggestion context has no `EffortPreference`
- **THEN** all dishes SHALL receive a score of zero from `EffortMatchRule`

#### Scenario: Dish with no effort level scores neutrally
- **WHEN** a dish has no `EffortLevel` set (unenriched)
- **AND** the suggestion context has an `EffortPreference`
- **THEN** the dish SHALL receive a score of zero from `EffortMatchRule`

### Requirement: Effort preference is optional on suggestion context
The `SuggestionContextValueObject` SHALL accept an optional `EffortPreference` value. When absent, effort-based scoring SHALL not apply. When present, it SHALL be one of `Quick`, `Medium`, or `Elaborate`.

#### Scenario: Suggestion without effort preference behaves as before
- **WHEN** a suggestion is requested with no `effortPreference` parameter
- **THEN** the suggestion SHALL be scored without any effort-based adjustment
- **AND** results SHALL be equivalent to the engine before `EffortMatchRule` was added (for the same inputs)
