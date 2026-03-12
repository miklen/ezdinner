# EzDinner — Claude Code Guide

## Project Overview
EzDinner is a family dinner planning app. Users belong to families and plan weekly dinners from a dish catalog. Multi-tenant by family, with per-family RBAC authorization.

## Structure
```
/               # Legacy Nuxt 2 frontend (not actively developed)
/web            # Active Nuxt 3 frontend (TypeScript, Pinia, Vuetify 3)
/api            # .NET 10 Azure Functions backend (Clean Architecture)
  /src
    EzDinner.Functions          # HTTP-triggered Azure Functions (entry points)
    EzDinner.Application        # Use cases / commands
    EzDinner.Core               # Domain aggregates (DDD)
    EzDinner.Infrastructure     # CosmosDB repos, Casbin adapter, EF Core
    EzDinner.Authorization.Core # Casbin RBAC engine wrapper
    EzDinner.Query.Core         # Read-only query interfaces
  /tests
    EzDinner.IntegrationTests   # Integration tests (require live CosmosDB emulator)
```

## Running Locally

**Backend** (from `api/src/EzDinner.Functions/`):
```bash
func start
```
Runs at http://localhost:7071. Requires Azurite and `local.settings.json` with CosmosDB + B2C config.

**Frontend** (from `web/`):
```bash
npm run dev
```
Runs at http://localhost:3000.

## Key Commands
```bash
# Backend build only
cd api/src/EzDinner.Functions && dotnet build

# Run integration tests (requires CosmosDB emulator)
cd api && dotnet test

# Frontend tests
cd web && npm test

# Frontend lint
cd web && npm run lint
```

## Tech Stack
- **Frontend**: Nuxt 3, Vue 3, Pinia, Vuetify 3, TypeScript, MSAL Browser 3
- **Backend**: .NET 10, Azure Functions v4 (isolated worker), EF Core 9, Cosmos DB
- **Auth**: Azure AD B2C (MSAL on frontend, Microsoft.Identity.Web on backend)
- **Authorization**: Casbin.NET 2.19.1 with RBAC-with-domains model, EFCore adapter 2.4.0
- **Database**: Azure Cosmos DB; local dev uses Azurite emulator
- **Date/Time**: NodaTime `LocalDate` (no time zones — dinners are calendar dates)

## Authorization Architecture
- Casbin RBAC-with-domains: families are domains, users get roles (Owner, FamilyMember) per family
- Policies stored in `CasbinRulesV2` CosmosDB container (partition key: `/id`)
- `CasbinCosmosAdapter` overrides `AddPolicyAsync` to bypass a broken EF Core 9 LINQ existence check that generates invalid CosmosDB SQL (`Identifier 'root' could not be resolved`). Uses direct insert + conflict catch instead.
- EF Core maps Casbin fields to shadow properties named `"Type"` and `"Value1"`–`"Value6"` (NOT `"PType"` / `"V0"`–`"V5"`)
- `PUT /api/migrate` seeds authorization policies for existing families — must be called after initial setup or schema changes

## Non-Obvious Gotchas
- Azure Functions v4 isolated worker uses System.Text.Json — Newtonsoft `[JsonConverter]` attributes on model classes are silently ignored. Map NodaTime types to strings in AutoMapper using e.g. `LocalDatePattern.Iso.Format(s.Date)`.
- `IAsyncEnumerable<T>` returned via `OkObjectResult` serializes as `{}` with System.Text.Json — always `.ToListAsync()` before returning.
- Nuxt 3 auto-import prefixes components by folder: `components/Plan/TopDishes.vue` → `<PlanTopDishes>`. Using the short name silently renders nothing.
- Vuetify 3 `v-timeline` with `density="compact"` + `side="end"` shrinks to content width (uses `auto` column, not `1fr`). Use a custom CSS timeline (`position: relative`, `::before` for vertical line) instead.
- EF Core 9 + CosmosDB: avoid LINQ queries with `.Any()` / existence checks — they generate invalid SQL. Use direct insert with conflict handling.
- `HasNoDiscriminator()` is required on `CasbinEntityConfiguration` for Cosmos — without it EF adds a discriminator field that breaks queries.
- `HasPartitionKey(p => p.Id)` must match the container's actual partition key path (`/id`) or CosmosDB rejects writes.
- Casbin.NET.Adapter.EFCore 2.4.0 requires Casbin.NET >= 2.19.1 (older 2.x versions cause NU1605 downgrade warning and runtime failures).
- Integration tests are in `api/tests/EzDinner.IntegrationTests` and hit a real CosmosDB — no mocking.
- The legacy Nuxt 2 app in the repo root is not the active frontend. Use `/web`.

## CI/CD
- `ci.yml` — lint + test `web/` on PR/push to main
- `azure-static-web-apps-*.yml` — deploys `web/` to Azure Static Web Apps
- `func-ezdinner-prod-01.yml` — deploys backend to Azure Functions
