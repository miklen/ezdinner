## 1. Remove the review page

- [x] 1.1 Delete `web/pages/dishes/review.vue`

## 2. Clean up the dishes catalog

- [x] 2.1 Remove the `pendingReviewCount` computed property from `web/pages/dishes/index.vue`
- [x] 2.2 Remove the "Review AI suggestions" button block (`v-btn` with `:to="'/dishes/review'"`) from the catalog header in `web/pages/dishes/index.vue`
- [x] 2.3 Remove the `.catalog__review-badge` CSS rule from `web/pages/dishes/index.vue`
