## ADDED Requirements

### Requirement: Week suggestion accepts per-day effort preferences
The suggest-week endpoint SHALL accept an optional `effortPreferences` parameter that maps individual dates within the week to an effort preference value. Each day's preference is applied independently when scoring candidates for that day.

#### Scenario: Per-day effort preference applied to correct day
- **WHEN** a family member requests a week suggestion with `effortPreferences` specifying `Quick` for Friday
- **THEN** Friday's suggestion SHALL be scored with `EffortMatchRule` using `Quick`
- **AND** other days in the week SHALL be scored without effort preference (unless also specified)

#### Scenario: Days without effort preference score without effort bias
- **WHEN** a family member requests a week suggestion and provides effort preference for only some days
- **THEN** days without a specified preference SHALL not have effort-based scoring applied
