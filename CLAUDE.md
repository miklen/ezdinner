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
- `AddPolicyAsync` handles both `p`-type (permissions) and `g`-type (role assignments) rules — `EFCoreAdapter` 2.4.0 has no separate `AddGroupingPolicyAsync` override.
- EF Core maps Casbin fields to shadow properties named `"Type"` and `"Value1"`–`"Value6"` (NOT `"PType"` / `"V0"`–`"V5"`)
- `PUT /api/migrate` seeds authorization policies for existing families — must be called after initial setup or schema changes
- Casbin `Enforcer` is a singleton that loads from DB at startup into an in-memory model. New rules added via `AddPolicyAsync` update both DB and the current instance's in-memory model, but other running instances remain stale until restarted. If a user has the correct rule in DB but gets 401, restart the function app.

## Non-Obvious Gotchas
- Azure Functions v4 isolated worker uses System.Text.Json — Newtonsoft `[JsonConverter]` attributes on model classes are silently ignored. Map NodaTime types to strings in AutoMapper using e.g. `LocalDatePattern.Iso.Format(s.Date)`.
- CosmosDB triggers in Azure Functions v4 isolated worker use STJ to deserialize the change feed payload. Domain classes with parameterized constructors and private-backed properties (like `Family`) cannot be bound by STJ. Use `IReadOnlyList<string>` as the trigger parameter type and deserialize manually with Newtonsoft.
- `IAsyncEnumerable<T>` returned via `OkObjectResult` serializes as `{}` with System.Text.Json — always `.ToListAsync()` before returning.
- Nuxt 3 auto-import prefixes components by folder, with deduplication: the folder prefix is prepended unless the filename already starts with it. `Plan/TopDishes.vue` → `<PlanTopDishes>`. `Dish/DatesVisualization.vue` → `<DishDatesVisualization>`. `Dish/DishPill.vue` → `<DishPill>` (NOT `<DishDishPill>` — "Dish" prefix deduplicated). Using the wrong name silently renders nothing.
- Vuetify 3 `v-timeline` with `density="compact"` + `side="end"` shrinks to content width (uses `auto` column, not `1fr`). Use a custom CSS timeline (`position: relative`, `::before` for vertical line) instead.
- EF Core 9 + CosmosDB: avoid LINQ queries with `.Any()` / existence checks — they generate invalid SQL. Use direct insert with conflict handling.
- `HasNoDiscriminator()` is required on `CasbinEntityConfiguration` for Cosmos — without it EF adds a discriminator field that breaks queries.
- `HasPartitionKey(p => p.Id)` must match the container's actual partition key path (`/id`) or CosmosDB rejects writes.
- Casbin.NET.Adapter.EFCore 2.4.0 requires Casbin.NET >= 2.19.1 (older 2.x versions cause NU1605 downgrade warning and runtime failures).
- Integration tests are in `api/tests/EzDinner.IntegrationTests` and hit a real CosmosDB — no mocking.
- The legacy Nuxt 2 app in the repo root is not the active frontend. Use `/web`.
- MSAL Browser v3 does not populate `idTokenClaims` on accounts returned by `getAllAccounts()` after a page reload. Call `acquireTokenSilent()` first to get a token response with fresh claims. See `web/plugins/msal.client.ts`.
- Vuetify 3 `v-rating` inter-icon spacing cannot be controlled via `:deep()` scoped CSS. Apply `style="gap: Npx"` directly on the `<v-rating>` element instead (it renders as `inline-flex`).
- Vuetify 3 `v-rating` `size` prop only controls the icon size, not the button wrapper. The wrapper `v-btn` retains its default `min-width` (36–64px), which causes rating rows to consume excessive width in flex layouts. Override with `:deep(.v-btn) { min-width: unset !important; width: auto !important; padding: 0 !important; }` in the parent component.
- `func-ezdinner-prod-02` requires all `AzureAdB2C:*` settings manually (Instance, TenantId, ClientId, Domain, SignUpSignInPolicyId, ClientSecret) — portal "Advanced edit" export from prod-01 may omit them. Refer to `api/src/EzDinner.Functions/local.settings.json` for the full list of required keys.
- Don't use `NuxtLink` (or `v-card :to`) as the outer container when the element also contains removable/clickable children. The entire `<a>` becomes a navigation trigger; `@click.stop` on children is unreliable. Instead wrap only the navigable text in `NuxtLink` and keep the outer element a plain `<span>` or `<div>`.
- `overflow-x: hidden` on `body` breaks Vuetify `position: fixed` overlays (tooltips, menus, dialogs) in some browsers. Use `overflow-x: clip` instead — prevents horizontal scroll without affecting fixed-position descendants.
- `v-tooltip` does not work reliably inside `v-navigation-drawer` in rail mode (Vuetify 3.7.x) — neither activator slot pattern nor `activator="parent"` shows a tooltip. Workaround: track `mouseenter`/`mouseleave` on a wrapper div, capture Y from `getBoundingClientRect()`, render a custom `position: fixed` tooltip outside the drawer.
- `@vueuse/core` is NOT a transitive dependency of Nuxt in this project — must `npm install @vueuse/core` explicitly before importing `useSwipe`, `useIntersectionObserver`, etc.
- Luxon `DateTime.toISODate()` returns `string | null` — use `.toFormat('yyyy-MM-dd')` when a guaranteed non-null string is required (e.g., `:key` bindings, URL params).
- `grid-template-rows: 0fr → 1fr` expand animation (smooth height reveal) belongs on the *wrapper* component, not the content component. The content component renders normally; the wrapper applies `overflow: hidden` + the transition.

## CI/CD
- `ci.yml` — lint + test `web/` on PR/push to main
- `azure-static-web-apps-*.yml` — deploys `web/` to Azure Static Web Apps
- `func-ezdinner-prod-02.yml` — deploys backend to `func-ezdinner-prod-02`
- SWA automatically creates preview environments for PRs targeting `main` (free tier limit: 3 concurrent staging envs)
- CI uses npm 11 (`npm install -g npm@11` step) — lockfile was generated with npm 11 and `npm ci` fails on Node 22's bundled npm 10
- `npx nuxt prepare` must run before `npm test` in CI to generate `.nuxt/tsconfig.json`
- `Azure/functions-action` latest is `v1` — `v2` does not exist
