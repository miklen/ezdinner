## MODIFIED Requirements

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

### Requirement: Family members can upvote or retract their vote
Any family member SHALL be able to upvote a wish they have not yet voted on. A member who has already voted SHALL be able to retract their vote. Retracting a vote decrements the vote count but does not affect the wish expiry.

#### Scenario: First-time upvote
- **WHEN** a family member upvotes a wish they have not previously voted on
- **THEN** the vote is recorded and the vote count increments by 1
- **THEN** `expiresAt` is updated to `max(current expiresAt, voteDate + 14 days)`

#### Scenario: Retracting a vote
- **WHEN** a family member clicks the upvote button on a wish they have already voted on
- **THEN** their vote is removed and the vote count decrements by 1
- **THEN** the wish expiry is NOT changed
- **THEN** the upvote button returns to its unvoted state

#### Scenario: Duplicate upvote attempt (via API)
- **WHEN** a family member calls the upvote endpoint for a wish they have already voted on
- **THEN** the system rejects the request (409 Conflict)

#### Scenario: Retract non-existent vote (via API)
- **WHEN** a family member calls the remove-upvote endpoint for a wish they have not voted on
- **THEN** the system rejects the request (409 Conflict)

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

## ADDED Requirements

### Requirement: Design system provides RGB variants for error and accent colors
The design token sheet SHALL define `--color-error-rgb` and `--color-accent-rgb` CSS custom properties so that component styles can use `rgba()` with these colors.

#### Scenario: Error hover background on remove button
- **WHEN** the user hovers over a remove button
- **THEN** the background SHALL render as a low-opacity tint of the error color (valid CSS)

#### Scenario: Accent rgba usage in upvote hover
- **WHEN** the user hovers over an unvoted upvote button
- **THEN** the background SHALL render as a low-opacity tint of the accent color (valid CSS)
