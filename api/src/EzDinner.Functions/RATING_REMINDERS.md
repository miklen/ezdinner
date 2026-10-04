# Rating reminder API

All authenticated routes derive the owner from the token; bodies reject unknown properties, including client-supplied user IDs. Family routes require Family/Read, Dish/Read (or Dish/Update for dismissal), and current autonomous membership before accessing reminder state. Dismissals also verify dish-family identity.

| Request | Successful response |
| --- | --- |
| `GET /api/families/{familyId}/rating-reminders` | `200 {"today":"2026-10-12","reminders":[{"dishId":"<guid>","dishName":"Lasagne","dinnerDate":"2026-10-11"}]}` |
| `PUT /api/families/{familyId}/rating-reminders/{dishId}/dismiss` with `{"dinnerDate":"2026-10-11"}` | `204` |
| `GET /api/rating-reminders/preferences` | `200 {"pushEnabled":false}` for missing state |
| `PUT /api/rating-reminders/preferences` with `{"pushEnabled":true}` | `200 {"pushEnabled":true}` after conditional persistence |

Dates are ISO calendar strings and eligibility uses Copenhagen, including DST. No state is created by reads. Saving the existing default-off preference is idempotent. A removed/opted-out menu occurrence is acknowledged without suppression; rating, archive or deletion also resolves a stale card without a new dismissal. New dismissal dates must lie from today minus seven through yesterday. Dismissal stores a family/dish cutoff, so earlier dates do not reappear; later dates remain eligible.

Unauthenticated/inaccessible families return `401`. Invalid IDs, dates, missing/wrong-typed fields and unknown body properties return `400` with the existing machine-readable error-code convention. A dish outside the requested family returns `400 DISH_NOT_FOUND`. Commands reload and reapply at most four times on conditional conflicts; exhausted retries return `409 RATING_REMINDER_CONFLICT`, without silently overwriting another action.

`PUT /api/migrate` provisions `RatingReminders` with partition path `/id`. Document ID and partition key both equal the user ID. No backfill or new Casbin policies are required. Deploy/provision this container before exposing reminder routes. Storage uses point read, create-if-absent and ETag replacement; no unconditional upsert is available in the contract.

`RatingReminderApiTests` exercises the ISO response/body fixtures, defaults, caller identity, membership checks, malformed bodies, stale actions and conflict retries. `RatingRemindersRepositoryTests` verifies storage outcomes and serialization with the production Cosmos serializer. `RatingRemindersPersistenceTests` requires a live Cosmos emulator and covers reload and competing writes; an unrun emulator check is not proof of persistence correctness.
