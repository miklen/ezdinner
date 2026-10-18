# Dish-first week planning delta

## ADDED Requirements

### Requirement: Name selected stores with no usable discounts
The existing recommendation status card SHALL name evaluated selected stores that returned no usable source discounts, distinguishing an unavailable check from a completed check with no qualifying discounts, partial coverage and stale observations. It SHALL explain that offers may still exist in the store's leaflets, app or physical shop. This list SHALL use source results before dish matching; a store with usable source discounts SHALL NOT be labelled empty simply because none appear on the displayed dishes. Loading and outdated-selection states SHALL hide the list. Legacy responses without per-store results SHALL not invent store outcomes. All copy SHALL be localized in English and Danish.

#### Scenario: One store failed and another completed with no discounts
- **WHEN** selected Nemlig is unavailable, Lidl completed with zero usable discounts, and Netto returned usable discounts
- **THEN** the card names Nemlig and Lidl with their distinct reasons and suggests checking manually
- **AND** Netto is not listed as missing discounts

### Requirement: Offer evidence extends existing recommendation presentations
Offer information SHALL appear in the existing recommendation card's reason area, preserving established dish titles, heart ratings, metadata tags, history reasons, planned dates, and action styling. Known purchase deadlines, especially those occurring within the selected planning week, SHALL be visibly identifiable as purchase deadlines using text and not color alone. A recommendation-wide incomplete-coverage message SHALL use the existing recommendation feedback area. There SHALL be no separate grocery-results page, competing dish card, recipe preview, or automatic shopping-list workflow.

#### Scenario: Inspect a discounted-ingredient suggestion
- **WHEN** a member views an offer-influenced recommendation
- **THEN** the existing dish card shows its ingredient, retailer, inference qualification, and known buy-before date alongside existing reasons
- **AND** selecting its dish name opens the existing dish details page

#### Scenario: Offer action and dish navigation
- **WHEN** the member follows an available product-source link or assigns the dish
- **THEN** that action remains separate from dish-detail navigation and retains the existing browsing context

### Requirement: Offer presentation preserves responsive localized continuity
New offer details, unavailable-source feedback, and owner store controls SHALL use the current design system and existing component interactions on desktop and mobile. Long retailer/product names and Danish copy SHALL wrap without horizontal overflow or hiding assignment controls. Dates and monetary values SHALL use the selected language; all new interface text SHALL be available in English and Danish. Displayed opportunities SHALL stop being presented as current at their server-provided exclusive `usableUntil`, including while the page remains visible. Expiry SHALL also be reassessed after a Danish calendar-date rollover or return to the workspace without automatically reordering dishes or fetching offers. Vue SHALL compare supplied instants rather than reproduce domain freshness rules.

#### Scenario: Mobile recommendation with several reasons
- **WHEN** a member views an offer recommendation on a narrow mobile screen
- **THEN** history, offer text, buy-before information, and assignment actions remain readable and usable in the existing card layout

#### Scenario: Change language
- **WHEN** a member switches from English to Danish while recommendations are visible
- **THEN** structured offer labels, deadlines, prices, and coverage feedback use Danish
- **AND** the cards, their order, selected week, request, and browsing context remain intact
- **AND** existing provider prose stays in its original language with localized refresh feedback until explicit refresh, which is required before More can append
- **AND** obsolete requests are cancelled without automatically fetching new recommendations

#### Scenario: Displayed offer expires overnight
- **WHEN** a retained recommendation's offer has expired when the member returns or the Danish date rolls over
- **THEN** it is removed as a current offer opportunity or explicitly marked expired
- **AND** the dish remains available without silently reranking the result list

#### Scenario: Evidence becomes too old while the page stays open
- **WHEN** an offer observed Monday at 14:00 reaches its 48-hour cutoff on Wednesday at 14:00 while the workspace stays visible
- **THEN** it is removed as a current opportunity or explicitly marked no longer current at that deadline even if the promotion ends later
- **AND** the dish order, browsing context, and assignment controls remain intact without a new request

#### Scenario: Recommendation scope is disposed
- **WHEN** the member leaves the workspace or changes family/account
- **THEN** the expiry timeout and obsolete visibility listeners are cleared
- **AND** old callbacks cannot change another scope's results

### Requirement: Retained results keep their evaluated store scope and coverage
Recommendation coverage SHALL identify the canonical store selection actually evaluated by the server. More SHALL append only within the same evaluated selection and existing request scope. A saved-selection change observed locally, or a different selection returned by More, SHALL retain existing cards as stale and require explicit refresh before appending. A later response SHALL NOT erase incomplete or stale coverage for retained results. Successful explicit refresh SHALL replace results and coverage together, preserve conversation/week/target, and clear prior result/dismissal exclusions. Missing coverage in a rollout-compatible response SHALL be treated as unknown, not verified complete.

#### Scenario: Another owner changes stores before More
- **WHEN** retained cards were evaluated for Nemlig and a More response was evaluated for REMA 1000
- **THEN** the new response is not appended to the old result set
- **AND** existing cards remain visible with the stale/refresh affordance until an explicit refresh

#### Scenario: Store settings change while a request is pending
- **WHEN** a saved store-selection change becomes visible locally during a recommendation request
- **THEN** the obsolete request is cancelled and any late response is ignored
- **AND** retained results become stale without losing conversational constraints

#### Scenario: Complete More response follows partial coverage
- **WHEN** retained results have incomplete offer coverage and a same-selection More response has complete coverage
- **THEN** new dishes can append but the combined feedback still discloses the retained limitation
- **AND** only a successful replacement refresh can clear that limitation for the result set
