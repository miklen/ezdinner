## Context

EzDinner families plan weekly dinners from a shared dish catalog. When the week is already set, family members (typically children) have no way to register dishes they'd like in a future week. Requests are forgotten and kids feel unheard.

The wish list introduces a family-scoped, vote-weighted backlog of desired dishes. The key design challenge is preventing the list from becoming dominated by low-effort junk-food entries — solved through deduplication (one entry per dish, everyone +1s the same entry) and vote-driven decay (forgotten wishes auto-expire; popular ones stay alive naturally).

Push notification infrastructure already exists (`POST /api/push/send-tonight`, `PushSubscriptions` container). The planning write path (`AddDishToDinnerCommand`) is the natural integration point for wish granting.

## Goals / Non-Goals

**Goals:**
- Family members can wish for dishes from the existing catalog
- One entry per dish — second requester +1s rather than duplicating
- Vote weight drives ranking; votes extend expiry (self-vote allowed as "I still want this")
- Wishes auto-expire after 14 days of inactivity; planning a dish grants and removes the wish
- Notifications: +1 notifies original requester; granting notifies requester + all voters
- Wish stats per family member tracked from day one (seed for gamification)
- Plan page support sidebar shows the wish list instead of the unused Top Dishes panel

**Non-Goals:**
- Freeform text wishes not linked to a catalog dish (quick-create flow handles this)
- Quarantine / cooldown after a wish is granted
- Gamification UI (tracked, not displayed yet)
- Real-time updates to the wish list panel (refresh on page load is sufficient)

## Decisions

### 1. One CosmosDB document per wish item, partitioned by `familyId`

**Decision**: Store wish items in a new `Wishlist` container with partition key `/familyId`.

**Rationale**: All queries are scoped to a single family (`GET wishlist for familyId`). Partitioning by `familyId` means all of a family's wishes are co-located — single-partition reads, no cross-partition fan-out. Each item is small (dishId, votes array, timestamps), so a family's entire wish list fits comfortably in one partition.

**Alternative considered**: Embedding wishes inside the `Family` document. Rejected — the Family document is already loaded frequently; embedding a growing list would bloat it and complicate partial updates.

### 2. Vote-driven expiry: `expiresAt = max(expiresAt, voteDate + 14 days)`

**Decision**: Each vote (including self-vote) extends expiry to at least 14 days from the vote date. Expiry is not a fixed duration from creation — it resets forward with every vote.

**Rationale**: Collapses ranking weight and longevity into a single signal. A vote means "I expect this within the next two weeks." No magic accumulation formula, no cap to argue about. A wish that keeps getting voted on never expires; a forgotten wish fades naturally.

**Alternative considered**: Fixed 14-day expiry from creation with a separate "renew" action. Rejected — adds UI friction and a superfluous concept. Self-vote already serves as renewal.

**Implementation**: `expiresAt` is stored on the document and updated on every upvote. Lazy evaluation on `GET /wishlist` filters out items where `expiresAt < now`. No background cleanup job needed initially.

### 3. Deduplication enforced at command level with UX nudge

**Decision**: `AddWishCommand` checks whether the dish already has an active wish. If so, it returns the existing wish ID with a `409 Conflict`-style response so the frontend can offer a +1 instead of silently failing or creating a duplicate.

**Rationale**: The catalog can't get polluted by duplicate entries if deduplication is a hard rule. The UX reinforces this — the add-wish search shows existing wishes prominently ("already wished by Emma") so the path of least resistance is +1, not create.

**Alternative considered**: Allow duplicates and merge on read. Rejected — two "I want pizza" entries from different kids is confusing and undermines the collective vote signal.

### 4. Wish granting triggered inside `AddDishToDinnerCommand`

**Decision**: After successfully adding a dish to a dinner, `AddDishToDinnerCommand` queries the wish list for that dishId and family. If found, it removes the wish and enqueues push notifications.

**Rationale**: The command already knows the familyId and dishId. No event bus infrastructure exists in the project — adding one for a single use case is disproportionate. A direct service call keeps the change self-contained.

**Alternative considered**: Domain event `WishGranted` published from the planning aggregate, handled by a separate consumer. Rejected — no event infrastructure, adds significant complexity for one trigger.

**Trade-off**: This couples the planning command to the wishlist. Acceptable for now; extractable to an event-driven model if the project later adopts domain events broadly.

### 5. Notification via existing push infrastructure

**Decision**: Two new notification types reuse the existing `PushNotificationService` / web push delivery path:
- `WishUpvoted` → sent to the original wish requester
- `WishGranted` → sent to the original requester + all voters

**Rationale**: Infrastructure exists and works. No new dependencies. Notification content is simple (dish name, family member name).

**Expiry is silent** — no "your wish expired" notification. Demoralising and noisy.

### 6. WishlistItem as a domain aggregate in `EzDinner.Core`

**Decision**: `WishlistItem` is an aggregate root in `EzDinner.Core/Aggregates/WishlistAggregate/`. `Vote` is a value object nested within it.

**Rationale**: Business rules (decay, dedup, vote limits) live in the domain. The aggregate enforces invariants: one vote per member, expiry extension on upvote, immutable `addedBy`.

## Risks / Trade-offs

**Lazy expiry evaluation on GET** → Expired wishes remain in CosmosDB until the family fetches their list. Low risk — storage cost for a few extra documents is negligible. A cleanup function can be added later if needed.

**Planning command coupled to wishlist** → If the wishlist service throws, the dinner assignment could fail or leave the wish un-granted. Mitigation: wrap wishlist grant in a try/catch that logs but does not fail the primary command. Dish assignment is the critical path; wish granting is best-effort.

**Push notification fan-out on grant** → If a wish has many voters, multiple notifications are sent synchronously within the command handler. For a family app with small membership, this is not a concern. Mitigation if needed: fire notifications as a background task.

**Self-vote extends decay but not visible in vote count** → If self-votes inflate the visible "+N others want this" count it could feel misleading. Decision: self-votes count fully toward both ranking and decay — the count is "total interest signals" not "other people who want this."

## Migration Plan

1. Deploy backend with new `Wishlist` container (auto-created by CosmosDB on first write, or provisioned via `PUT /api/migrate`)
2. Deploy frontend — `PlanTopDishes` is replaced by `PlanWishList`; no data migration needed (wish list starts empty)
3. No rollback risk — existing features untouched; the only modified write path (`AddDishToDinner`) degrades gracefully if wishlist service is unavailable

## Open Questions

- Should voters receive a notification when the original requester removes their own wish? (Probably no — silent removal is fine.)
- Should the add-wish search be accessible from the plan page sidebar directly (inline search), or always from the dish detail page? (Recommendation: both — a "+" button in the sidebar opens a dish-search dialog.)
