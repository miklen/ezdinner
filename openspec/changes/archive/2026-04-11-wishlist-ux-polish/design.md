## Context

The wishlist panel lives in `web/components/Plan/WishList.vue`. Most changes are frontend-only. The un-upvote feature additionally requires a new backend endpoint and a domain method on `WishlistItem`. Votes are stored as `List<Vote>` on the aggregate; removing a vote is a straightforward list removal.

## Goals / Non-Goals

**Goals:**
- Consistent amber color for all vote/wish indicators
- Valid CSS for error-state hover on the remove button
- Dish name navigates to `/dishes/{dishId}` (link wraps text only, not the row)
- Empty state has an inline CTA with stagger-in animation
- Vote count given appropriate visual weight (it is the sort signal)
- Upvote button is a toggle — voted state allows retraction
- Tactile scale micro-interaction on upvote click

**Non-Goals:**
- Expiry rollback when a vote is removed (expiry stays extended — acceptable simplification)
- Push notification on un-upvote (no meaningful signal to send)
- "In plan this week" indicator (deferred)
- Sorting changes (already server-side)

## Decisions

**1. RGB tokens for opacity variants**
Add `--color-error-rgb: 198, 40, 40` and `--color-accent-rgb: 212, 168, 75` to `:root` in `global.scss`. Replace invalid `rgba(var(--color-error), 0.06)` with `rgba(var(--color-error-rgb), 0.06)`. Follows the same pattern as `--color-primary-rgb`.

**2. Dish name as NuxtLink (text-only)**
Per CLAUDE.md: do not wrap the entire row in a link when it has interactive children. Wrap only the `wish-row__dish` span. The link is `display: block` to maximise the tap target width on mobile without conflicting with the action buttons. Style: inherit color, no underline by default, subtle underline on hover.

**3. Empty state stagger animation**
Three elements (icon, text, button) each get `animation-delay` of 0ms, 80ms, 160ms on a simple `fade-up` keyframe. CSS-only, no JS. Removed motion respects `prefers-reduced-motion`.

**4. Vote count visual weight**
Bump `.wish-row__votes` from `font-size: var(--text-xs)` to `var(--text-sm)` and `font-weight: 600` to `700`. Color stays `--color-text-secondary`. No layout changes needed.

**5. Upvote as a toggle (un-upvote)**
- Domain: add `RemoveUpvote(Guid userId)` to `WishlistItem` — removes the `Vote` entry for the user. Throws `NOT_VOTED` if no vote exists. Expiry is not recalculated.
- Backend: new `DELETE /api/families/{familyId}/wishlist/{wishId}/upvote` function `WishlistRemoveUpvote`. Authorised with `Resources.Wishlist, Actions.Update` (same as upvote). Returns `204 No Content` on success, `404` if wish not found, `409` if user had not voted.
- Frontend: when `wish.isVotedByCurrentUser` is true, the upvote button is no longer disabled — clicking calls `removeUpvote(wish)`. Tooltip changes to "Remove your vote". The button retains amber colour in voted state; on hover it dims slightly to signal it is clickable.
- Store: add `removeUpvote(wishId)` action alongside existing `upvoteWish`.

**6. Upvote button micro-interaction**
Add `active:scale-95` equivalent via CSS: `.wish-row__upvote:active { transform: scale(0.88); }` with a fast transition. CSS-only, zero JS.

## Risks / Trade-offs

- [Risk] `dishId` must be on the `WishlistItem` frontend type. → Check `~/types`; add if missing.
- [Risk] Un-upvoting the creator's initial vote leaves wish with 0 votes. → Wish stays on list until natural expiry. Acceptable — user can also remove the wish entirely.
- [Risk] Expiry not rolled back on un-upvote. → Acceptable simplification; wish expires within 14 days of last real activity regardless.

## Migration Plan

Frontend changes: deploy with next static web app build. Backend: deploy to `func-ezdinner-prod-02`. No data migration or `PUT /api/migrate` call required — no new Casbin policies needed (reuses existing `Wishlist:Update`).
