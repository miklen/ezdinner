## Why

The wishlist feature shipped as functional MVP but has several UX rough edges: vote indicators use the wrong brand color (green instead of amber), the remove button's error-hover background is broken CSS, wished dishes have no path to their detail page, the empty state has no CTA near the content itself, and once a vote is cast there is no way to retract it if added by mistake.

## What Changes

- Fix `wish-row__star` and upvoted state to use the amber accent color (`--color-accent`) — consistent with rating hearts
- Fix broken `rgba(var(--color-error), 0.06)` CSS on the remove button hover (hex variable inside `rgba()` is invalid; needs an RGB token)
- Make the dish name in each wish row a `NuxtLink` navigating to `/dishes/{dishId}` so members can inspect a wish before upvoting
- Add an "Add a wish" button inside the empty state itself, not just in the header
- Increase visual weight of the vote count (it is the ranking signal — currently undersized)
- Add a tactile `scale` micro-interaction on the upvote button click
- Add a stagger-in animation to the empty state elements
- **Allow un-upvoting**: clicking the upvote button again when already voted removes the vote — requires a new backend endpoint and a domain method on `WishlistItem`

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `wish-list`: Vote indicators use accent color; dish name navigates to detail; empty state includes inline CTA; upvote is a toggle (add and remove); vote count display has appropriate visual weight

## Impact

- `web/components/Plan/WishList.vue` — color fixes, dish name link, empty state CTA, un-upvote toggle, visual polish
- `web/assets/global.scss` — add `--color-error-rgb` and `--color-accent-rgb` tokens
- `api/src/EzDinner.Core/Aggregates/WishlistAggregate/WishlistItem.cs` — new `RemoveUpvote(userId)` domain method
- `api/src/EzDinner.Application/Commands/Wishlist/RemoveUpvoteCommand.cs` — new command
- `api/src/EzDinner.Functions/WishlistFunctions.cs` — new `WishlistRemoveUpvote` function (DELETE upvote)
- `web/stores/wishlist.ts` — new `removeUpvote` action
