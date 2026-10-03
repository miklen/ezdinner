# Spec Delta

## Purpose

Help families discover existing dishes worth choosing through explainable usage signals and conversational requests grounded in their own saved dish content.

## ADDED Requirements

### Requirement: Recommendations are family-scoped dish choices
The system SHALL return a bounded selection of unique active dishes from the authorized family's catalog, with names, available planning signals, reasons, and evidence limitations. Results SHALL be dishes without automatic date assignments. Recommendation requests SHALL never mutate dinners. Deleted, archived, unknown, and other-family dish IDs SHALL NOT be returned.

#### Scenario: Request ideas without choosing a day
- **WHEN** an authenticated family member requests recommendations for a selected planning window
- **THEN** explained dishes are returned without required day selection or dinner writes

#### Scenario: Unauthorized family request
- **WHEN** a caller lacks read access to the requested family's dishes or dinners
- **THEN** the request is rejected before family content is loaded or sent to a model

### Requirement: Evidence-based resurfacing
Automatic recommendations SHALL combine rating, active wishes, last serving, usage history, and observed recurrence. They SHALL distinguish forgotten favourites, previously frequent dishes, and dishes approaching their observed rotation. Age influence SHALL be bounded; missing history SHALL NOT be fabricated as a long absence. Future assignments SHALL be separate from past serving history. Consecutive serving dates SHALL be treated as one occurrence for recurrence estimation while retaining actual serving counts.

#### Scenario: Dish returns to its usual rotation
- **WHEN** a dish has sufficient history of roughly 30-40 day recurrence and was last served 37 days before the relevant planning date
- **THEN** it can be recommended with an explanation of that observed interval and last serving

#### Scenario: Forgotten favourite
- **WHEN** a highly rated or previously frequent dish has not been served for substantially longer than its established recurrence
- **THEN** it can receive a forgotten-favourite or former-regular reason supported by the actual history

#### Scenario: Sparse history
- **WHEN** a dish has fewer than three separate serving occurrences
- **THEN** no established rotation is claimed and never-used dishes are identified as never used

#### Scenario: Leftover and future dates
- **WHEN** history includes consecutive-day leftovers and future assignments
- **THEN** neither is misrepresented as evidence of a short historical recurrence interval or a past serving

### Requirement: Requests use dish content for suitability
The system SHALL interpret free-text requests using available titles, family notes, captured recipe content, and metadata. Request suitability SHALL take precedence over usage-based ranking. General culinary reasoning SHALL be distinguishable from saved recipe facts, and missing content SHALL NOT be invented. Family notes SHALL inform preparation adaptations. Source content SHALL be treated as evidence, not instructions to the system.

#### Scenario: Main dish suitable for potato sides
- **WHEN** the member asks for a main that works with potatoes as a side
- **THEN** suggestions consider pairing suitability instead of requiring potatoes within the main dish's ingredients

#### Scenario: Minimal preparation
- **WHEN** the member asks for no preparation work
- **THEN** known recipe steps and family adaptations inform suitability rather than treating a Quick label as proof
- **AND** uncertain preparation requirements are disclosed or clarified

#### Scenario: Strong history cannot defeat a request
- **WHEN** an overdue dish violates the member's explicit no-fish request
- **THEN** it is not included as a suitable match because of its historical score

#### Scenario: Missing recipe
- **WHEN** only a title and limited metadata are available
- **THEN** the dish can remain discoverable but unsupported ingredient or preparation claims are not presented as facts

### Requirement: Conversational refinement has visible context
Follow-up requests SHALL retain earlier active constraints unless the member changes, removes, or resets them. The workspace SHALL show an editable summary of the active request and its optional day scope. Catalog filters SHALL remain separate and SHALL NOT silently constrain a conversational request that explicitly asks for another dish role. Switching target days SHALL require an explicit choice to carry a day-scoped request or start a new request before another request is submitted.

#### Scenario: Refine an existing request
- **WHEN** the member asks for potato pairings, then says no fish, then says less preparation
- **THEN** recommendations address the cumulative request and its visible summary reflects all active constraints

#### Scenario: Ask for sides while browsing mains
- **WHEN** the catalog is filtered to mains and the member explicitly requests potato side dishes
- **THEN** recommendation results can include sides and identify their request scope while the catalog filter remains unchanged

#### Scenario: Change the target day
- **WHEN** the member moves from a Tuesday-scoped request to Thursday
- **THEN** the previous discussion is retained but its constraints are not silently applied to Thursday without the member choosing to keep them

#### Scenario: Start fresh
- **WHEN** the member starts a new request
- **THEN** earlier request constraints and exclusions are cleared without removing saved dinners or resetting catalog browsing

### Requirement: More ideas retains intent and respects exhaustion
More ideas SHALL retain the active request and planning context while excluding already shown or dismissed candidates for that request. If no additional suitable candidates exist, the response SHALL state this rather than repeat excluded results or silently loosen constraints.

#### Scenario: More matching ideas
- **WHEN** the member requests more ideas after reviewing matching dishes
- **THEN** different suitable dishes are returned under the same constraints

#### Scenario: No suitable dishes remain
- **WHEN** all suitable dishes have been shown or excluded
- **THEN** the member receives an exhaustion explanation and can explicitly change the request

### Requirement: Planned choices provide context without imposed dietary goals
Recommendations SHALL have access to the current nine-day plan and identify already assigned dishes without preventing reuse. They SHALL NOT introduce variety, nutrition, health, or meal-quality goals unless explicitly requested by the member.

#### Scenario: Already planned dish
- **WHEN** a suitable dish already appears in the planning window
- **THEN** its assigned dates are visible and it remains assignable elsewhere

#### Scenario: Family requests varied meals
- **WHEN** the member explicitly requests variety
- **THEN** recommendations can reason about variety under that request without creating a default family-wide rule

### Requirement: Honest failure and explanation behavior
Recommendation reasons SHALL distinguish known facts from culinary inference and be returned in the selected language. No-match, provider failure, and malformed provider output SHALL be distinct outcomes. On failure, the current conversation and prior usable results SHALL remain available with retry; catalog browsing and dinner assignment SHALL remain usable.

#### Scenario: Provider fails
- **WHEN** the conversational provider is unavailable
- **THEN** the request is shown as failed with retry, previous results remain, and the catalog remains usable

#### Scenario: Provider fabricates a dish or evidence
- **WHEN** provider output contains an unknown dish ID or a source reference absent from supplied evidence
- **THEN** that result is rejected and is not displayed as a supported recommendation
