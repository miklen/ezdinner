# Home quick rating

Home shows one recent, personally unrated dish after the dinner hero and before tomorrow. Only the dish name opens the existing detail route. Heart selection saves immediately through the existing personal rating endpoint; Not now persists the occurrence cutoff. The card advances after confirmed persistence only. Read failures have their own retry; failed actions retain the selected rating for retry. A successful save remains successful if the follow-up read fails.

The queue is local to the mounted feature, identity-aware and read-only to callers. Family/account switches clear it and discard stale reads/writes. Visibility/focus and Copenhagen midnight refresh eligibility, with listeners/timers removed on disposal. Expired dates retire even when refresh fails. An empty queue hides the card: no count, overdue message or historical backlog.

The shared DishRating preserves default read-only display, five hearts and half increments; editable uses support keyboard arrows and pending disabling. Existing family rows keep self/dependent edit permissions. English and Danish labels/date context update with the locale. Only actions started with focus inside the card restore focus after progression; unrelated activity retains focus. Live text announces saves and the next dish.

Unit/component tests use the real Vuetify heart control. Vitest inlines Vuetify so its CSS can be transformed in tests; jsdom supplies test-only observer boundaries. Browser verification of actual mobile/desktop layout and touch targets remains separately required.
