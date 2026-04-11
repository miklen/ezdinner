## ADDED Requirements

### Requirement: Family members can add a dish to the wish list
Any family member SHALL be able to add a dish from the catalog to the family wish list. The system SHALL enforce deduplication — only one wish entry per dish per family. If the dish is already on the wish list, the system SHALL return the existing wish so the member can upvote it instead.

#### Scenario: Adding a dish not yet on the wish list
- **WHEN** a family member submits a wish for a dish that has no active wish entry for that family
- **THEN** a new wish item is created with the member as requester, a vote count of 1, and an expiry of 14 days from now

#### Scenario: Adding a dish already on the wish list
- **WHEN** a family member attempts to add a dish that already has an active wish entry
- **THEN** the system returns the existing wish entry (409 Conflict response with wish ID)
- **THEN** no duplicate entry is created

#### Scenario: Adding a dish via quick-create
- **WHEN** a family member searches for a dish, finds no match, and creates a new dish via quick-create
- **THEN** the newly created dish is immediately added to the wish list as a new wish entry

### Requirement: Family members can upvote an existing wish
Any family member SHALL be able to upvote an existing wish entry, including their own (self-vote). Each member MAY upvote a given wish at most once. An upvote SHALL extend the wish's expiry to at least 14 days from the vote date.

#### Scenario: First-time upvote
- **WHEN** a family member upvotes a wish they have not previously voted on
- **THEN** the vote is recorded and the vote count increments by 1
- **THEN** `expiresAt` is updated to `max(current expiresAt, voteDate + 14 days)`

#### Scenario: Duplicate upvote attempt
- **WHEN** a family member attempts to upvote a wish they have already voted on
- **THEN** the system rejects the request (409 Conflict)
- **THEN** the vote count and expiry remain unchanged

#### Scenario: Self-vote by original requester
- **WHEN** the family member who added a wish upvotes it
- **THEN** the vote is recorded the same as any other upvote
- **THEN** the expiry is extended accordingly

### Requirement: Wish list is ranked by vote count
The system SHALL return wish items sorted by vote count descending. Items with equal vote counts SHALL be sorted by age ascending (oldest wish first) as a tiebreaker.

#### Scenario: Fetching the wish list
- **WHEN** any family member fetches the family wish list
- **THEN** all non-expired wishes are returned sorted by vote count descending, then by creation date ascending
- **THEN** wishes where `expiresAt < now` are excluded from the response

### Requirement: Wishes expire automatically after inactivity
A wish with no upvote activity for 14 days SHALL be treated as expired. Expired wishes SHALL NOT appear in the wish list. The system uses lazy evaluation — expiry is checked on read, not by a background job.

#### Scenario: Wish with no activity
- **WHEN** a wish item has `expiresAt < now` (no vote cast in the last 14+ days)
- **THEN** the wish is excluded from the `GET /wishlist` response
- **THEN** the wish is not shown in the plan page wish list panel

#### Scenario: Upvote resets expiry
- **WHEN** a family member upvotes a wish that would expire in 2 days
- **THEN** `expiresAt` is extended to `voteDate + 14 days`
- **THEN** the wish continues to appear in the list

### Requirement: Wishes are removed when the dish is planned
When a dish is added to a dinner and that dish has an active wish entry, the wish SHALL be automatically removed from the list.

#### Scenario: Planned dish matches an active wish
- **WHEN** a dish is assigned to a dinner day
- **THEN** the system checks whether that dish has an active wish for the family
- **THEN** if found, the wish entry is deleted
- **THEN** a "wish granted" push notification is sent to the original requester and all voters

#### Scenario: Planned dish has no active wish
- **WHEN** a dish is assigned to a dinner day and no active wish exists for that dish
- **THEN** the dinner assignment proceeds normally with no wishlist side effects

### Requirement: Family members can remove wishes they own
The family member who added a wish SHALL be able to remove it. Family owners SHALL be able to remove any wish.

#### Scenario: Requester removes their own wish
- **WHEN** the family member who added a wish requests its deletion
- **THEN** the wish entry is removed
- **THEN** no notification is sent

#### Scenario: Family owner removes any wish
- **WHEN** a family member with the Owner role requests deletion of any wish
- **THEN** the wish entry is removed
- **THEN** no notification is sent

#### Scenario: Non-owner attempts to remove another member's wish
- **WHEN** a family member without the Owner role attempts to delete a wish they did not add
- **THEN** the system returns 403 Forbidden
- **THEN** the wish remains on the list

### Requirement: Wish stats are tracked per family member
The system SHALL record wish activity per family member for future gamification use. Stats are not displayed in the UI in this iteration.

#### Scenario: Wish is added
- **WHEN** a family member adds a wish
- **THEN** the member's `wishesAdded` counter is incremented

#### Scenario: Wish is granted
- **WHEN** a wish is removed because its dish was planned
- **THEN** the original requester's `wishesGranted` counter is incremented
