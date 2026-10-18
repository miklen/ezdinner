# Proposal

## Why

Families currently receive dish suggestions without considering useful grocery discounts at the supermarkets they actually use. Relevant offers on important ingredients should make suitable dishes more attractive, even when ingredient matching must rely on the dish name rather than a saved recipe.

## What Changes

- Add one owner-managed grocery-store selection to each family, supporting every chain returned by Prej?s `/v1/chains`, including border-trade and flyer-only chains. All members use the same selection; there are no weekly overrides. Owners can change or clear it whenever needed.
- Retrieve verified offers through a bounded source adapter and share a persistent daily cache across families. Confirm acquisition feasibility, coverage, validity dates, conditions, and caching permission before enabling offer acquisition for a retailer.
- Derive and cache a small ingredient profile from existing dish titles, notes, and captured recipes after applying existing candidate filters. Distinguish explicit ingredients from culinary inference, retain match confidence and specificity, and resolve family adaptations before scoring.
- Boost recommendations for meaningful discounts on primary, cost-relevant ingredients. Salt and other incidental pantry ingredients do not qualify. Deduplicate equivalent products and retailers, cap the boost, and preserve request suitability and explicit exclusions.
- Assume weekly shopping. An offer may help a dish served after the offer ends; show the purchase deadline, retailer, ingredient, and known conditions rather than claiming exact meal savings.
- Extend the existing Families cards and Plan your week recommendation cards, using the current brand tokens, heart ratings, dish navigation, assignment controls, loading/error patterns, and English/Danish localization.
- Keep ordinary recommendations usable when optional offer retrieval or ingredient inference is unavailable, with honest coverage feedback across retained results. Tie appended results to their evaluated store selection and stop presenting offers as current at their server-computed evidence deadline, without reordering dishes.

## Capabilities

### New Capabilities

- `family-grocery-stores`: One persisted family store selection, owner-only updates, member read access, compatible handling of existing families, and extensible retailer identifiers.
- `grocery-offer-cache`: Verified and normalized offer observations, shared daily caching, freshness and expiry rules, bounded refresh, and independent source failure handling.
- `dish-ingredient-matching`: Evidence-aware primary-ingredient profiles, confidence and specificity, content-based invalidation, and reusable offer-to-ingredient matching.

### Modified Capabilities

- `dish-recommendations`: Apply bounded offer relevance to existing automatic and conversational recommendations, with validated offer evidence and continued precedence for suitability.
- `dish-first-week-planning`: Present offer reasons and purchase deadlines in existing cards without disturbing browsing, navigation, assignment, or responsive continuity.

## Impact

- Backend: extend `Family` and its existing query/DTO mapping; add a focused family command/endpoint using existing Family permissions; add pure ingredient/offer value objects and a scoring rule in Core, query orchestration in Query.Core, and source/LLM/cache adapters in Infrastructure.
- Frontend: reuse `web/pages/families.vue`, `FamilyRepository`, `useFamiliesStore`, `RecommendationExplorer`, `PlanningDishCard`, `DishCard` slots, existing Zod recommendation contracts, and `web/assets/global.scss`. No new route, planner, independent dish card, grocery catalog UI, or client-side offer cache.
- Storage/operations: small family document addition plus bounded operational offer/profile cache documents. No catalog backfill, full inventory mirror, per-request retailer search, recurring TimerTrigger, new store platform, shopping list, retailer login, checkout integration, or recipe-import replacement.
- External dependencies: source selection remains a first-task feasibility decision. Prej is a documented candidate, not a confirmed integration or approved subscription. Existing Anthropic and Cosmos infrastructure can be reused; no new package or service is assumed.
- Compatibility: existing day/week planners, Plan, Dishes, recipe imports, and saved dinners retain their current contracts and interactions. This change targets the current Plan your week recommendation pipeline.
