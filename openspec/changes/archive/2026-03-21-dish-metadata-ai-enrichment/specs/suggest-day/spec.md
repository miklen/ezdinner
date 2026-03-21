## ADDED Requirements

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
