## Why

When the week's dinner plan is already set, family members (especially kids) have no way to capture dishes they'd like in a future week. Requests get forgotten, and the kids feel unheard. A family wish list gives those requests a home — and surfaces them when the planner is deciding next week's menu.

## What Changes

- **New**: Family wish list — a shared, vote-weighted list of dishes family members want planned soon
- **New**: Any family member can add a dish to the wish list from the dish detail view; if the dish doesn't exist yet, they can quick-create it first
- **New**: Any family member can +1 an existing wish (deduplication enforced — one entry per dish)
- **New**: Vote-driven decay — each +1 (including self-vote) extends the wish's expiry; wishes with no activity for 14 days are auto-removed
- **New**: When a wished dish is planned, the wish is auto-removed and push notifications are sent to the original requester and all voters ("Your wish was granted!")
- **New**: When someone +1s a wish, the original requester is notified
- **New**: Wish list panel replaces the unused Top Dishes panel in the plan page support sidebar
- **New**: Wish stats tracked per family member (wishes added, wishes granted) as a seed for future gamification
- **Modified**: Planning flow checks the wish list when a dish is assigned to a dinner day and triggers removal + notification

## Capabilities

### New Capabilities

- `wish-list`: Family-shared wish list for dinner dishes — add, vote, decay, grant, and notify

### Modified Capabilities

- `dinner-push-delivery`: New notification triggers — wish +1'd (to requester) and wish granted (to requester + voters)

## Impact

- **Backend**: New `WishlistItem` aggregate in `EzDinner.Core`; new commands (`AddWish`, `RemoveWish`, `UpvoteWish`); `AddDishToDinner` command extended to check and grant wishes; new query `GetWishlist`; push notification triggers wired to existing infra
- **Frontend**: New `PlanWishList` component replacing `PlanTopDishes` in `plan.vue`; new "Add to wish list" action on dish detail; add-wish flow with catalog search + dedup + quick-create fallback
- **API**: New endpoints under `/api/families/{familyId}/wishlist`
- **Data**: New CosmosDB container or document type for wish list items
