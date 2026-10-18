# Dish recommendations delta

## ADDED Requirements

### Requirement: Qualifying grocery offers influence existing recommendations
When the family has selected supermarkets, automatic and conversational recommendations SHALL consider qualifying cached ingredient offers as a bounded positive preference. The influence SHALL increase with ingredient confidence, cost importance, and verified discount significance, and SHALL be capped so offers do not unboundedly replace existing planning signals. Offer context SHALL be available before candidate shortlisting, including each shortlist in a multi-batch request. For conversational requests, explicit constraints and suitability SHALL remain primary; offers SHALL NOT reintroduce excluded or unsuitable dishes. Empty store selection or no qualifying offers SHALL preserve ordinary selection behavior.

#### Scenario: Otherwise equivalent dishes
- **WHEN** two eligible dishes have equivalent ordinary ranking signals and only one has a qualifying significant offer
- **THEN** that dish receives a positive preference

#### Scenario: Match occurs outside the first batch
- **WHEN** a catalog requires several recommendation batches and a later batch contains an offer-relevant dish
- **THEN** that batch and any final comparison receive its offer context before choosing their shortlists
- **AND** offers are not applied only after all shortlists have already been chosen

#### Scenario: Discount conflicts with a request
- **WHEN** the member requests no meat and a meat dish has a strong ingredient offer
- **THEN** the offer does not make the meat dish eligible or loosen the request

### Requirement: Candidate filters precede optional enrichment
The existing excluded-ID, name, and role filters SHALL run before optional ingredient-profile reads or inference. Only candidates passing those filters SHALL consume the shared enrichment budget, and their offer assessment SHALL precede automatic ranking or conversational shortlisting. No eligible candidates or an empty store selection SHALL skip both optional readers. Conversational suitability SHALL remain in the existing provider pipeline.

#### Scenario: Eligible dish appears late in a cold catalog
- **WHEN** earlier uncached dishes fail the request's existing filters and a later dish passes them
- **THEN** the earlier dishes consume no profile reads or inference budget
- **AND** the eligible dish can be assessed within the available enrichment budget before selection

#### Scenario: All candidates are excluded
- **WHEN** no dish passes the existing candidate filters
- **THEN** neither grocery offers nor ingredient profiles are loaded for that request

### Requirement: Equivalent offer opportunities do not compound
Different brands, package listings, or selected stores for the same ingredient SHALL NOT independently multiply a dish's offer boost. The strongest compatible opportunity per ingredient SHALL be used for scoring, and the total contribution of distinct ingredients SHALL be bounded. Unknown discount magnitude and unverified purchase eligibility SHALL NOT be converted into invented savings. Purchase conditions SHALL remain attached to any opportunity that is displayed.

#### Scenario: Many minced-beef listings
- **WHEN** several brands and both selected stores offer compatible minced beef
- **THEN** the dish's minced-beef contribution uses one strongest qualifying opportunity rather than summing every listing

#### Scenario: Conditional saving is not established
- **WHEN** an apparent saving requires an unknown membership or an unsupported multi-buy condition
- **THEN** it does not gain an unconditional recommendation boost

### Requirement: Weekly shopping reasons expose purchase deadlines
Offer relevance SHALL assume a weekly shopping opportunity that remains available at request time. A dish's serving date SHALL NOT be treated as the required purchase date. Recommendations SHALL expose the retailer, ingredient, supported price/discount evidence, observation freshness, ingredient-match evidence kind, known offer conditions, and known purchase deadline separately from ordinary reasons. Offers ending before a planned dinner SHALL remain usable while purchasable and SHALL highlight their buy-before date. Unknown expiry SHALL remain explicit, and the system SHALL NOT imply an offer will remain available in a future planning week.

#### Scenario: Offer expires before serving
- **WHEN** the member requests ideas on Monday for a Saturday dinner and a qualifying offer ends Wednesday
- **THEN** the dish can benefit from the offer
- **AND** the explanation says to buy by Wednesday with its calendar date

#### Scenario: Planning a future week
- **WHEN** a current offer ends before a future selected week
- **THEN** it can be described only as a current buy-before opportunity
- **AND** the recommendation does not claim the offer applies during that future week

#### Scenario: Confident inferred ingredient
- **WHEN** an offer is factual but its relationship to the dish is inferred from the title
- **THEN** the price evidence and the inferred ingredient relationship remain distinguishable
- **AND** no exact per-meal saving is claimed without quantities

#### Scenario: Evidence expires before the promotion
- **WHEN** source observation age reaches 48 hours before the promotional end date
- **THEN** the server-provided exclusive `usableUntil` is that source-age cutoff
- **AND** the known promotional buy-by date remains distinct from the evidence deadline

#### Scenario: Promotional expiry is unknown
- **WHEN** a qualifying offer has no known promotional end date
- **THEN** `usableUntil` is the start of the day after its Danish observation date
- **AND** the promotional expiry remains explicitly unknown

### Requirement: Offer enrichment preserves existing recommendation outcomes
Offer data and inferred profiles SHALL be validated against the authorized family's current evidence and selected stores. Fabricated offer references SHALL NOT be displayed. Optional offer or ingredient enrichment failure SHALL produce ordinary results with an appropriate coverage limitation, rather than masquerading as no suitable dishes. Existing conversational-provider failure, malformed-response, no-match, clarification, and exhaustion outcomes SHALL retain their distinct behavior.

#### Scenario: Fabricated offer reference
- **WHEN** model output cites an offer not supplied for the dish and selected stores
- **THEN** that claim is rejected and is not displayed as verified evidence

#### Scenario: Recommendation provider fails independently
- **WHEN** offer evidence is available but the conversational recommendation provider fails
- **THEN** the existing provider-failure behavior applies rather than reporting that no discounted dish exists

#### Scenario: Stores changed after previous results
- **WHEN** an owner changes the family store selection and a member next requests recommendations
- **THEN** the request uses the latest saved stores
- **AND** existing dinners and the member's active conversational constraints are preserved

#### Scenario: Family settings finish loading after recommendations
- **WHEN** initially unknown family store settings load with the same selection used by the recommendation response
- **THEN** the interface does not report a store-selection change or require another refresh
- **AND** initial settings loading alone does not cancel an in-flight recommendation request
- **AND** a genuinely different loaded selection still requires explicit refresh before requesting more ideas
