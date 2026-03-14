# EzDinner — Easy Dinner Planner

A family dinner planning app. Users belong to families and plan weekly dinners from a dish catalog. Multi-tenant by family with per-family RBAC authorization.

## Tech Stack

- **Frontend**: Nuxt 3, Vue 3, Pinia, Vuetify 3, TypeScript, MSAL Browser 3
- **Backend**: .NET 10, Azure Functions v4 (isolated worker), EF Core 9, Cosmos DB
- **Auth**: Azure AD B2C
- **Authorization**: Casbin.NET RBAC-with-domains (families as domains)
- **Database**: Azure Cosmos DB

## Prerequisites

- [Node.js](https://nodejs.org/) 22+ and npm 11+
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- [Azure Cosmos DB Emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-develop-emulator) — for local CosmosDB
- [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite) — for local Azure Storage (required by Azure Functions)

## Local Setup

### 1. Start Azurite

Azurite provides local Azure Storage emulation, which Azure Functions requires.

```bash
azurite
```

Or run via the VS Code extension (Azurite: Start).

### 2. Start the Azure Cosmos DB Emulator

Launch the [Cosmos DB Emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-develop-emulator) (Windows) or the Linux emulator via Docker.

The emulator runs at `https://localhost:8081` with a well-known local key. The backend `local.settings.json` is pre-configured with these defaults.

### 3. Configure the backend

Create `api/src/EzDinner.Functions/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "CosmosDb:ConnectionString": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "CosmosDb:Database": "EzDinner",
    "AzureAdB2C:Instance": "<your-b2c-instance>",
    "AzureAdB2C:TenantId": "<your-tenant-id>",
    "AzureAdB2C:ClientId": "<your-client-id>",
    "AzureAdB2C:Domain": "<your-domain>",
    "AzureAdB2C:SignUpSignInPolicyId": "<your-policy-id>",
    "AzureAdB2C:ClientSecret": "<your-client-secret>"
  },
  "Host": {
    "CORS": "*"
  }
}
```

The `CosmosDb:ConnectionString` value above is the standard local emulator connection string.

### 4. Start the backend

```bash
cd api/src/EzDinner.Functions
func start
```

Runs at http://localhost:7071.

On first run (or after schema changes), seed authorization policies:

```bash
curl -X PUT http://localhost:7071/api/migrate
```

### 5. Install frontend dependencies

```bash
cd web
npm install
```

### 6. Start the frontend

```bash
cd web
npm run dev
```

Runs at http://localhost:3000.

The frontend defaults to `http://localhost:7071` as the API base URL. Override with:

```bash
NUXT_PUBLIC_API_BASE_URL=http://localhost:7071 npm run dev
```

## Project Structure

```
/web            # Active Nuxt 3 frontend
/api
  /src
    EzDinner.Functions          # Azure Functions HTTP entry points
    EzDinner.Application        # Use cases / commands
    EzDinner.Core               # Domain aggregates (DDD)
    EzDinner.Infrastructure     # CosmosDB repos, Casbin adapter, EF Core
    EzDinner.Authorization.Core # Casbin RBAC engine wrapper
    EzDinner.Query.Core         # Read-only query interfaces
  /tests
    EzDinner.IntegrationTests   # Integration tests (require live CosmosDB emulator)
```

## Common Commands

```bash
# Backend — build
cd api/src/EzDinner.Functions && dotnet build

# Backend — integration tests (requires CosmosDB emulator running)
cd api && dotnet test

# Frontend — tests
cd web && npm test

# Frontend — lint
cd web && npm run lint
```
