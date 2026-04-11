## ADDED Requirements

### Requirement: Wish upvote triggers notification to original requester
When a family member upvotes an existing wish, the system SHALL send a push notification to the family member who originally added the wish, provided they have an active push subscription.

#### Scenario: Upvote notification sent
- **WHEN** a family member upvotes a wish
- **THEN** a push notification is sent to the original wish requester (if subscribed)
- **THEN** the notification body identifies who upvoted and names the dish (e.g. "Lucas also wants Tacos!")
- **THEN** no notification is sent if the requester has no active push subscription

#### Scenario: Requester self-votes
- **WHEN** the original requester upvotes their own wish
- **THEN** no notification is sent (self-notification would be noise)

### Requirement: Wish granted triggers notification to requester and voters
When a dish is planned and its wish entry is removed, the system SHALL send a push notification to the original requester and all family members who upvoted the wish, provided they have active push subscriptions.

#### Scenario: Wish granted notification sent
- **WHEN** a dish is assigned to a dinner day and an active wish for that dish is found
- **THEN** a push notification is sent to the original requester (if subscribed)
- **THEN** a push notification is sent to each voter on the wish (if subscribed)
- **THEN** the notification body announces the dish is on the menu (e.g. "Tacos is on the menu this week! 🎉")
- **THEN** duplicate notifications are avoided if the requester is also a voter

#### Scenario: No subscribed recipients for wish granted
- **WHEN** a wish is granted but neither the requester nor any voter has an active push subscription
- **THEN** no notifications are sent and the wish is still removed from the list
