# Grocery offer cache

## Purpose

Provide reusable, timely supermarket offer evidence without repeated retailer lookups for every family or recommendation request.

## ADDED Requirements

### Requirement: Offer evidence comes from verified retailer sources
Each enabled retailer SHALL have a verified usable acquisition source supporting the application's intended daily caching. Normalized observations SHALL identify the retailer, product and ingredient category, current price and comparable price basis, source reference, observation time, available start/end dates, and known purchase or availability conditions. A discount percentage or saving SHALL be claimed only when supported by a comparable reference price or explicit source discount. An advertisement flag alone SHALL NOT establish a numerical discount. Missing dates and unavailable stock information SHALL remain explicitly unknown.

#### Scenario: Advertised item has no verified saving
- **WHEN** the source marks an item as advertised but supplies no comparable reference price or explicit saving
- **THEN** the system does not invent a discount percentage or use a fabricated saving to boost dishes

#### Scenario: Retailer cannot be reliably integrated
- **WHEN** no usable acquisition source has been verified for an initial retailer
- **THEN** its offers are reported as unavailable and are not fabricated
- **AND** the full two-retailer feature is not considered complete without working coverage or an explicit reduction of scope

### Requirement: Shared cache reuses daily successes and allows bounded failure recovery
Public offer observations SHALL be cached persistently across application restarts and shared across families only within the same verified retailer/applicability scope. Successful snapshots, including successful empty responses, SHALL be reused until the next 05:00 Europe/Copenhagen boundary. Failed, cancelled, timed-out or abandoned attempts SHALL become eligible for a demand-triggered retry five minutes after the attempt was claimed, including before 05:00. Initial acquisition SHALL be allowed when no snapshot exists. The persisted cooldown and conditional claim SHALL prevent concurrent instances, restarts, or a new daily boundary from bypassing an active attempt. Retrieval SHALL be bounded and SHALL NOT require fetching full inventories or searching per dish on each request. A failed or incomplete refresh SHALL NOT replace the last complete snapshot with partial results.

#### Scenario: Multiple families request recommendations
- **WHEN** several families request recommendations using the same retailer and applicability scope on the same day
- **THEN** they reuse the shared observations and do not independently trigger repeated retailer refreshes

#### Scenario: Refresh fails halfway through pagination
- **WHEN** a refresh cannot obtain a complete validated response within its limits
- **THEN** the previous complete snapshot remains available subject to freshness rules
- **AND** the failure is distinguishable from a successful refresh containing no offers

#### Scenario: Application restarts after a failed attempt
- **WHEN** another instance handles a request before the five-minute cooldown expires
- **THEN** it honors the recorded cooldown and does not call the retailer again
- **AND** a later request after the cooldown may claim a retry on the same day
- **AND** only the winning claim can publish a complete snapshot

#### Scenario: A successful check contains no offers
- **WHEN** the source returns a complete validated response containing no qualifying offers
- **THEN** it is reused until the next daily boundary instead of being treated as a retryable failure

#### Scenario: Upgrade with an earlier failed attempt
- **WHEN** a persisted head has no published version for its latest attempt and no cooldown field
- **THEN** the next request may claim recovery without a migration or manual cache deletion

### Requirement: Acquisition respects the shared provider quota and aligned deadlines
Every outbound Prej request SHALL honor a persisted cooldown scoped by a non-secret identifier of its API key. HTTP 429 SHALL defer requests until the provider's usable Retry-After deadline. Zero remaining daily quota, including on a successful final request, SHALL block subsequent requests until at least the next UTC midnight. A 429 without a usable deadline SHALL conservatively defer until the next UTC midnight. Concurrent updates SHALL NOT shorten an existing block. The block SHALL survive application restart and apply to all store/catalog requests using that key within the configured cache database. Separate environments sharing a key SHALL consume the same provider quota; separate cache databases SHALL NOT be represented as sharing request reservations.

Acquisition SHALL use a configurable refresh deadline nested within a longer total source-read deadline. Ingredient inference SHALL receive its own bounded deadline after acquisition. A failed page SHALL cancel in-flight and queued pagination without replacing the original failure with sibling cancellation, and incomplete results SHALL NOT be published.

#### Scenario: Exhausted key is reused for another store after restart
- **WHEN** a response reports zero remaining daily requests and the application restarts
- **THEN** a recommendation request for another store using the same key and cache database does not call Prej before the persisted reset
- **AND** existing cached evidence remains subject to its ordinary freshness rules

#### Scenario: Burst limit has an earlier reset
- **WHEN** Prej returns 429 with remaining daily calls and a usable Retry-After value
- **THEN** requests are deferred until that deadline, accepting either seconds or an HTTP date
- **AND** the response does not force an unnecessary wait until the next day

#### Scenario: A page fails while other pages are queued
- **WHEN** one paginated request fails
- **THEN** in-flight page requests are cancelled and queued requests do not start
- **AND** acquisition reports the first failure and retains the previous complete snapshot

#### Scenario: Valid acquisition takes longer than the previous six-second deadline
- **WHEN** a complete validated snapshot arrives within the configured refresh and total source-read deadlines
- **THEN** it can be published and used without an earlier unrelated ingredient-inference deadline cancelling it

### Requirement: Freshness and promotional validity are independent
The system SHALL assess both the source observation's age and the promotional period before allowing an offer boost. Fetching an old source observation SHALL NOT make it fresh. Known end dates SHALL be inclusive Danish calendar dates. Future-start and expired offers SHALL NOT boost current shopping recommendations. A dated observation SHALL contribute for no more than 48 hours from its source observation time, and retained observations after a failed refresh SHALL be marked as potentially outdated. An offer without a known end date SHALL contribute only on its source observation's Danish calendar day and SHALL be labeled as having unknown expiry. The pure domain assessment SHALL derive an exclusive `usableUntil` as the earlier of observation plus 48 hours and the start of the Danish day after the promotional end; for unknown expiry, that day boundary SHALL instead follow the observation date. Returned opportunities SHALL expose this instant without presenting it as a known promotional end. At or after the cutoff an opportunity SHALL NOT contribute. Danish boundaries SHALL respect Europe/Copenhagen daylight-saving rules.

#### Scenario: Discount ends today
- **WHEN** an otherwise usable offer ends on today's Danish date
- **THEN** it can contribute today and is excluded after that date ends

#### Scenario: Unknown expiry is carried into tomorrow
- **WHEN** an offer with no end date was last observed yesterday
- **THEN** it no longer contributes even if its cached response can still be read

#### Scenario: Source returns stale content successfully
- **WHEN** an HTTP fetch succeeds but the underlying observation exceeds the allowed age
- **THEN** that observation is excluded from scoring rather than assigned the retrieval time as a new observation time

#### Scenario: Promotional deadline occurs before the source-age cutoff
- **WHEN** the start of the Danish day after an offer's inclusive end occurs before observation plus 48 hours
- **THEN** `usableUntil` is that Danish day boundary converted to a UTC instant
- **AND** eligibility ends exactly at that instant, including across daylight-saving transitions

### Requirement: Optional source failure preserves ordinary suggestions
Failure, unavailability, malformed observations, or lack of fresh offers from one retailer SHALL NOT prevent ordinary dish recommendations or usable evidence from another selected retailer. Results SHALL distinguish unavailable or partial offer coverage from a successful search with no qualifying offers. Third-party grocery sources SHALL receive no family recipe, notes, member identity, or dish catalog content.

#### Scenario: One selected retailer is unavailable
- **WHEN** Nemlig is unavailable and REMA has usable offers
- **THEN** recommendations can use REMA's offers, disclose incomplete coverage, and remain usable

#### Scenario: Both selected retailers fail
- **WHEN** neither selected retailer supplies usable observations
- **THEN** ordinary recommendations remain available with a localized offer-availability limitation
