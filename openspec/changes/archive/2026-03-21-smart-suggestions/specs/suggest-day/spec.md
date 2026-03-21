## ADDED Requirements

### Requirement: Single-day suggestion accepts optional effort preference
The suggest-day endpoint SHALL accept an optional `effortPreference` query parameter (`Quick`, `Medium`, or `Elaborate`) and pass it through to the suggestion engine context so that `EffortMatchRule` can score candidates accordingly.

#### Scenario: Effort preference passed through to scoring
- **WHEN** a family member requests a suggestion for a date with `effortPreference=Quick`
- **THEN** the engine SHALL apply `EffortMatchRule` with `Quick` preference when scoring candidates
- **AND** dishes with `EffortLevel = Quick` SHALL be scored higher than equivalent dishes with `EffortLevel = Elaborate`

#### Scenario: Suggestion without effort preference is unaffected
- **WHEN** a family member requests a suggestion with no `effortPreference` parameter
- **THEN** the engine SHALL not apply any effort-based scoring adjustment
