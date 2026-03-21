## ADDED Requirements

### Requirement: Week suggestion response includes reasons per day
The suggest-week response SHALL include a `reasons` string array in each day's suggestion object, containing human-readable explanations for why the dish was chosen for that day.

#### Scenario: Reasons field present in each day of week suggestion
- **WHEN** a week suggestion is returned
- **THEN** each day's suggestion object SHALL include a `reasons` field of type string array
- **AND** the array SHALL contain zero or more plain-language explanation strings
