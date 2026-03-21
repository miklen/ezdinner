## ADDED Requirements

### Requirement: Scoring rules produce human-readable explanations
Each scoring rule that has a notable contribution SHALL be able to produce a concise plain-language reason string. Rules with no notable contribution (zero score, neutral case) SHALL return no reason. Reasons are surfaced only for the top-ranked candidate on each suggestion slot.

#### Scenario: High-rated dish explanation
- **WHEN** a dish is the top-ranked suggestion and has a rating of 6 or higher
- **THEN** the reasons list SHALL include "Rated {N}/10"

#### Scenario: Overdue dish explanation
- **WHEN** a dish is the top-ranked suggestion and has been unused for at least its full typical rotation interval
- **THEN** the reasons list SHALL include "Not had in {N} days"

#### Scenario: Recency penalty explanation
- **WHEN** a dish is the top-ranked suggestion and was served within the last 7 days
- **THEN** the reasons list SHALL include "Served {N} days ago"

#### Scenario: Season match explanation
- **WHEN** a dish is the top-ranked suggestion and its season affinity matches the current season
- **THEN** the reasons list SHALL include "Matches the season"

#### Scenario: Season mismatch explanation
- **WHEN** a dish is the top-ranked suggestion and its season affinity does not match the current season
- **THEN** the reasons list SHALL include "Out of season"

#### Scenario: AllYear or no affinity produces no season reason
- **WHEN** a dish has no season affinity or is marked AllYear
- **THEN** no season-related reason SHALL appear in the list

#### Scenario: Effort match explanation
- **WHEN** a dish is the top-ranked suggestion and its effort level matches the requested effort preference
- **THEN** the reasons list SHALL include "Matches your effort preference"

#### Scenario: Leftover explanation
- **WHEN** a dish is the top-ranked suggestion and earned a leftover bonus (historically served on consecutive days ≥ 30% of the time, and was planned or suggested the day before)
- **THEN** the reasons list SHALL include "Often served as leftovers"

#### Scenario: Neutral rules produce no reason
- **WHEN** a rule contributes zero to the winning candidate's score and has no notable signal
- **THEN** no reason string SHALL be added for that rule

### Requirement: Suggestion response includes reasons
The suggestion response for both single-day and week suggestions SHALL include a `reasons` field containing the ordered list of human-readable explanation strings for the top dish on each slot. An empty list is valid when no rules produce a notable reason.

#### Scenario: Reasons included in single-day response
- **WHEN** a single-day suggestion is returned
- **THEN** the response SHALL include a `reasons` string array alongside `dishId`, `dishName`, `rating`, and `daysSinceLast`

#### Scenario: Reasons included in week suggestion response
- **WHEN** a week suggestion is returned
- **THEN** each day's suggestion object SHALL include a `reasons` string array

#### Scenario: Empty reasons array when no signals are notable
- **WHEN** the top-ranked dish has no scoring signals worth surfacing (all rules return null)
- **THEN** the `reasons` array SHALL be present but empty

### Requirement: Frontend surfaces reasons behind a toggle
The `SuggestedDish` component SHALL display a small info icon that, when activated, reveals the reasons as inline tags beneath the dish name. The toggle SHALL be hidden when the `reasons` array is empty.

#### Scenario: Info icon visible when reasons exist
- **WHEN** a suggestion has one or more reasons
- **THEN** an info icon SHALL be visible next to the reroll button

#### Scenario: Reasons visible after toggle
- **WHEN** a user activates the info icon
- **THEN** the reasons SHALL appear as compact inline tags beneath the dish name

#### Scenario: Toggle hidden when no reasons
- **WHEN** the `reasons` array is empty
- **THEN** no info icon or toggle SHALL be rendered
