## ADDED Requirements

### Requirement: Suggestion candidates are filtered to main-role dishes when roles are populated
The system SHALL exclude dishes from week suggestion candidates if their `Roles` field is populated and does not include `Main`. Dishes with no roles set SHALL remain eligible.

#### Scenario: Side dish excluded from week suggestion
- **WHEN** a dish has `Roles = [Side]`
- **THEN** it SHALL NOT appear in any day of the week suggestion

#### Scenario: Main dish included in week suggestion
- **WHEN** a dish has `Roles = [Main]`
- **THEN** it SHALL remain eligible as a week suggestion candidate

#### Scenario: Dish with Main and Dessert roles included
- **WHEN** a dish has `Roles = [Main, Dessert]`
- **THEN** it SHALL remain eligible as a week suggestion candidate

#### Scenario: Dish with no roles set remains eligible
- **WHEN** a dish has no roles assigned
- **THEN** it SHALL remain eligible as a week suggestion candidate
