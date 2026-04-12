## REMOVED Requirements

### Requirement: Wish list is displayed in the plan page support panel
**Reason:** The plan page support panel is replaced by the Planning Assistant panel, which incorporates wish signal (vote count) as a read-only indicator on each dish row. Full wishlist management is accessible via the Wishlist mode toggle within the Planning Assistant panel.
**Migration:** The `PlanWishList` component is removed from the `#support` slot in `plan.vue`. Wishlist management is available via the Wishlist toggle in the Planning Assistant panel. No data loss — the wishlist data model and API are unchanged.
