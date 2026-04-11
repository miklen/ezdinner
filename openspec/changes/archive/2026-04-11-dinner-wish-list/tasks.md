## 1. Backend — Domain Layer

- [x] 1.1 Create `EzDinner.Core/Aggregates/WishlistAggregate/` folder with `WishlistItem.cs` aggregate root (fields: id, familyId, dishId, dishName, addedBy, addedAt, expiresAt, votes list)
- [x] 1.2 Create `Vote.cs` value object inside `WishlistAggregate/` (fields: userId, votedAt)
- [x] 1.3 Add `Upvote(userId)` method on `WishlistItem` that adds a vote and extends `expiresAt` to `max(expiresAt, voteDate + 14 days)`
- [x] 1.4 Add `IsExpired` computed property on `WishlistItem` (`expiresAt < now`)
- [x] 1.5 Add unit tests for `Upvote` (first vote, duplicate vote, self-vote, expiry extension logic)

## 2. Backend — Infrastructure

- [x] 2.1 Create `IWishlistRepository` interface in `EzDinner.Infrastructure` with methods: `GetActiveAsync`, `GetByDishAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`
- [x] 2.2 Implement `CosmosWishlistRepository` with CosmosDB container `Wishlist` (partition key `/familyId`)
- [x] 2.3 Provision `Wishlist` container in `PUT /api/migrate` alongside existing containers

## 3. Backend — Application Layer (Commands)

- [x] 3.1 Create `AddWishCommand` (familyId, dishId, dishName, requestedByUserId) — checks for existing wish, returns 409 with wish ID if duplicate, else creates and saves
- [x] 3.2 Create `RemoveWishCommand` (familyId, wishId, requestingUserId) — enforces ownership: requester or family owner only; returns 403 otherwise
- [x] 3.3 Create `UpvoteWishCommand` (familyId, wishId, userId) — enforces no-duplicate-vote rule; updates expiry; triggers `WishUpvoted` notification (skip if self-vote)
- [x] 3.4 Extend `AddDishToDinnerCommand` to call wishlist after successful dinner assignment: find active wish for dishId+familyId, delete it, trigger `WishGranted` notifications (best-effort — catch and log, don't fail the command)

## 4. Backend — Query Layer

- [x] 4.1 Create `GetWishlistQuery` (familyId) returning `IReadOnlyList<WishlistItemResult>` — filters expired items, sorts by vote count desc then createdAt asc
- [x] 4.2 Create `WishlistItemResult` model (wishId, dishId, dishName, addedBy name, voteCount, voterIds, expiresAt, isVotedByCurrentUser)

## 5. Backend — Push Notifications

- [x] 5.1 Add `WishUpvotedNotification` type to push notification service — sends to original requester when someone else upvotes their wish (body: "{voterName} also wants {dishName}!")
- [x] 5.2 Add `WishGrantedNotification` type — sends to requester + all voters when wish is granted (body: "{dishName} is on the menu this week! 🎉"); deduplicate recipients
- [x] 5.3 Wire both notification sends into `UpvoteWishCommand` and the wish-grant path in `AddDishToDinnerCommand`

## 6. Backend — HTTP Functions

- [x] 6.1 Create `WishlistFunctions.cs` with `GET /api/families/{familyId}/wishlist` → returns sorted active wishes
- [x] 6.2 Add `POST /api/families/{familyId}/wishlist` → AddWishCommand; returns 201 with wish, or 409 with existing wish ID
- [x] 6.3 Add `POST /api/families/{familyId}/wishlist/{wishId}/upvote` → UpvoteWishCommand; returns 200 or 409 on duplicate vote
- [x] 6.4 Add `DELETE /api/families/{familyId}/wishlist/{wishId}` → RemoveWishCommand; returns 204 or 403

## 7. Backend — Wish Stats

- [x] 7.1 Add `wishesAdded` and `wishesGranted` counters to the family member stats document (or create a `WishStats` document per member if no stats document exists yet)
- [x] 7.2 Increment `wishesAdded` in `AddWishCommand`; increment `wishesGranted` in the wish-grant path of `AddDishToDinnerCommand`

## 8. Frontend — Pinia Store

- [x] 8.1 Create `web/stores/wishlist.ts` Pinia store with state: `wishes: WishlistItem[]`; actions: `fetchWishes`, `addWish`, `removeWish`, `upvoteWish`
- [x] 8.2 Add TypeScript types `WishlistItem` and `AddWishRequest` to `web/types/index.ts`
- [x] 8.3 Add wishlist API calls to `web/repositories/` (or the existing repositories composable)

## 9. Frontend — Add Wish Flow

- [x] 9.1 Create `web/components/Wish/AddWishDialog.vue` — dish search input (reuse existing dish search pattern), shows existing wishes with +1 affordance, "create and add" fallback at bottom de-emphasized
- [x] 9.2 When search returns a dish already in wish list, show "Already wished by {name} — tap to +1" instead of "Add to wish list"
- [x] 9.3 Add "Add to wish list" action button on the dish detail view (reuse the dish detail sheet/page)
- [x] 9.4 Wire quick-create path: after creating a new dish, prompt "Add to wish list?" before closing the creation dialog
- [x] 9.5 Add i18n keys for all new strings in `en.json` and `da.json`

## 10. Frontend — Plan Page Wish List Panel

- [x] 10.1 Create `web/components/Plan/WishList.vue` — shows wishes sorted by vote count, each row: dish name, requester name, vote count, +1 button, remove button (own wishes / owners)
- [x] 10.2 Show empty state when wish list is empty ("No wishes yet — family members can add wishes from any dish")
- [x] 10.3 Wire +1 button to `upvoteWish` store action; disable if current user already voted
- [x] 10.4 Wire remove button with permission check (own wish or family owner)
- [x] 10.5 Replace `<PlanTopDishes />` with `<PlanWishList />` in `web/pages/plan.vue` support slot
- [x] 10.6 Load wishes on plan page mount alongside existing `loadWeek()` calls
- [x] 10.7 After a dish is assigned to a dinner day (in `onSuggestionUsed` and inline assignment), refresh the wish list to reflect any auto-granted wish removal
- [x] 10.8 Add i18n keys for all new strings in `en.json` and `da.json`

## 11. Frontend — Mobile Wish Entry Point

- [x] 11.1 Ensure the "Add to wish list" action is accessible on mobile from the dish detail view (the plan page support panel is below the dinner list on mobile — primary mobile entry point should be dish detail)
- [x] 11.2 Verify wish list panel stacks correctly below dinner list on mobile (cols="12" in Content split layout) and is usable without horizontal scroll
