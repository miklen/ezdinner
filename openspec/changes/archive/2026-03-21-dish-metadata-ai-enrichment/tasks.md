## 1. Domain Layer

- [x] 1.1 Define `DishRole` enum: `Main`, `Side`, `Dessert`, `Other`
- [x] 1.2 Define `EffortLevel` enum: `Quick`, `Medium`, `Elaborate`
- [x] 1.3 Define `SeasonAffinity` enum: `Summer`, `Winter`, `Spring`, `Autumn`, `AllYear`
- [x] 1.4 Add `DishMetadata` value object to `Dish` aggregate: `Roles` (set), `EffortLevel`, `SeasonAffinity`, `Cuisine` — each with a `IsConfirmed` flag
- [x] 1.5 Add `Dish.UpdateMetadata(DishMetadata metadata)` method that merges incoming fields, skipping confirmed fields when called from enrichment
- [x] 1.6 Add unit tests: confirmed fields not overwritten, unconfirmed fields updated, multi-role assignment

## 2. Application Layer — Metadata Command

- [x] 2.1 Create `UpdateDishMetadataCommand` with `FamilyId`, `DishId`, and individual nullable metadata fields
- [x] 2.2 Create `UpdateDishMetadataCommandHandler` — load dish, call `UpdateMetadata`, save; all updated fields marked confirmed

## 3. Application Layer — Enrichment Command + Provider Interface

- [x] 3.1 Define `IDishEnrichmentProvider` interface in `EzDinner.Application` with method `EnrichAsync(string dishName, string? notes, CancellationToken ct) → DishEnrichmentResult`
- [x] 3.2 Define `DishEnrichmentResult` value type in `EzDinner.Application`: `Roles`, `EffortLevel`, `SeasonAffinity`, `Cuisine` — no SDK types, all nullable to represent "not inferred"
- [x] 3.3 Create `EnrichDishCommand` with `FamilyId` and `DishId`
- [x] 3.4 Create `EnrichDishCommandHandler` — depends on `IDishEnrichmentProvider`:
  - Load dish by `FamilyId` + `DishId`
  - Call `_provider.EnrichAsync(dish.Name, dish.Notes)`
  - Call `Dish.UpdateMetadata()` with AI-inferred values (unconfirmed)
  - Save dish
- [x] 3.5 Handle enrichment failure gracefully — log and return error without modifying dish
- [x] 3.6 Add `Anthropic.SDK` NuGet package to `EzDinner.Infrastructure`
- [x] 3.7 Create `AnthropicEnrichmentProvider` in `EzDinner.Infrastructure` implementing `IDishEnrichmentProvider`:
  - Build Claude prompt with dish name and notes; instruct to return JSON with `roles`, `effortLevel`, `seasonAffinity`, `cuisine`; instruct to handle Danish dish names
  - Call `claude-haiku-4-5-20251001`, parse structured JSON response
  - Map response to `DishEnrichmentResult`
- [x] 3.8 Register `AnthropicEnrichmentProvider` as `IDishEnrichmentProvider` in DI; bind `Anthropic:ApiKey` from app settings

## 4. API Layer (Azure Functions)

- [x] 4.1 Create `EnrichDish` function — `POST /api/families/{familyId}/dishes/{dishId}/enrich`
- [x] 4.2 Create `UpdateDishMetadata` function — `PATCH /api/families/{familyId}/dishes/{dishId}/metadata`
- [x] 4.3 Add Casbin `Dish.Write` authorization to both endpoints
- [x] 4.4 Add `Anthropic:ApiKey` to `local.settings.json` template and Azure Function app settings documentation (consumed by `AnthropicEnrichmentProvider` only — future providers use their own settings keys)
- [x] 4.5 Expose metadata fields and confirmed flags on dish query response DTO

## 5. Suggestion Engine — Role Filter

- [x] 5.1 Add role-based candidate filter in `DinnerSuggestionService`: exclude dishes where `Roles` is non-empty and does not contain `Main`
- [x] 5.2 Add unit tests: side-dish excluded, main-dish included, unenriched dish (no roles) included, multi-role dish with Main included

## 6. Frontend — Dish Detail Metadata UI

- [x] 6.1 Add metadata section to dish detail page: display `Roles`, `EffortLevel`, `SeasonAffinity`, `Cuisine`
- [x] 6.2 Show AI-suggested indicator (e.g., sparkle icon + muted style) on unconfirmed fields
- [x] 6.3 Allow inline editing of each metadata field (dropdown for enums, text for Cuisine, multi-select for Roles)
- [x] 6.4 Saving a metadata field calls `PATCH .../metadata` and marks field as confirmed in UI
- [x] 6.5 Update dish Pinia store to include metadata fields and confirmed flags

## 7. Frontend — Enrichment Trigger + Loading State

- [x] 7.1 After successful dish create, call `POST .../enrich` and show "Analyzing dish..." indicator
- [x] 7.2 After successful dish edit (name or notes changed), call `POST .../enrich`
- [x] 7.3 Update displayed metadata fields when enrichment response returns
- [x] 7.4 Handle enrichment failure gracefully — dismiss loading state, show non-blocking error notice

## 8. Frontend — Notes Guidance Hint

- [x] 8.1 Add hint text beneath the notes field in the dish form: "Mention prep time, season, and main ingredients for better suggestions"

## 9. Frontend — Bulk Review UI

- [x] 9.1 Add "Review AI suggestions" entry to catalog navigation or catalog view
- [x] 9.2 List all dishes with at least one unconfirmed metadata field
- [x] 9.3 Allow inline confirmation or editing of metadata per dish in the review list
- [x] 9.4 Show count of dishes pending review (badge or subtitle)
