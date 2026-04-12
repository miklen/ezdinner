## REMOVED Requirements

### Requirement: Suggestion bar displays per-day suggestions on the plan page
**Reason:** The `PlanSuggestionBar` component is retired. Its function — surfacing dishes the planner should consider for the current week — is superseded by the Planning Assistant panel, which provides a full browsable dish list with freshness, wish, and effort signals alongside AI-powered full-week draft generation.
**Migration:** Remove `PlanSuggestionBar` from `plan.vue`. The backend `GET /api/suggest/day` endpoint remains available and is unchanged; the frontend simply no longer renders the suggestion bar. The `suggest-week` backend endpoint remains available as a potential data source for the AI week planner.
