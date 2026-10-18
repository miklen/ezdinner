# Family grocery stores

## Purpose

Let a family choose which supermarkets influence its dish recommendations through one simple, owner-managed selection shared by all family members.

## ADDED Requirements

### Requirement: One current store selection per family
The system SHALL persist one set of selected supported supermarket identifiers per family. Supported retailers SHALL be all chains returned by Prej?s `/v1/chains` catalog, including border-trade and flyer-only chains. The list SHALL be refreshed through shared bounded caching; selected identifiers that disappear temporarily SHALL remain visible and removable. Discovery SHALL NOT imply verified offer availability. Owners SHALL be able to select any listed chain even while its offer source is unavailable. The selection SHALL apply to all subsequent recommendation requests by that family's members, independently of selected week. The owner SHALL be able to clear the set to disable offer influence. There SHALL be no per-week or per-member overrides.

#### Scenario: Owner selects the family's supermarkets
- **WHEN** an owner saves Nemlig and REMA 1000 for a family
- **THEN** subsequent recommendations for that family consider qualifying offers from those retailers only
- **AND** navigating to another planning week does not change the selection

#### Scenario: Clear selection
- **WHEN** an owner saves an empty selection
- **THEN** subsequent requests use ordinary recommendations without offer influence
- **AND** saved dinners and existing dish content remain unchanged

#### Scenario: Existing family has no stored selection
- **WHEN** a family has no persisted supermarket selection
- **THEN** it is treated as an empty selection without a migration requirement
- **AND** an owner can enable the supported retailers explicitly

### Requirement: Store updates are owner-only and family-scoped
Only an authenticated owner of the target family SHALL be able to change its store selection. Authorized family members SHALL be able to read the saved selection and use recommendations influenced by it. A caller SHALL NOT read or update another family's selection without access. Ownership SHALL include any current owner, not just the first owner in a list. Unknown identifiers SHALL be rejected without changing the saved selection; duplicate identifiers SHALL NOT create duplicate entries.

#### Scenario: Ordinary member attempts an update
- **WHEN** a non-owner family member submits a store-selection update
- **THEN** the update is rejected and the saved selection remains unchanged

#### Scenario: Second owner updates the selection
- **WHEN** an owner who is not the first listed owner changes the stores
- **THEN** the update succeeds under the same rules as for every other owner

#### Scenario: Unsupported store is submitted
- **WHEN** a caller submits an unsupported retailer identifier
- **THEN** the request is rejected without partially saving the selection

### Requirement: Existing family interface hosts store selection
The family interface SHALL show the current store selection within the existing family presentation. Owners SHALL have compact accessible selection and save controls; other members SHALL see a read-only summary. Saving and failure feedback SHALL preserve the owner's current draft and SHALL NOT report a failed save as successful. Retailer availability SHALL be visible without silently removing an owner's selection. Labels, descriptions, empty states, and feedback SHALL use the selected English or Danish language.

#### Scenario: Save fails
- **WHEN** an owner's save request fails
- **THEN** the selected draft remains available for retry and the persisted summary is not replaced with unsaved values

#### Scenario: Change family or language
- **WHEN** the viewed family or UI language changes
- **THEN** selection and permission controls belong to the displayed family and all interface copy uses the current language
- **AND** a late save response cannot update another family's controls

#### Scenario: Select another Prej chain
- **WHEN** the source catalog includes a chain beyond Nemlig and REMA 1000
- **THEN** an owner can toggle and persist that chain in the existing family settings
- **AND** border-trade, flyer-only, and unavailable-source limitations remain visible
