# Proposal

## Why

Planning dinner currently requires switching between dish discovery and the week overview, while the AI planner assigns dishes to dates without enough recipe evidence to support useful requests. Families need a continuous dish-first experience: explore, ask for ideas, inspect, choose a day, and keep exploring with their decisions visible.

## What Changes

- Add an independent `Plan your week` page and navigation entry; retain the existing Plan, Dishes, assistant, and suggestion endpoints.
- Make dishes the primary exploration surface, combining catalog browsing, family wishes, ratings, last-served information, and explained recommendations.
- Keep a supporting nine-day overview: Saturday and Sunday preceding the selected Monday, Monday-Friday, and the following Saturday-Sunday. Existing previous/next week navigation moves the whole window.
- Support free-text requests and follow-up refinements grounded in dish titles, user notes, captured recipes, metadata, usage history, and wishes. Return dishes rather than dated week drafts.
- Recommend forgotten favourites and dishes approaching their observed rotation without letting indefinite age or missing history dominate.
- Support recipe inspection, assignment to any visible day, explicit removal, and undo without losing browsing or conversational context. Use existing saved dinners, not an experimental draft database.
- Provide desktop and mobile layouts that preserve the explore/decide loop, with accessible controls and English/Danish text.
- Leave dietary goals, variety, and meal quality to the family. No unsolicited nutritional judgments, balance scores, or variety steering.

## Capabilities

### New Capabilities

- `dish-first-week-planning`: An isolated, dish-first planning workspace with continuous exploration, nine-day overview/navigation, recipe inspection, and shared dinner assignments.
- `dish-recommendations`: Explained dish recommendations based on family history and conversational requests, with evidence-aware reasoning and explicit request context.

### Modified Capabilities

None. Existing day/week suggestions and assistant requirements remain applicable to their current surfaces; this change introduces a separate recommendation contract.

## Impact

- Nuxt frontend: new `/plan-your-week` route, focused feature components/composables, recommendation repository/types, navigation link, and both locale files.
- Backend: additive family-authorized recommendation query/API, pure domain recommendation rules/value objects, and a dedicated LLM adapter using the existing provider infrastructure.
- Reuse existing dinner mutations and dish/wishlist queries. No new dinner aggregate, conversation database, or migration.
- Coordinate optional captured-recipe consumption with the in-flight `capture-recipe-snapshots` change; keep dishes without recipes usable. The catalog-preference change remains independent.
- Add focused domain, query/provider, repository, and Vue interaction coverage plus desktop/mobile visual validation before accepting the UX.
