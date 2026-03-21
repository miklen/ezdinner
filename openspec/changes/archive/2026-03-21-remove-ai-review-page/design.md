## Context

The dishes catalog currently shows a "Review AI suggestions" button in the header when any dish has unconfirmed AI-suggested metadata (roles, effort level, season affinity, cuisine). Clicking it navigates to a dedicated `/dishes/review` page that lists all pending dishes with their metadata cards inline.

This review flow was added as a discovery mechanism, but users already see and can edit AI suggestions directly on the dish detail page. The button adds noise to the catalog header and the page adds an unnecessary navigation step.

## Goals / Non-Goals

**Goals:**
- Remove the review page and button without affecting any other functionality
- Keep the existing per-dish metadata editing flow intact on the dish detail page

**Non-Goals:**
- Changing how AI enrichment works or is triggered
- Removing the `rolesConfirmed`, `effortLevelConfirmed`, `seasonAffinityConfirmed`, `cuisineConfirmed` flags from the domain model (they remain useful for the dish detail page)
- Any backend changes

## Decisions

**Delete `review.vue` outright** rather than hiding it behind a feature flag — the page has no other consumers and no route guards reference it. A clean delete is simpler than a flag.

**Remove `pendingReviewCount` entirely** — it exists solely to drive the button visibility. No other component reads it.

## Risks / Trade-offs

- Users who bookmarked `/dishes/review` will hit a 404. Acceptable: Nuxt will fall through to the default 404 page, and the URL was never linked from anywhere outside the catalog header.
- No rollback complexity — the change is a pure deletion with no data migration.
