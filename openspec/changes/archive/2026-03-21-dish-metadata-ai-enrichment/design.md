## Context

EzDinner's suggestion engine scores dishes using four rules (overdue, rating, recency penalty, leftover pattern). None of these rules know anything about a dish's characteristics — whether it's quick to make, seasonal, or even a main course vs a side dish. Side dishes appear as top suggestions. Winter stews appear in summer. The engine has hit a ceiling that only structured dish metadata can break.

The Anthropic Claude API (`claude-haiku-4-5-20251001`) is well-suited for this: it understands Danish food culture and dish names natively, can infer effort, season, roles, and cuisine from a dish name alone, and is cheap enough for per-dish enrichment calls.

## Goals / Non-Goals

**Goals:**
- Add `Roles`, `EffortLevel`, `SeasonAffinity`, `Cuisine` to `Dish` aggregate with per-field confirmation flags
- AI enrichment endpoint (`POST .../enrich`) that calls the configured AI enrichment provider and writes unconfirmed field values
- `IDishEnrichmentProvider` interface in Application layer; `AnthropicEnrichmentProvider` in Infrastructure — provider is swappable at the registration boundary
- `UpdateDishMetadataCommand` for user-driven field updates (always confirms)
- Frontend metadata display/edit UI with AI-suggested vs confirmed distinction
- Auto-trigger enrichment after dish save; visible loading state
- Main-role filter in suggestion candidates (non-breaking for unenriched dishes)

**Non-Goals:**
- Bulk enrichment of the entire catalog in one operation (will be addressed via the "Review AI suggestions" UI — user triggers per-dish)
- Weather-based scoring (out of scope)
- Effort-aware scoring rules (those come in Change 3+4, depend on this metadata existing)
- Using metadata for scoring rules in this change (metadata only; rules are Change 3+4)

## Decisions

### D1: Metadata stored directly on Dish aggregate, not a separate document
Keeping metadata co-located with the dish simplifies reads — no join or second fetch. CosmosDB's document model handles heterogeneous fields naturally. A separate `DishMetadata` document would add complexity for no query benefit given single-family access patterns.

### D2: Per-field confirmation flags, not a global "confirmed" state
Users may confirm some fields immediately while leaving others for AI to manage. A global confirmed flag would require users to re-review all fields together. Per-field flags give fine-grained control and let re-enrichment selectively update only what hasn't been validated.

### D3: Enrichment as an explicit separate endpoint, not inline with dish save
Dish save remains fast and synchronous. Enrichment is a separate, explicitly triggered call. The frontend calls it automatically after save, showing a loading state — making enrichment visible without blocking the save flow. This also allows enrichment to be re-triggered manually (e.g., after the user updates notes).

Alternative considered: Cosmos DB change feed trigger. Rejected because: adds async complexity, harder to show loading state in frontend, change feed triggers fire on every field change (not just name/notes), and the current app architecture doesn't use change feed triggers for business logic.

### D4: Claude Haiku for enrichment
`claude-haiku-4-5-20251001` is the right model: fast, cheap, sufficient for structured extraction from short text. The prompt asks for a structured JSON response with the four fields. Using `claude-sonnet-4-6` would be slower and more expensive for what is essentially a classification task.

### D8: AI provider abstraction via `IDishEnrichmentProvider`
The AI provider (Anthropic, OpenAI, Azure OpenAI, etc.) is an infrastructure concern. The Application layer defines `IDishEnrichmentProvider` with a single method: `EnrichAsync(dishName, notes) → DishEnrichmentResult`. `EnrichDishCommandHandler` depends only on this interface. The concrete `AnthropicEnrichmentProvider` lives in `EzDinner.Infrastructure` and is registered via DI.

Switching providers in the future requires: (1) a new Infrastructure class implementing the interface, (2) a DI registration change. Zero changes to domain or application logic. The interface contract is model-agnostic — it accepts text input and returns structured metadata.

`DishEnrichmentResult` is a plain value type (roles, effort, season, cuisine) defined in Application. It carries no SDK types. The provider implementation is responsible for mapping its SDK response to this type.

### D5: Roles as a set (IReadOnlySet or IEnumerable stored as array)
`Roles` must support multiple values (e.g., `[Main, Dessert]` for pandekager). A single enum cannot represent this. Stored as an array in CosmosDB, exposed as a collection in the domain. Suggestion filtering checks `roles.Contains(DishRole.Main)` or `roles is empty`.

### D6: Main-role filter is non-breaking (empty roles = eligible)
Dishes created before enrichment have no roles. If the filter excluded all unenriched dishes, suggestions would break immediately after deployment. The filter only excludes dishes where roles are explicitly set and `Main` is not among them. This gives the catalog time to be enriched without degrading suggestions.

### D7: Claude API key in Azure Function app settings
The key is never sent to or stored by the frontend. The enrichment endpoint is authenticated via Casbin (`Dish.Write` on the family). The Function app reads `Anthropic:ApiKey` from app settings (equivalent of environment variable). No Azure Key Vault required for this use case given the app's current infrastructure posture.

## Risks / Trade-offs

- **[Risk] Claude infers incorrect metadata for niche dishes** → Mitigation: all fields start as AI-suggested (unconfirmed); users can correct via the dish detail UI. The "Review AI suggestions" catalog view makes bulk review easy.
- **[Risk] Enrichment adds a second API call after every dish save** → Mitigation: enrichment is fire-and-forget from the user's perspective; the dish is already saved. A failed enrichment leaves the dish without metadata (not broken). Users can retry via the "Review AI suggestions" view.
- **[Risk] Per-field confirmed flags increase domain complexity** → Mitigation: encapsulate in a `DishMetadata` value object within the aggregate. The flags are simple booleans; the logic (don't overwrite confirmed fields) lives in one place.
- **[Risk] Prompt injection via dish notes** → Mitigation: the enrichment prompt is structured ("return only JSON in this schema") and the response is parsed strictly. Unexpected output is treated as an enrichment failure, not executed.

## Migration Plan

- `IsArchived`-equivalent: no migration — new fields default to null/empty, existing dishes remain eligible for suggestions (D6)
- Deploy backend (new fields, new endpoints) first
- Deploy frontend (metadata UI, auto-enrichment trigger) after backend is live
- Users enriching dishes going forward; no forced batch migration

## Open Questions

- Should the "Review AI suggestions" view show a count badge on the catalog navigation item (e.g., "12 dishes need review")? Deferred to frontend implementation.
- Should failed enrichment be retried automatically, or only on user action? Recommend user-action-only for simplicity in this change.
