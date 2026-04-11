## 1. Design System Tokens

- [x] 1.1 In `web/assets/global.scss`, add `--color-error-rgb: 198, 40, 40` to `:root` alongside `--color-error`
- [x] 1.2 Add `--color-accent-rgb: 212, 168, 75` to `:root` alongside `--color-accent`

## 2. Backend — Un-upvote

- [x] 2.1 In `WishlistItem.cs`, add `RemoveUpvote(Guid userId)` — remove the `Vote` entry for the user; throw `InvalidOperationException("NOT_VOTED")` if no vote exists; do not recalculate expiry
- [x] 2.2 Create `RemoveUpvoteCommand.cs` in `EzDinner.Application/Commands/Wishlist/` — load wish, call `RemoveUpvote`, save; return `Removed`, `NotFound`, or `NotVoted` result enum
- [x] 2.3 In `WishlistFunctions.cs`, add `WishlistRemoveUpvote` function: `DELETE families/{familyId}/wishlist/{wishId}/upvote`, auth check `Resources.Wishlist, Actions.Update`; map results to `204 / 404 / 409`
- [x] 2.4 Build backend: `cd api/src/EzDinner.Functions && dotnet build` — confirm 0 errors

## 3. WishList Color & Visual Fixes

- [x] 3.1 In `WishList.vue`, change `.wish-row__star` color from `var(--color-primary)` to `var(--color-accent)`
- [x] 3.2 Change `.wish-row__upvote--voted` color from `var(--color-primary)` to `var(--color-accent)`
- [x] 3.3 Change `.wish-row__upvote:hover` color to `var(--color-accent)` and background to `rgba(var(--color-accent-rgb), 0.08)`
- [x] 3.4 Fix `.wish-row__remove:hover` background from `rgba(var(--color-error), 0.06)` to `rgba(var(--color-error-rgb), 0.06)`
- [x] 3.5 Increase `.wish-row__votes` to `font-size: var(--text-sm)` and `font-weight: 700`
- [x] 3.6 Add `active` micro-interaction to `.wish-row__upvote`: `transform: scale(0.88)` with a `var(--duration-instant)` transition

## 4. Dish Name Navigation

- [x] 4.1 Verify `WishlistItem` type in `~/types` exposes `dishId: string` — add it if missing (check API query response shape in `WishlistQueries`)
- [x] 4.2 In the wish row template, replace the `wish-row__dish` span with a `NuxtLink :to="'/dishes/' + wish.dishId"` — the span becomes the link's inner content only, not the outer container
- [x] 4.3 Style the link: `display: block`, inherit color, no underline by default, subtle underline on hover with `text-decoration-color: var(--color-primary-dark)`, `text-underline-offset: 2px`

## 5. Empty State CTA & Animation

- [x] 5.1 In the empty state block, add an "Add a wish" button below `wish-list__empty-text` that sets `addWishDialogOpen = true`; apply the existing `wish-list__add-btn` class
- [x] 5.2 Add `fade-up` keyframe and stagger-in classes: icon animates at 0ms delay, text at 80ms, button at 160ms; suppress with `@media (prefers-reduced-motion: reduce)`

## 6. Frontend — Un-upvote

- [x] 6.1 In `wishlist.ts` store, add `removeUpvote(wishId: string)` action — call `DELETE /api/families/{familyId}/wishlist/{wishId}/upvote`; update local wish state (flip `isVotedByCurrentUser`, decrement `voteCount`)
- [x] 6.2 In `WishList.vue`, change the upvote button: when `wish.isVotedByCurrentUser`, button is no longer `disabled` — clicking calls `removeUpvote(wish)`
- [x] 6.3 Update the upvote button tooltip: when voted, show `$t('wishlist.removeVoteTooltip')` instead of `$t('wishlist.alreadyVotedTooltip')`
- [x] 6.4 Add i18n keys `wishlist.removeVoteTooltip` to `en.json` ("Remove your vote") and `da.json` ("Fjern din stemme")
- [x] 6.5 Add `upvotingId` guard to `removeUpvote` to prevent double-tap (reuse existing ref)

## 7. Verification

- [x] 7.1 Run `npm run lint` in `web/` — confirm no new errors
- [ ] 7.2 Confirm vote stars and upvoted button state appear amber
- [ ] 7.3 Confirm upvote button has scale animation on click
- [ ] 7.4 Confirm voted upvote button is clickable and removes the vote; count decrements
- [ ] 7.5 Confirm clicking dish name navigates to `/dishes/{dishId}`
- [ ] 7.6 Confirm empty state CTA opens add-wish dialog
- [ ] 7.7 Confirm empty state elements stagger in
- [ ] 7.8 Confirm remove button hover renders a faint red tint (not transparent)
