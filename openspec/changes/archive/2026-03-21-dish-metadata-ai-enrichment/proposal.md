## Why

The suggestion engine is blind to dish characteristics — it cannot distinguish a quick weeknight dish from an elaborate Sunday roast, a summer grilling dish from a winter stew, or a main course from a side dish. Without structured metadata on dishes, no effort-aware, seasonal, or role-aware scoring rules can exist. Manual tagging at scale is unrealistic; AI enrichment bootstraps the catalog automatically from dish names and notes, with the family retaining full control to review and correct inferences.

## What Changes

- `Dish` aggregate gains four structured metadata fields: `Roles` (set), `EffortLevel`, `SeasonAffinity`, `Cuisine`
- Each field carries an `IsConfirmed` flag — confirmed fields survive re-enrichment; unconfirmed fields are owned by AI
- New `POST /api/families/{familyId}/dishes/{dishId}/enrich` endpoint calls the AI enrichment provider and writes AI-suggested values
- AI enrichment is encapsulated behind an `IDishEnrichmentProvider` interface; the Anthropic/Claude implementation lives in Infrastructure and can be replaced by any other provider without touching domain or application logic
- New `UpdateDishMetadataCommand` allows users to set and confirm individual fields
- Frontend displays metadata on dish detail with visual distinction between AI-suggested and confirmed values
- After every dish save (create or edit), frontend automatically triggers enrichment
- Notes field in the dish form shows guidance hints for better enrichment results
- Catalog gains a "Review AI suggestions" view listing dishes with unconfirmed metadata

## Capabilities

### New Capabilities

- `dish-metadata`: Structured metadata fields (Roles, EffortLevel, SeasonAffinity, Cuisine) on dishes with AI-suggested vs confirmed state
- `dish-ai-enrichment`: AI enrichment endpoint that infers dish metadata from name and notes via a pluggable AI provider (default: Claude/Anthropic)

### Modified Capabilities

- `suggest-day`: Suggestion candidates filtered to dishes where `Main ∈ Roles` (when roles are populated)
- `suggest-week`: Suggestion candidates filtered to dishes where `Main ∈ Roles` (when roles are populated)

## Impact

- **Domain**: `Dish` aggregate gains metadata value object or individual fields with confirmation flags
- **Application**: `UpdateDishMetadataCommand`, `EnrichDishCommand` (calls Claude API)
- **API**: `POST /api/families/{familyId}/dishes/{dishId}/enrich`, `PATCH /api/families/{familyId}/dishes/{dishId}/metadata`
- **Infrastructure**: `AnthropicEnrichmentProvider` implements `IDishEnrichmentProvider` using Anthropic SDK; API key in Azure Function app settings; swapping provider requires only a new Infrastructure implementation and a registration change
- **Frontend**: Dish detail metadata display/edit, enrichment trigger after save, notes hint, bulk review UI
- **Suggestion engine**: Main-role filter added to candidate selection (non-breaking — dishes with no roles set remain eligible)
