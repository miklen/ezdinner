# Rating reminders

The caller supplies Copenhagen `LocalDate today`; Core never reads a clock or performs I/O.

Selection uses the current family plan from today minus seven through yesterday, inclusively. Empty and opted-out dinners, unavailable dishes, archived/deleted dishes, and every recorded personal rating (including zero) are excluded. Another member's rating has no effect.

Group eligible occurrences by dish and retain the latest dinner date **before** applying dismissal. Return newest date first, breaking ties with ordinal `Guid.ToString("D")` dish identity. A retroactive replacement changes eligibility immediately on refresh according to the dinner date; editing time is irrelevant.

An occurrence is `(familyId, dishId, dinnerDate)`. A personal dismissal is a cutoff for that family/dish: all dates through the dismissed date are suppressed, including a removed/restored occurrence. A later date reappears. Different dishes, families and users remain independent.

The per-user aggregate defaults push off. Attempts are reserved before transport, at most once per occurrence and once per Copenhagen day across families. A crash, cancellation after reservation or uncertain transport result consumes the budget. Push history never suppresses Home. Conditional persistence must arbitrate competing reservations; successful local mutation alone does not authorize sending.

Prune dismissals and attempts with occurrence dates older than today minus seven during successful writes. Preserve preference and latest attempted day even after pruning, so retirement cannot reset today's cap. Reads of missing state use `CreateNew` without writing; persistence reconstruction uses `Hydrate` and copies collections.

Direct tests live in `api/test/EzDinner.UnitTests/RatingReminderTests`, including inclusive boundaries, personal zero ratings, latest-before-suppression ordering, retroactive replacements, persisted-state reconstruction and daily/occurrence limits.
