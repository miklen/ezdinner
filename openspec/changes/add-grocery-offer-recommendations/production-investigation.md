# Production investigation — 2026-10-24

Scope: diagnose repeated unavailable Nemlig coverage and zero usable REMA offers, then implement the user-authorized recovery fixes. Local changes now include normalization/HTTP diagnostics, quota-aware request blocking, pagination cancellation and aligned acquisition deadlines. Offer eligibility and the production cache remain unchanged; nothing has been deployed.

## Evidence

Read the user's Application Insights exports `query_data.csv` (168 requests), `query_data(1).csv` (5 exceptions), and `query_data(2).csv` (traces). The dependencies query was empty. Times below are UTC on 2026-10-24. HTTP trace query strings are redacted by the runtime, so individual page numbers are not observable.

| Time | Observed result |
| --- | --- |
| 15:26:26 | Both REMA and Nemlig acquisitions cancelled by the six-second refresh deadline. Recommendation request returned 200. |
| 15:47:11.663 | REMA successfully published one cache page. |
| 15:47:16.709 | Nemlig acquisition cancelled by the six-second deadline. |
| 15:53:01–07 | Nemlig started 12 offer-page requests; 9 logged HTTP 200 headers before the refresh timed out. No product-batch request was logged. Several page responses took about 3.1–3.2 seconds. |
| 15:56:23.161 | Nemlig retry skipped during the five-minute cooldown; log explicitly says no cached snapshot exists. |
| 18:26:02–08 | Nemlig started 12 offer-page requests. First response returned 200 after 4,986 ms; six subsequent responses returned 429. More queued requests started after the first 429. Final logged exception was a six-second timeout. No product-batch request was logged. |

All five exported recommendation requests returned 200, including the four containing refresh timeout exceptions. This is the implemented optional-source fallback, not proof of healthy grocery acquisition. Traces do not include the first request's acquisition sequence, although its exceptions are present.

## Confirmed Nemlig failure chain

`GrocerySnapshotRefresh.ReadAsync` starts a fixed six-second cancellation budget before claiming refresh ownership. `PrejGroceryOfferSource.AcquireAsync` must fetch the first offer page, every remaining page with four concurrent requests, then product observations in sequential batches of 500, then normalize all rows. Publication must also finish within the refresh budget. `CachedGroceryOfferReader` independently imposes a nine-second total source budget.

Production repeatedly exhausts the inner deadline during offer acquisition, before product observation requests. Without a published snapshot the reader returns Unavailable, rendered as “Kunne ikke tjekke butikken”. Retries after five minutes restart acquisition from page one; partial progress is not retained. A later calendar day does not fix this structural mismatch between the required work and its time budget.

Rate limiting is also confirmed in production. `PrejHttpClient` calls `EnsureSuccessStatusCode` without preserving the structured 429 scope or retry deadline. The page acquisition uses `Task.WhenAll` without cancelling sibling/queued work on failure. Consequently, failed pages release semaphore slots and more queued requests start; the enclosing timeout can win before the combined task reports its earlier HTTP failures. The 18:26 trace demonstrates this masking.

Prej documents distinct daily and burst limits, a structured `scope`, and `Retry-After`/`reset_at` values: <https://prej.dk/api/dokumentation>. The production export does not include those values, so daily quota exhaustion versus burst limiting is not established for those earlier production responses. A subsequent diagnostic returned HTTP 429 with `X-RateLimit-Limit-Day: 25`, `X-RateLimit-Remaining-Day: 0`, and `Retry-After: 19008`. The user confirmed development and production use the same key. The shared daily quota is exhausted, blocking a new raw-source replay until reset. No successful live data download was obtained during this investigation.

## REMA is a separate issue

The 15:47 publication proves a successful REMA acquisition and cache write that day. The supplied UI text corresponds to Partial coverage with zero usable source offers, calculated before dish matching. Ingredient-profile inference and the displayed shortlist therefore cannot explain that zero count.

Normalization excludes missing/non-discount reference prices, multi-buy offers, ambiguous price-row joins, AI-matched product identities, package conflicts, unsupported ingredient names, and invalid fields. Usability subsequently excludes observations older than 48 hours, expired/future offers, and discounts below 5%. The cache stores normalized offers and an aggregate partial flag, but no per-reason rejection counts.

`CachedGroceryOfferReader` gives Partial status precedence over Stale. Thus partial coverage can also conceal expired evidence in the per-store message. A freshly retrieved snapshot does not imply fresh underlying observations.

The user subsequently supplied both REMA cache documents. The head's attempt and published version match, `retrievedAt` is `2026-10-24T15:47:11.6573418Z`, and `retryAfter` is null. Its referenced page contains exactly `{"Offers":[],"IsPartial":true}`. This confirms zero offers survived normalization before persistence; observation-age filtering, promotional expiry at read time, and dish matching are not the cause of this snapshot's zero count.

The source adapter initializes `partial` to false and sets it only from per-row normalization results. Therefore the input was not simply an empty source response: at least one row was processed and rejected as partial. All input rows were excluded, but the boolean does not establish that every row failed for the same reason, or even that every rejection was an error rather than an intentionally unsupported product. Acquisition's product-count checks completed before publication, ruling out its missing-product/incomplete-page failure paths for this successful snapshot.

Every missing-field/type/conversion exception caught by `Normalize` is reduced to `(null, true)` without recording its cause. Explicit ambiguous/unsupported evidence branches can produce the same result. The stored payload cannot distinguish these paths. Raw offer and corresponding product responses, or per-reason normalization diagnostics on the next acquisition, are required to identify the exact rejection cause. Neither loosening evidence checks nor choosing a particular parser defect is justified yet.

Because this result was published as a successful snapshot, it is reused until 05:00 Europe/Copenhagen on October 25. The five-minute recovery applies to failed acquisitions, not this empty partial success. The seven-day page TTL is storage retention, not the refresh interval. Thus refreshing the recommendations that evening cannot recover REMA, even if the source data becomes usable in the meantime.

## Verification and correction scope

41 existing acquisition/reader tests passed in Release. They validate filtering, cache/cooldown behavior, and fallback, but do not establish a complete live Nemlig refresh within production latency/quota limits. The existing verification document already records incomplete live source acceptance before deployment.

Corrections should address acquisition duration and call volume together; increasing the six-second timeout alone still encounters the nine-second outer budget and does not solve 429 handling. Preserve quota-aware retry deadlines, stop queued work on terminal acquisition failure, and retain the actual failure reason. Record per-store raw/normalized/usable counts and exclusion reasons so a successful empty snapshot is diagnosable. Verify a complete live acquisition and later cache reuse for both stores before calling the feature operational.

The observations above establish today's failure mechanism, not a historical account of every previous day.

## Diagnostic patch

`PrejGroceryOfferSource` now emits one structured summary per completed normalization: store, source/normalized counts, partial flag, counts by exclusion reason, and one public product ID per reason. Exceptions also identify the normalization stage, field being read, and exception type. An empty partial result is logged at Warning; other completed results at Information. No raw source content, product names, family content, or credentials are logged. Existing normalization decisions and cache payloads are preserved.

`PrejHttpClient` now logs an unsuccessful HTTP status and the source's retry/daily-quota headers immediately before throwing. This retains evidence of rate limiting even if the surrounding combined acquisition subsequently times out. It logs only the endpoint path, without query parameters or authorization headers. This is diagnostic coverage, not quota-aware backoff or a timeout fix.

Regression tests first failed because these diagnostic entries were absent, then the normalization tests passed alongside existing acquisition/reader checks (49 tests). After adding the HTTP diagnostic case, the full backend unit suite passed: 591 tests, zero failures/skips. The test build includes the Functions project; existing dependency vulnerability and nullable warnings remain. Whitespace checks passed. A future live acquisition remains necessary: today's rejected raw REMA inputs were not retained, and the local API quota prevents acquiring a new sample now. Nothing was committed, pushed, or deployed.

## Authorized recovery implementation

The recovery preserves demand-driven refresh and daily successful snapshot reuse. `PrejRateLimitHandler` checks a key-scoped cooldown before every HTTP call. It honors Retry-After seconds/dates, and zero remaining daily quota blocks until at least next UTC midnight, even if the last response succeeded. A singleton in-process deadline closes the interval before Cosmos persistence finishes. The existing cache container stores a hash-key-scoped nonexpiring rate-limit record; conditional create/replace and reload on conflict ensure a shorter racing block cannot overwrite a longer one. Restarted instances using the same database share the deadline. Separate development/production databases learn exhaustion from provider responses and do not share local quota reservations.

Cooldown observation is logged immediately. Persistence uses a separate bounded write so caller cancellation cannot discard the observed limit; if persistence fails, diagnostics preserve that failure separately while the local block remains active and the original HTTP response is returned. A failed cooldown read prevents an outbound request. No new live requests were made to test these changes.

Pagination now cancels sibling and queued work on its first failure, awaits cleanup and rethrows the original exception. Regression testing found and corrected a semaphore-release race by rechecking cancellation after acquiring a slot and before sending. An eleven-second cold acquisition now completes and publishes. Configurable defaults are 45 seconds per refresh and 60 seconds per source read, including catalog retrieval; ingredient inference receives its own thirty-second budget after source retrieval. Another test exercises acquisition beyond the old thirty-second enrichment deadline.

An independent code review identified the pending-persistence window and the loss of provider errors on persistence failure. Both fixes have regression coverage and were re-reviewed with no remaining correctness findings. Final verification passed all 609 backend unit tests, the integration-test project Release build (zero errors), strict OpenSpec validation and whitespace checks. Existing dependency vulnerability/nullable/obsolete API warnings remain. Real Cosmos tests were not executed, and no live Prej calls were made during the fix. Complete live Nemlig/REMA acquisition remains unverified; the empty REMA snapshot still requires a new source sample or its next normalization diagnostics. Deployment and live verification are separate rollout steps.
