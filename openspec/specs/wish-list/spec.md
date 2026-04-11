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

### Requirement: Family members can upvote or retract their vote on an existing wish
Any family member SHALL be able to upvote an existing wish entry, including their own (self-vote). Each member MAY upvote a given wish at most once. An upvote SHALL extend the wish's expiry to at least 14 days from the vote date. A member who has already voted SHALL be able to retract their vote; retracting decrements the vote count but does not affect the wish expiry.

#### Scenario: First-time upvote
- **WHEN** a family member upvotes a wish they have not previously voted on
- **THEN** the vote is recorded and the vote count increments by 1
- **THEN** `expiresAt` is updated to `max(current expiresAt, voteDate + 14 days)`

#### Scenario: Retracting a vote
- **WHEN** a family member calls the remove-upvote endpoint for a wish they have already voted on
- **THEN** their vote is removed and the vote count decrements by 1
- **THEN** the wish expiry is NOT changed

#### Scenario: Duplicate upvote attempt (via API)
- **WHEN** a family member calls the upvote endpoint for a wish they have already voted on
- **THEN** the system rejects the request (409 Conflict)
- **THEN** the vote count and expiry remain unchanged

#### Scenario: Retract non-existent vote (via API)
- **WHEN** a family member calls the remove-upvote endpoint for a wish they have not voted on
- **THEN** the system rejects the request (409 Conflict)

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

### Requirement: Design system provides RGB variants for error and accent colors
The design token sheet SHALL define `--color-error-rgb` and `--color-accent-rgb` CSS custom properties so that component styles can use `rgba()` with these colors for low-opacity tint backgrounds.

#### Scenario: Error hover background on remove button
- **WHEN** the user hovers over a remove button
- **THEN** the background SHALL render as a low-opacity tint of the error color (valid CSS via `rgba(var(--color-error-rgb), ...)`)

#### Scenario: Accent rgba usage in upvote hover
- **WHEN** the user hovers over an unvoted upvote button
- **THEN** the background SHALL render as a low-opacity tint of the accent color (valid CSS via `rgba(var(--color-accent-rgb), ...)`)

### Requirement: Wish list displays vote indicators in accent color
Vote-related UI elements — the star icon next to the vote count and the upvoted state of the upvote button — SHALL use the accent color (`--color-accent`) rather than the primary color, keeping wish indicators visually consistent with rating hearts throughout the app.

#### Scenario: Vote count star color
- **WHEN** the wish list is rendered with one or more wishes
- **THEN** the star icon next to the vote count SHALL appear in the accent color, not the primary green

#### Scenario: Upvoted button state color
- **WHEN** the current user has already upvoted a wish
- **THEN** the upvote button icon SHALL appear in the accent color, not the primary green

#### Scenario: Upvote hover color
- **WHEN** the user hovers over an upvote button they have not yet voted on
- **THEN** the hover highlight SHALL use the accent color

### Requirement: Vote count has sufficient visual weight
The vote count displayed alongside each wish SHALL use a font size and weight that reflects its role as the primary sort signal, not secondary metadata.

#### Scenario: Vote count legibility
- **WHEN** the wish list is rendered
- **THEN** the vote count SHALL be displayed at body-small size or larger with bold weight

### Requirement: Wished dish name navigates to dish detail
The dish name displayed in each wish row SHALL be a navigable link to the dish detail page at `/dishes/{dishId}`. Only the dish name text is wrapped in the link — upvote and remove buttons are not affected.

#### Scenario: Clicking dish name navigates to detail
- **WHEN** a family member clicks the dish name in a wish row
- **THEN** the app navigates to `/dishes/{dishId}` for that wish
- **THEN** the navigation does not trigger the upvote or remove actions

#### Scenario: Link does not wrap interactive siblings
- **WHEN** a wish row renders the dish name link alongside upvote and remove buttons
- **THEN** only the dish name text is wrapped in the link, not the entire row

### Requirement: Empty wish list shows an inline add CTA
When the wish list has no items, the empty state SHALL include a button that opens the add-wish dialog directly within the empty state area, in addition to the existing header button. The empty state elements SHALL animate in with a stagger.

#### Scenario: Empty state CTA opens dialog
- **WHEN** the wish list is empty and the user clicks the add button in the empty state
- **THEN** the add-wish dialog opens

#### Scenario: Empty state stagger animation
- **WHEN** the empty state is displayed
- **THEN** the icon, text, and button SHALL appear sequentially with a short delay between each
- **THEN** the animation is suppressed when `prefers-reduced-motion` is set
