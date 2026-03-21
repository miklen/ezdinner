## ADDED Requirements

### Requirement: Single-day suggestion response includes reasons
The suggest-day response SHALL include a `reasons` string array in the suggestion object, containing human-readable explanations for why the dish was chosen. This is in addition to the existing `dishId`, `dishName`, `rating`, and `daysSinceLast` fields.

#### Scenario: Reasons field present in day suggestion
- **WHEN** a single-day suggestion is returned
- **THEN** the suggestion object SHALL include a `reasons` field of type string array
- **AND** the array SHALL contain zero or more plain-language explanation strings
