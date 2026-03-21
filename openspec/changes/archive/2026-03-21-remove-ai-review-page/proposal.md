## Why

The "Review AI suggestions" button and dedicated review page are unnecessary — users can already see and edit AI-suggested metadata directly on each dish's detail page immediately after saving. The extra navigation step adds friction without value.

## What Changes

- Remove `web/pages/dishes/review.vue` (the review page)
- Remove the "Review AI suggestions" button from the dishes catalog header (`web/pages/dishes/index.vue`)
- Remove the `pendingReviewCount` computed and its associated CSS (`.catalog__review-badge`) from the catalog page

## Capabilities

### New Capabilities

_None_

### Modified Capabilities

- `dish-ai-enrichment`: Remove the review flow — AI suggestions are no longer surfaced as a separate review step; users confirm/edit metadata inline on the dish detail page

## Impact

- `web/pages/dishes/review.vue` — deleted entirely
- `web/pages/dishes/index.vue` — button block and `pendingReviewCount` computed removed
- No backend changes required
- No API changes
