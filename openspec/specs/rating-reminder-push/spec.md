# rating-reminder-push Specification

## Purpose

Provide optional, restrained push reminders for recent unrated planned dishes and take the recipient directly to their existing personal rating control.

## Requirements

### Requirement: Rating reminders are evaluated the following afternoon
The system SHALL evaluate rating push reminders once daily in the afternoon using Europe/Copenhagen time, with daylight-saving changes respected. Delivery SHALL use the same seven-day personal eligibility, latest-occurrence deduplication, and dismissal rules as Home, additionally excluding occurrences already attempted for push. Today and future dinners SHALL be excluded. Retroactive edits SHALL be considered at the next scheduled run while their dinner date remains eligible. Push delivery SHALL NOT remove the Home card.

#### Scenario: Yesterday's dinner remains unrated
- **WHEN** the daily afternoon run finds an eligible unrated dish from yesterday, an active subscription, and rating reminders enabled
- **THEN** the dish can be selected for a rating reminder
- **AND** its Home card remains eligible until rated or dismissed

#### Scenario: Several eligible dishes
- **WHEN** multiple dishes qualify for push
- **THEN** only the most recent eligible dish without a prior push attempt is selected
- **AND** other eligible dishes remain available through Home

#### Scenario: Retroactive edit after today's run
- **WHEN** a recent past dinner is updated after today's delivery run
- **THEN** the edit sends no immediate notification
- **AND** the new eligible dish can be considered tomorrow if still within the seven-day window

### Requirement: Delivery limits survive retries and concurrent runs
The system SHALL durably limit rating-reminder push attempts to at most one per authenticated recipient per Copenhagen calendar day across families and once per family/dish/dinner-date occurrence. Repeated or concurrent scheduler calls, subscription replacement, and process restarts SHALL NOT reset these limits. A later dinner date SHALL be a new occurrence. When the result of a push attempt is uncertain, the system SHALL NOT resend that occurrence automatically.

#### Scenario: Scheduler retries or overlaps
- **WHEN** two delivery executions process the same recipient on the same date
- **THEN** at most one execution makes a rating-reminder push attempt for that recipient

#### Scenario: Occurrence already attempted
- **WHEN** the next daily run finds a still-unrated occurrence whose push was previously attempted
- **THEN** that occurrence is not pushed again
- **AND** a different eligible occurrence can be selected subject to the daily limit

#### Scenario: Later occurrence after Not now
- **WHEN** a dismissed dish appears on a later past dinner date, remains unrated, and reminders are enabled
- **THEN** that later occurrence can trigger a new push subject to the delivery limits

#### Scenario: Uncertain send result
- **WHEN** a push attempt times out or the process stops after recording the attempt
- **THEN** retrying delivery does not send another push for that occurrence
- **AND** the Home rating action remains available unless rated or dismissed

### Requirement: Delivery rechecks personal eligibility and recipient access
Immediately before attempting delivery, the system SHALL recheck the recipient's rating-reminder preference, active subscription, family access, dish availability, personal rating, and dismissal. A rating, dismissal, opt-out, or relevant plan change completed before that final check SHALL suppress delivery. Invalid subscriptions SHALL use the established cleanup behavior, and one recipient's failure SHALL NOT stop processing other recipients.

#### Scenario: User rates or dismisses before delivery
- **WHEN** the user rates or dismisses the occurrence before the final delivery eligibility check
- **THEN** no push is sent for that occurrence

#### Scenario: Membership or dish changes
- **WHEN** the user has left the family, the dish is removed from that dinner, or the dish is deleted or archived before the final check
- **THEN** that reminder is not delivered

#### Scenario: Expired subscription
- **WHEN** a push attempt returns an established stale-subscription response
- **THEN** the subscription is removed and other recipients continue to be processed

### Requirement: Reminder notification opens the user's existing rating row
The rating push SHALL identify the dish and its past menu date using the recipient's supported language, without asserting that it was eaten. Tapping SHALL open the normal dish detail route for the intended family, bring the authenticated user's editable heart-rating row into view after loading, and make that row easy to identify. The destination SHALL survive sign-in and work when an existing app window is showing another dish or family.

#### Scenario: App is closed
- **WHEN** the recipient taps the rating notification
- **THEN** the app opens the targeted dish in the correct accessible family
- **AND** brings their editable rating row into view after loading

#### Scenario: Existing app window shows another dish
- **WHEN** a rating notification is tapped while the app displays a different dish
- **THEN** the app navigates to and loads the notification's dish instead of merely focusing the previous page

#### Scenario: Sign-in is needed
- **WHEN** the recipient taps a notification while signed out
- **THEN** sign-in is presented and its successful completion resumes the intended dish and rating row
- **AND** only the newly authenticated user's own rating is editable

#### Scenario: Notification is no longer relevant
- **WHEN** the occurrence was rated, dismissed, changed, or expired after the notification arrived
- **THEN** opening it does not recreate a pending reminder or automatically change a rating
- **AND** the normal accessible dish page can still be viewed and edited

#### Scenario: Destination is no longer accessible
- **WHEN** the family or dish cannot be accessed by the signed-in user
- **THEN** the app presents a localized unavailable message and an accessible way back to Home
- **AND** no unauthorized family is selected and no rating is changed

### Requirement: Rating delivery has an independently secured scheduled trigger
The system SHALL expose `POST /api/push/send-rating-reminders` for an external daily scheduler and SHALL require the configured push shared-secret header. Invalid or missing secrets SHALL deny execution without reading or changing recipients' reminder state. A successful authorized run SHALL process eligible recipients subject to durable delivery limits.

#### Scenario: Scheduler is authorized
- **WHEN** the delivery endpoint receives the correct configured `X-Push-Secret` header
- **THEN** it runs rating-reminder delivery and returns success after processing recipients

#### Scenario: Scheduler is unauthorized
- **WHEN** the delivery endpoint receives a missing or invalid secret
- **THEN** it denies execution without attempting any reminder delivery
