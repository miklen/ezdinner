# Design

Configuration update (2026-10-18): at the user's request, there is no terms-confirmation runtime flag. Offer acquisition is controlled by `GroceryRecommendations.Enabled` and the API key. Catalog refresh retains its separate switch; family selections determine the requested chains.

## Context

See [proposal.md](proposal.md) for motivation and scope. This is an additive extension to Plan your week's existing recommendation path, not a replacement planner.

Observed integration points:

- `DishRecommendationContextAssembler.BuildEvidence` already supplies each authorized dish's title, family notes, captured recipe, and metadata. Recipe snapshots contain saved text, not a persistent structured ingredient catalog.
- `DishRecommendationQuery` has an automatic full-catalog scoring branch and a conversational branch with existing evidence batching, at most three concurrent provider calls, six-result provider shortlists, and a possible finalist comparison. Conversational responses already distinguish `Fact` and `Inference` and validate dish/source references.
- `ResurfacingRule` uses a tens-of-points score: rating contributes up to +/-20, wishes up to 22, and recent use can subtract 20. Conversational suitability uses a separate 0-to-1 scale.
- `Family` has no store preference. `FamilyRepository.SaveAsync` currently replaces whole documents, so a new preference-only writer must also account for older family writers overwriting its changes.
- `web/pages/families.vue` already has family cards, owner checks using `some`, and member actions. `FamilyRepository` and `useFamiliesStore` already load those families.
- `PlanningDishCard` already wraps `DishCard`, supplies its `additional` slot for reasons, and separates the dish-name link from assignment/dismiss actions. `RecommendationExplorer` already owns status messages and the grid. `useDishRecommendations` already handles request cancellation, stale results, exclusions, family/account changes, and language changes. Its Zod contracts already validate API results.
- The current brand in `web/assets/global.scss` is green (`--color-primary: #6FAF7A`, dark `#3A6B4A`) with warm surfaces, DM Serif Display headings, and DM Sans body text. Reuse tokens, not old palette values quoted elsewhere.
- The existing localhost browser tab returned `ERR_CONNECTION_REFUSED` during planning. Source inspection establishes reuse targets; it is not visual acceptance evidence.

## Goals / Non-Goals

**Goals:** Preserve the current planning loop, isolate deterministic offer decisions from external retrieval, make optional enrichment reusable and cheap after its first computation, and add only the contracts needed by the agreed behavior.

**Non-Goals:** No new planner, ingredient-management screen, full inventory mirror, shopping list, retailer account linking, order placement, shopping-date settings, weekly overrides, confidence editor, dietary optimizer, generic scraping framework, or restructure of existing features. Legacy day/week suggestions retain their present behavior. Do not modify recipe imports or metadata confirmation semantics just to compute recommendation evidence.

## Decisions

Ingredient interpretation update: at the user's request, ingredient translation and natural-language source-precedence interpretation belong to the existing inference model. The canonical vocabulary contains only supported categories and compatibility, without language aliases. The inference adapter checks response structure, current dish/source references, verbatim factual excerpts and bounded weights; it does not claim that these checks independently establish semantic correctness. The model receives explicit multilingual normalization, negation/substitution and family-note precedence instructions. Profile and vocabulary versions invalidate earlier profiles after removing the lexical validator. This adds no second model call or per-product AI classification.

### 1. Gate retailer enablement on acquisition evidence

Implement one narrow `IGroceryOfferSource` boundary, with adapters only for verified sources. Source/provider identity is separate from stable retailer IDs `nemlig` and `rema1000`; one authorized provider may cover both without two duplicate clients. All Prej catalog chains are selectable through one adapter. Per-chain verification gates offer acquisition independently of selection; no plugin platform is introduced.

The first implementation task must establish, for each initial retailer: retrievable representative grocery offers; reference-price/discount meaning; date semantics; purchase restrictions; chain versus location scope; freshness fields; bounded pagination; daily cache permission; operational quotas/cost; and source links. Keep a small sanitized fixture and record the selected source and evidence here. Do not count a search-engine excerpt or documentation alone as a successful integration test.

Research on 2026-10-18: direct Nemlig access redirected to Queue-it and direct REMA shop access returned 403. [REMA's public shop](https://shop.rema1000.dk/?filters=avisvare) exposes campaign periods and restrictions in indexed content, but ingestion remains unverified. [Prej's data model](https://prej.dk/api/data) documents dated offers, reference pricing, and nightly observations. Its [coverage table](https://prej.dk/api/daekning) separately counts leaflet coverage, which must not be confused with complete promotion coverage. [API terms](https://prej.dk/vilkaar-api) restrict whole-dataset mirroring and require attribution for public use. Prej is a candidate only; neither an account nor subscription is authorized by this proposal.

This gate has a firm outcome: enable a verified adapter or leave the affected retailer unavailable and report the blocker. Do not ship fictional coverage or silently reduce the requested two-retailer scope. A verified source can be chosen behind the same boundary without changing the feature contract; any need for family addresses/login or another product setting requires revisiting scope.

**Alternative rejected:** committing to retailer-specific scrapers before proving acquisition, or making live retailer searches part of every suggestion.

#### Task 1.1 verification attempt — 2026-10-18

Initial attempt: no source credentials were available. The authenticated follow-up below supersedes that access blocker; retailer enablement remains pending the listed source-contract questions.

- [Prej authentication and pagination](https://prej.dk/api/dokumentation): data endpoints require a Bearer key; lists support up to 100 rows per page and return totals. No matching grocery-source credential names were found in the current process environment or Function local settings. Provider edge caching documentation does not establish permission for this application's persistent daily snapshots.
- [Prej data semantics](https://prej.dk/api/data): prices are integer øre; observations use `last_seen_date`, with response-level `as_of`. End dates are inclusive Danish dates; Nemlig and REMA start dates may be missing. Chain prices can represent the lowest observed location price. Single-item prices and multi-buy terms are separate. These claims still need representative payload verification, including comparable reference prices and purchase restrictions.
- [Prej coverage](https://prej.dk/api/daekning): both retailers have catalog entries; its leaflet-offer column is not proof of qualifying promotion coverage.
- [Prej plans](https://prej.dk/api): evaluation access is free with 25 calls/day; Start is 750 DKK/month with 2,500 calls/day. The published burst limit is 120 calls/minute. No account or subscription was created. Actual offer counts must determine whether complete bounded refreshes fit the chosen quota and timeout.
- [Prej terms](https://prej.dk/vilkaar-api): public use requires attribution, commercial use requires a paid plan, and whole-dataset mirroring is prohibited. Permission for the proposed bounded daily offer cache remains to be established before enablement.
- Direct Nemlig browsing redirected to Queue-it; no offer payload was obtained. [REMA's shop](https://shop.rema1000.dk/?filters=avisvare) exposed campaign dates and quantity/location restrictions through web browsing, but this does not verify a permitted automated acquisition contract or reference-price semantics.

#### Authenticated evaluation follow-up — 2026-10-18

The user supplied evaluation access and clarified that the 25-call plan is for trying the API, not ongoing integration. Six authenticated HTTP 200 responses were captured between 12:09 and 12:11 UTC; the final response reports 19/25 calls remaining. The key was entered through a masked prompt and was not persisted. An earlier Python-client request returned 403; its cause was not established. The successful requests used PowerShell's HTTP client. No family content was transmitted, no full catalog was retrieved, and no subscription was purchased.

Sanitized response fixtures and acquisition paths are in [fixtures](fixtures/README.md). Nemlig reports 965 dated offers overall and 77 in meat/fish; REMA reports 188 overall and 22 in meat/fish. These are endpoint totals, not counts of validated usable opportunities. The samples verify live prices, reference prices, nullable starts, inclusive end fields, and pagination for both requested chains; they do not establish complete usable coverage.

| Contract concern | Evidence and mapping decision | Remaining verification |
| --- | --- | --- |
| Discount | Use integer `price` and comparable `normal_price`; calculate the fraction instead of using rounded `discount_pct` at thresholds. | Reject absent/nonpositive reference prices and inconsistent quantity bases. |
| Source observation | Offer rows lack `last_seen_date`. A bounded product batch supplied it for both sampled retailers. Join by product, chain, price, quantity, unit, and offer window; use the Danish start of that observation date. | Missing or inconsistent joins cannot become fresh using response `as_of` or retrieval time. |
| Identity and preparation | Product 17590 has structured quantity 650 g but description 750 g and `ai_matched=true`. REMA samples include both raw meat and prepared products. | Do not infer exact package equivalence or raw-meat compatibility from shared keywords; ambiguous evidence must be rejected. |
| Dates | Sample starts are null; ends are 2026-10-18 for Nemlig and 2026-10-23 for REMA. | `/v1/offers` covers dated offers only. Undated multi-buy appears on product rows and must not be misreported as covered. |
| Purchase conditions | `multi_buy` is present and null in all captured offers. | The inspected Offer/Price schemas expose no membership, quantity-cap, or local-availability restriction fields. Absence is not verified unconditional eligibility; source confirmation is needed before enablement. |
| Source links | Samples contain image/logo URLs, not retailer product-page URLs. | Do not turn an image URL into a product source link; attribution and an optional source link are separate. |
| Completeness and bounds | At 100 rows/page, current totals imply 10 Nemlig plus 2 REMA offer requests, before observation batches. A two-product batch verified the freshness join. | No complete snapshot or six-second refresh was tested. Sequential sample calls took roughly 1.4 seconds each; concurrency, shared quota, and timeout must be validated. |
| Empty/unavailable/conditional cases | Successful nonempty pages and distinct REMA page 2 were captured. | Successful empty results, absent dates/reference prices, real conditional offers, and failure recovery are not live-verified; synthetic test cases must be labelled separately. |

The [machine-readable guide](https://api.prej.app/llms.txt) and [OpenAPI contract](https://api.prej.app/openapi.json) were read directly. Source semantics above combine those contracts with the captured responses. Ongoing use still needs a suitable access plan and verification that the bounded daily offer cache is permitted; missing purchase restrictions must be resolved without silently weakening the spec. Tasks 1.1 and 1.2 remain unchecked. Implementation may resume after these questions are resolved or the user explicitly authorizes fixture-based implementation with retailer enablement deferred.

### 2. Extend the existing Family aggregate, with one simple setting

Add `GroceryStoreSelectionValueObject` under `Aggregates/FamilyAggregate`, holding a validated, deduplicated immutable selection. `Family.SetGroceryStores` replaces it and updates `UpdatedDate`. Missing persisted properties and new-family defaults resolve to empty; selecting the two retailers is an owner action, not a forced assumption for all tenants. Empty means offer influence is disabled.

Reuse Family read models/mappings (`FamilyDetails`, HTTP family DTOs, `Family` frontend type), existing family loading, and `Family/Read` access. Return a small supported-store descriptor list with the family settings projection, including stable ID and configured source availability; this is a read DTO, not a new domain aggregate. The list comes from the shared Prej chain catalog and includes every returned chain, with border-trade and flyer-only flags. A checked-in verified catalog snapshot bootstraps settings while live discovery is disabled; discovery refreshes from the API when configured. Selected missing chains remain removable and unavailable.

Add `PUT /api/families/{familyId}/grocery-stores` with `{ storeIds: [...] }`. Use the existing authenticated Function -> Application command -> Family domain method -> persistence pattern. Reuse `Resources.Family` / `Actions.Update`, which already restricts this action to owners. Reject unknown IDs atomically; normalize duplicates; return the canonical saved selection. Preserve existing local 401 behavior for authenticated-but-unauthorized Functions and return 400 for invalid input, 404 for a missing family. No new Casbin role or policy resource is needed.

Protect the preference from unrelated family updates in **both directions**. Persist preference changes as a focused property update. Change existing-record saves in `FamilyRepository` to update their existing mutable fields without replacing `groceryStores`; retain create-if-absent behavior for new families. Only the store command writes this field. This avoids an invite/role/merge operation that loaded an older Family overwriting newly saved stores. Do not turn this into a general family-concurrency rewrite. Concurrent store saves use last completed write wins; unrelated member fields are preserved atomically. Add the reverse-race acceptance test explicitly.

**Alternative rejected:** a separate family-preferences aggregate, a new settings route, or week-specific records.

### 3. Keep domain decisions pure and cache records disposable

Use the existing layer boundaries:

| Layer | Responsibility |
| --- | --- |
| Core / FamilyAggregate | Validate the selected retailer set through the Family root |
| Core / DomainServices/DishRecommendations | Immutable ingredient/offer/match value objects, a small canonical ingredient vocabulary, and one pure `GroceryOfferBoostRule` |
| Query.Core / DishRecommendationQueries | Assemble authorized family evidence/settings, apply existing candidate filters, then load optional offers/profiles for eligible candidates and invoke the rule before existing selection |
| Application | Owner's store-selection command only; no retailer HTTP or scoring logic |
| Infrastructure | Source HTTP mapping, Anthropic ingredient inference, persistent operational caches, bounded refresh and distributed coordination |
| Functions | Authentication/authorization, bounded input validation, command/query call, DTO mapping |

Use `DishIngredientMatchValueObject`, `GroceryOfferValueObject`, and `DishGroceryOfferMatchValueObject` for validated domain values. A profile's content, confidence, and ingredient role have meaning; do not wrap every primitive with a class merely to increase type count. Cache storage models, provider responses, and query DTOs remain outside Core. Offer snapshots and profile fingerprints are derived read/cache material, not new aggregate roots or domain repositories.

Two query-facing reader ports are sufficient: `IGroceryOfferReader` and `IDishIngredientProfileReader`. Infrastructure encapsulates read-through operational cache maintenance behind them. Filling these disposable caches does not mutate Family, Dish, Dinner, or other business aggregates; Query.Core never coordinates persistence/leases itself. Avoid a new business command/refresh workflow for every cache miss. Existing Cosmos and Anthropic SDK registrations are reused. Require existing Family read permission before loading its settings in the recommendation endpoint, alongside its current Dish/Dinner/Wishlist checks. An empty selection skips both readers; no qualifying offers skips ingredient inference.

Preserve the query's order: assemble authorized evidence/settings -> apply existing excluded-ID, name, and role filters -> enrich only that eligible candidate set -> automatic ranking or existing semantic batching. Do not perform profile loading/inference inside the full-catalog context assembler. No eligible candidates skips both optional readers. Apply conversational suitability through the existing provider path; do not add a second semantic eligibility pass. The shared enrichment deadline starts with optional reader work, and excluded/nonmatching dishes consume none of its profile budget.

### 4. Infer a small reusable primary-ingredient profile

Use existing evidence references, with precedence: explicit family adaptations -> saved recipe -> explicit title -> general culinary inference. Contradiction or missing culinary identity yields no qualifying match. Notes adapting a recipe override that recipe. Keep general minced meat separate from beef/pork and raw ingredients separate from prepared products.

The ingredient-profile inference adapter resolves this source precedence while producing and validating the profile. `GroceryOfferBoostRule` consumes the resulting validated ingredient values; it does not interpret natural-language notes or recipes. Test source interpretation in adapter fixtures and compatibility/scoring with already-resolved profiles in Core.

Use a small code-owned canonical vocabulary of common cost-relevant ingredient categories and aliases, including parent/specific relationships. The inference adapter selects validated keys from it; it cannot invent new identifiers. Return at most five primary ingredient entries per dish, each with importance in [0,1], finite confidence in [0,1], evidence kind/references, and specificity. Minor pantry roles, including incidental salt, have zero importance and cannot be promoted by confidence. A broad category matched to a specific product reduces confidence; it never turns into a confirmed species claim. The vocabulary is deliberately bounded, not a public ingredient catalog or exhaustive culinary ontology.

Reuse the existing Anthropic client and evidence serialization/validation patterns, but a focused ingredient-profile response is necessary: the current metadata-enrichment response cannot represent ingredients. Do not change its 512-token metadata contract or write inferred profiles into dish notes/recipes. Saved explicit claims require an actual supporting source reference and excerpt; unsupported explicit claims are rejected or remain inference. Treat all source content as untrusted evidence.

Cache each profile by `(familyId, dishId, evidenceHash, profileVersion)`. Hash ingredient-relevant title/notes/recipe/metadata, not ratings, serving history, locale, or store selection. Never reuse a prior hash after content changes, even if current inference fails. Keep profile entries language-independent; localize user-facing labels separately. Successful profiles have a fixed 30-day cleanup TTL from publication; cache hits do not write or extend retention. Failure markers last 15 minutes. The version includes inference/model/prompt and vocabulary versions.

On demand, load cached profiles first and batch misses in groups of at most ten dishes and 40,000 evidence characters; one larger valid dish is processed alone. At most three calls run concurrently with a shared 30-second optional-enrichment budget. Unprocessed/failed profiles mean no offer boost for those dishes and a `partial` coverage status; normal recommendation evaluation still sees all eligible dishes. Cache completed valid profiles for subsequent requests. No background backfill or per-request reinference of already cached dishes. This bounds cold-catalog cost without pretending all dishes were checked.

### 5. Persist daily offer snapshots and coordinate refresh without a timer

Use one operational Cosmos container, `GroceryRecommendationCache`, partitioned by `/scopeKey`, with typed documents for retailer snapshot metadata/pages and family/dish profiles. `retailer:<id>:<verified-scope>` keys may be shared; `family:<id>` profile partitions never are. Provision through the existing container setup pattern. Use bounded pages/version pointers rather than a single unbounded inventory document. Old versions and profiles use TTL cleanup. Operational cache DTOs use the existing serializer deliberately; verify date and enum round trips.

Store source observation time separately from retrieval time, nullable inclusive local start/end dates, product and unit-price basis, verified reference price or supplied saving, stable source/retailer IDs, validated HTTPS product link when available, purchase conditions, and source applicability. Catalog/product normalization uses explicit source fields and a small alias map; ambiguous products receive no match. Do not run a separate AI classification call for every grocery product. Omit unsupported multi-buy/member-only/location-specific savings from scoring; preserve usable conditions and availability limitations for displayed opportunities. Chain-level observations must be labeled as such rather than presented as confirmed local stock.

First due request performs a bounded refresh; other requests use the last eligible complete snapshot or ordinary suggestions. Successful snapshots are reused until the next 05:00 Europe/Copenhagen boundary. At the user's request (2026-10-18), unsuccessful attempts no longer consume the whole day: persist a five-minute `retryAfter` with the attempt claim before outbound I/O. Use Cosmos create-if-absent/ETag coordination so concurrent requests and restarts share that cooldown, even across a daily boundary. A later request may retry an unpublished attempt after the cooldown, including before 05:00; a missing initial snapshot can also be acquired then. AttemptDay identifies the refresh period starting at 05:00. Publication sets Version equal to AttemptId and clears the cooldown, including for successful empty results. Legacy unpublished heads without retryAfter can recover immediately. No fire-and-forget task and no TimerTrigger. A snapshot is published only after all required pages validate. Fence publication by ETag so an older attempt cannot supersede a newer retry.

Initial operational bounds: two retailer refreshes concurrently, six seconds total per scope, at most 50 pages/5,000 normalized grocery offers per scope, 250 KB serialized cache pages, 48-hour source-age ceiling for known-dated offers, and at most today's observations for unknown expiry. Exceeding bounds is unavailable/partial source coverage, not a successful truncated snapshot. Profile and retailer work share a 30-second optional-enrichment request budget. Completed valid profiles can be reused and remaining profile misses processed on later requests; an interrupted retailer refresh can be retried by a later request after its persisted five-minute cooldown, without an automatic retry loop.

If a source gives only an observation date, use that Danish date's start as the conservative observation instant. Never reset observation age on a successful refetch. A previous dated snapshot within 48 hours may still contribute with a stale warning after a failed/due refresh; unknown expiry can contribute only on the source observation date. Expired/future-start offers always score zero. A source status of `unavailable`, `partial`, `fresh`, `stale`, or `disabled` is operational coverage, separate from recommendation outcomes. Successful empty offers are not failures.

**Alternative rejected:** per-instance memory as the only cache, maintaining a full product inventory, per-family duplicate public snapshots, or a recurring refresh that prevents scale-to-zero.

### 6. Apply one bounded boost in the existing selection paths

The pure rule receives current time/date, validated offers, and ingredient values. Require primary cost relevance, effective match confidence >=0.60, and a verified discount fraction >=0.05. Reject incompatible products, unknown saving magnitude, disallowed conditions, observations beyond the freshness ceiling, expired offers, and minor ingredients. Source-provided percentage alone can establish a fraction; otherwise compute it from comparable normal/offer prices for the same product/quantity basis. Package savings are not meal savings.

For each currently eligible opportunity, the same pure domain assessment derives an exclusive `usableUntil` instant: the earlier of source observation plus 48 hours and the start of the Danish day after its inclusive promotional end date. If the end date is unknown, use the start of the day after the Danish observation date instead of a promotional deadline. Resolve Danish day boundaries with Europe/Copenhagen rules, including DST. At or after `usableUntil` the opportunity is unusable; the API exposes this instant so Vue need not reproduce the eligibility calculation. This evidence deadline does not turn an unknown promotional end into a known buy-by date.

Initial designer defaults, not learned preferences or calibrated probabilities:

```text
ingredientPoints = 10 * effectiveConfidence * costImportance
                     * min(discountFraction / 0.20, 1)
offerPoints      = min(10, sum(topTwoDistinctIngredientPoints))
semanticBonus    = 0.05 * offerPoints / 10
```

Effective confidence includes any specificity reduction. Select the strongest compatible offer per canonical ingredient before taking the top two, including overlap between a parent and its specific ingredient; do not count one meat opportunity twice. Deterministic ties use retailer/product identity. Thus 10% off an exact primary minced-beef match yields five points, while incidental salt yields zero regardless of its discount. Constants are backend defaults, not new owner-facing controls.

- **Automatic:** extend `DishRecommendationService` to accept per-dish offer assessments; combine resurfacing plus offer points before its existing `Take`. All existing eligible candidates are scored. Preserve stable name/ID ties and historical reasons separately from offer reasons.
- **Conversational:** include computed offer facts and the normalized bonus in each dish's existing evidence **before** every batch and finalist call. The prompt selects suitable shortlists with this preference but reports `Suitability` for request fit only, excluding the bonus. Query.Core orders returned matches by `Suitability + semanticBonus`, then existing resurfacing ties, then stable identity, before the final limit. The server computes the bonus from supplied facts once; never accept an AI-generated bonus, price, date, or link. Reuse batching, cancellations, constraints, roles, exclusions, six-result limits, and finalist comparison.
- **Boundary:** an offer never makes a provider-rejected or explicitly incompatible dish eligible. A maximum .05 adjustment favors close suitable matches, not arbitrary cheap dishes. Selection remains the existing heuristic LLM assessment; six-per-batch shortlists do not guarantee mathematically exhaustive global ranking. A prompt-only mention after selection would be insufficient, while returning complete catalog eligibility/explanations would add unnecessary cost and a second pipeline.
- No store selection or no qualifying matches must reproduce existing ordering. Incomplete enrichment does not remove dishes from normal evaluation. A nonfunctional source or malformed ingredient profile is not evidence that no suitable dish exists.

### 7. Return structured offer facts and preserve weekly shopping semantics

Extend the existing result additively with a recommendation-level `groceryCoverage` and per-dish `groceryOffers` array. Coverage includes evaluation time and the canonical sorted store IDs actually used by the server. Each server-built offer entry has stable reference and ingredient key, retailer/product identity, verified price/discount fields, `Fact`/`Inference` for the dish relationship, observation instant, exclusive `usableUntil` UTC instant, optional inclusive buy-by date, freshness, known conditions, and a validated optional source URL. Keep optional-date states explicit in the TypeScript/Zod contract. Limit displayed/scoring opportunities to the two contributing ingredients; no product grid.

Only attach offers derived from the authorized dish profile and current selected stores. Supply their bounded reference text in existing per-dish evidence for the semantic provider. Extend source-reference validation rather than trusting provider-generated grocery claims. Structured grocery facts are authoritative; the provider does not reproduce promotional numeric claims in free text. A saved recipe fact and a factual offer do not make an inferred relationship factual.

Assess current purchasability using Copenhagen today, not the dinner's target date. Show an inclusive deadline with weekday and actual date: `Buy by Wednesday, 21 October` / `Køb senest onsdag den 21. oktober`. Current offers may help future-week plans only as current purchase opportunities. Unknown expiry is labeled unknown. No exact meal saving, stock guarantee, or preservation/shelf-life assumption is inferred.

The server reads current family settings on each new request; no preference copy in a week record. Saving stores affects subsequent requests, not saved dinners. Capture the server-returned canonical store selection with the retained result set and include it in append identity. A locally observed saved-selection change cancels in-flight work, marks retained results stale, and requires explicit refresh before More can append. If a More response reveals a different server selection (including another owner's change), do not append it; keep the existing cards and show the same stale/refresh affordance. Explicit refresh preserves conversation, week, and target but starts a new result set without old result/dismissal exclusions. Reuse existing cancellation/generation and family/account guards; no polling, preference store, or new cache is needed.

For same-selection More responses, keep one conservative coverage summary for the retained result set. A later fresh response cannot clear an earlier stale, unavailable, or partial assessment while those results remain retained; mixed coverage is partial with the existing limitations retained. A successful explicit refresh replaces both cards and coverage. Missing additive coverage in an old API response means unknown coverage, never verified complete coverage. Keep all of this state in `useDishRecommendations`, not in cards or a second Pinia store.

The existing composable owns one timeout for the earliest `usableUntil` among displayed current opportunities. Recheck all visible opportunities when it fires, on return/visibility, and on Danish date rollover; hide or mark expired entries and schedule the next deadline without reranking dishes or fetching offers. Reschedule when visible results change and clear the timeout/listeners on scope disposal or family/account reset. Compare server instants directly; use Europe/Copenhagen for calendar labels and the current i18n locale for formatting. This covers midday freshness expiration as well as midnight promotional expiration.

### 8. Reuse the current Vue components and brand

Component contracts and ownership:

| Existing surface | Focused change |
| --- | --- |
| `pages/families.vue` | Compose one store-settings section in each existing family card; keep current member UI and `.some` owner check |
| `components/Family/GroceryStores.vue` (one new focused child) | Props: persisted selection, supported descriptors, editable/busy/error; emits: save selected IDs. Own a local draft, reset it when displayed family/persisted values change, and keep it on save failure. No HTTP or Pinia mutation inside the child |
| `repository/family-repository.ts`, `stores/families.ts` | Add validated save/load fields through existing repository/store. Page coordinates save, canonical reload, and existing snackbar feedback; no new preferences store |
| `PlanningDishCard.vue` | Render structured grocery rows in its existing reasons/additional area, preserving all existing slots and actions; no second card or rating implementation |
| `RecommendationExplorer.vue` | Render one localized coverage status in its existing feedback area, avoiding repeated source-failure messages on every card |
| `types/dish-recommendations.ts`, repository, `useDishRecommendations` | Extend existing Zod/TypeScript contracts and family/locale/late-response guards; keep scoring, inventory, and inference out of Vue |

Following the user-requested visual refinement, the family section shows a compact saved-store summary with an edit action. Editing reuses the existing Plan/PickerShell desktop dialog and mobile sheet, with a two-column checkbox tile grid (one column on very narrow screens), separate border-shop group, one shared availability notice, and persistent Cancel/Save actions. Read-only members see a short saved-store summary. Wrap rows/actions on mobile. Do not rewrite the whole Families page or extract an unrelated settings framework. Supported option labels and computed formatted values respond to locale changes; stable retailer IDs do not.

Use Composition API with `<script setup lang="ts">` for the new child; leave unrelated existing component style intact. Props down/events up; persisted server state and local unsaved draft have explicit ownership. Reuse `storeToRefs` when consuming reactive Pinia state. Use computed derivations, explicit typed emits, cancellation/generation checks, and no deep watchers or duplicated persisted refs. Read-only expiry presentation may use the existing composable's lifecycle rather than creating a general timer service.

Replace the current locale watcher's result-clearing behavior with cancellation of obsolete requests and computed reformatting of retained structured facts. Preserve cards, ordering, request, and browsing context. Existing provider prose remains in its original language until explicit refresh; show the existing localized stale/refresh affordance rather than automatically fetching or translating it. More requires that refresh before mixing provider-response languages.

Visual constraints: existing `text-section-title` and card body styles; `--space-*`, `--radius-*`, surface/text/border tokens; restrained existing accent/tonal treatment for deadlines; text and icons plus labels rather than color alone. Brand retailer names may remain names, but all supporting copy, conditions, statuses, accessibility labels, dates, and prices use English/Danish localization. No new fonts, images, retailer logos, shadows, navigation entry, animations, or primary palette. Product-source links are separate from the dish-name `NuxtLink`; assignment stays as it is.

**Alternative rejected:** new shopping dashboard, generic settings system, duplicate `OfferDishCard`, or migrating this small feature into a new Pinia store.

## Risks / Trade-offs

- **Retailer data access or caching cannot be verified** -> Treat acquisition as the first gating task, document source choice, keep unavailable coverage explicit, and do not declare the two-retailer outcome delivered early.
- **Inference confidence is imperfect** -> Preserve evidence/specificity and explicit overrides, reject vague/contradictory matches, test Danish/English dish examples, and never display scores as calibrated percentages.
- **Cold catalogs incur additional inference cost** -> Bounded batches and a shared optional budget; content-hash caching; honest partial coverage; no catalog backfill or exhaustive new semantic stage.
- **Repeated source failures consume quota** -> Share a persisted five-minute cooldown across families and instances, retry only on demand, retain only observations allowed by freshness/expiry, and reuse successful empty responses until the next daily boundary.
- **Chain-level pricing may differ by store** -> Source-gate scope verification, omit unverified local/member restrictions from boosts, and display relevant limitations. A postcode/store picker is outside this change.
- **Existing family replacement saves erase preferences** -> Preserve `groceryStores` atomically in all existing family write paths, and test both concurrency directions before enabling the owner UI.
- **LLM shortlist remains heuristic** -> Inject offer evidence before each existing shortlist and apply the server bonus only once; do not promise exact global optimization.
- **Retained cards outlive offers** -> Reassess at the server-provided deadline and on visibility/date change while keeping list order and planning context intact.

## Migration Plan

1. Complete the source gate and record source-specific fixtures/configuration. No new service subscription is assumed. Provision the cache container with verified partition key and TTL. Keep offer enrichment behind a backend configuration flag until source, budget, and normalization checks pass.
2. Deploy additive backend settings reads and write preservation before exposing store edits. Old documents read as empty; no backfill is required. Deploy family-writer preservation consistently across instances so an old replacement writer cannot remove the field.
3. Deploy cache/profile adapters and pure scoring; extend result DTOs additively. Then deploy the existing frontend surfaces and en/da keys, keeping missing additive fields compatible during rollout.
4. Verify both requested retailers with live permitted responses, representative domain cases, failure/expiry cases, and actual desktop/mobile browser comparisons. Optional source fixtures alone do not prove live coverage.
5. Roll back by disabling optional enrichment and hiding the new settings section; retain saved store choices and ordinary recommendation/dinner data. Dispose of operational caches via TTL. Keep preference-preserving family persistence while any deployed version exposes the setting.

## Validation Strategy

- Core tests instantiate value objects/rule directly: meaningful minced meat versus salt, exact versus inferred/species matches, resolved vegetarian profiles rejecting meat, duplicate brands/stores and parent categories, capped scoring, threshold boundaries, source age, inclusive expiry, missing dates, exclusive `usableUntil`, and Copenhagen date/DST boundaries. Adapter fixtures verify notes overriding recipe evidence before scoring.
- Application/Functions tests cover any owner's edit, denied ordinary/cross-family calls, invalid stores, empty/default compatibility, canonical saves, unrelated member-field preservation, and an older member writer completing after a store update.
- Query/provider tests verify deterministic candidate filters run before optional profile reads/inference, a cold catalog's ineligible dishes consume no enrichment budget, offers reach every existing batch and finalist prompt before shortlist, suitability excludes the bonus, server applies it once, source references are validated, constraints/exclusions/exhaustion remain intact, profile hashes isolate families and invalidate evidence changes, optional failures preserve normal outcomes, and stale upstream observations are not refreshed by retrieval time.
- Cache adapter checks exercise actual conditional publication/claim and page/TTL behavior through existing integration-test conventions where Cosmos is required; controlled transport tests cover pagination, source fixtures, timeouts, and malformed payloads. Do not call mocked unit tests proof of emulator or live retailer behavior.
- Vue tests use existing Vitest/Vue Test Utils conventions to check visible owner/read-only controls, save failure/draft preservation, locale changes retaining cards, structured reasons/deadlines, separate links/actions, midday and midnight expiry without reranking, timer disposal, store changes during More, conservative coverage across appended results, and family/late-response isolation. Reuse existing planning/navigation tests rather than adding a parallel harness.
- Implementation acceptance requires frontend tests, lint, build, backend build/unit tests, appropriate Cosmos integration checks, and rendered desktop/mobile comparison against existing equivalents. At proposal time only artifact validation is run; no implementation or visual success is claimed.

## Authorized scope update ? 2026-10-18

Runtime configuration correction: the user rejected a per-store verification allowlist. One shared Prej configuration now enables every catalog chain; family selections alone determine which chains are requested. Offer-level price, identity, freshness and condition checks remain. Verification records describe test coverage and do not become a second production store-selection mechanism.

The user explicitly authorized continuing fixture-based implementation while live retailer enablement remains gated, and expanded family store selection to every available Prej chain. Task 1 verification gaps remain tracked and do not block offline implementation. A live `/v1/chains` response was captured (21 chain records, 18/25 calls remaining); use its `chains` records, not auxiliary slug lists containing unlisted chains. Unknown new IDs are rejected, existing selections survive catalog disappearance, and source availability is independent of selectability. All chains use the same bounded adapter; none bypass freshness, price comparability, condition, or coverage rules. Production source permission and per-chain validation remain deployment gates, not a claimed completed integration.
