# Getting Started

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 | Build and run the API |
| [Node.js](https://nodejs.org/) | 22 | Angular CLI and frontend (`.nvmrc`) |
| [Docker](https://www.docker.com/) | Latest | SQL Server and Redis containers |
| Git | Latest | Clone the repository |

Optional: [Stripe CLI](https://stripe.com/docs/stripe-cli) for local webhook testing.

## 1. Clone and Start Infrastructure

```bash
git clone <repository-url>
cd <repo-root>
cp .env.example .env
# Set MSSQL_SA_PASSWORD and JWT_SIGNING_KEY in the ignored .env.
docker compose up -d
```

This starts:

- **SQL Server** on `localhost:1433` (set `MSSQL_SA_PASSWORD` in your ignored `.env`)
- **Redis** on `localhost:6379`

Verify health:

```bash
docker compose ps
```

## 2. Configure the API

Development settings are in `API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "",
    "Redis": "localhost:6379"
  },
  "CacheProvider": "Memory"
}
```

Store the local database connection in User Secrets rather than committed JSON:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<local SQL connection string>" --project API
```

For Redis-backed carts, set `"CacheProvider": "Redis"` in `appsettings.Development.json` or via environment variable.

The ignored `.env` is used by Docker Compose only; use User Secrets for direct `dotnet run`.

### Local JWT and Gmail SMTP secrets

Keep credentials out of `appsettings*.json`. From the repository root, store local values with .NET User Secrets:

```bash
dotnet user-secrets set "JwtSettings:Key" "<your-random-key-of-at-least-32-bytes>" --project API
dotnet user-secrets set "Email:FromAddress" "<your-gmail-address>" --project API
dotnet user-secrets set "Email:Smtp:Username" "<your-gmail-address>" --project API
dotnet user-secrets set "Email:Smtp:Password" "<your-gmail-app-password>" --project API
```

For Gmail, enable two-step verification and create an **App Password**. Google generates only the password: the SMTP username and sender address must both be the full Gmail address that created it. Do not use the normal Google account password. Copied spaces in the 16-character App Password are ignored by the SMTP implementation. Restart the API process after changing secrets.

If `JwtSettings:Key` is missing in Development, the API now creates a secure ephemeral key so login works, but all JWTs become invalid when the API restarts. Configure User Secrets for a stable local login session. Production startup still fails when the key is missing.

## 3. Database Migrations

EF Core migrations are the schema source of truth. Development startup applies them when `Database:ApplyMigrationsOnStartup=true`; Production keeps this false and deploys migrations separately.

For local manual application:

```bash
dotnet tool restore
# Set ConnectionStrings__DefaultConnection in the current shell first.
dotnet ef database update --project Infrastructure --startup-project API --context StoreContext
dotnet ef database update --project Infrastructure --startup-project API --context AppIdentityDbContext
```

The design-time factories intentionally read `ConnectionStrings__DefaultConnection` from the process environment so production tooling does not execute application startup.

Add a new migration:

```bash
dotnet ef migrations add <Name> --project Infrastructure --startup-project API --context StoreContext --output-dir Migrations/Store
```

## 4. Run the Backend

```bash
dotnet restore Natures_Chakki_Backend.sln
dotnet run --project API
```

| Endpoint | URL |
|----------|-----|
| API (HTTPS) | https://localhost:5001 |
| API (HTTP) | http://localhost:5000 |
| Swagger UI | https://localhost:5001/swagger |
| Health check | https://localhost:5001/health |

Development catalog seeding follows `Database:SeedStoreDataOnStartup`. Optional users require `SeedUsers:Enabled=true` plus secret-provided credentials.

## 5. Run the Frontend

```bash
cd client
npm ci
npm start
```

Angular dev server: **http://localhost:4200**

API base URL is configured in `client/src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api',
};
```

Production uses `client/src/environments/environment.prod.ts`, where `apiUrl` is the same-origin relative path `/api`. `dotnet publish -c Release` builds Angular automatically and includes it in API `wwwroot`.

## 6. Verify the Stack

```bash
# Health
curl -k https://localhost:5001/health

# Products
curl -k "https://localhost:5001/api/product?pageIndex=1&pageSize=6"

# Login (customer)
curl -k -X POST https://localhost:5001/api/v1/account/login \
  -H "Content-Type: application/json" \
  -d '{"email":"<configured-email>","password":"<configured-password>"}'
```

## 7. Run Tests

```bash
dotnet test Tests/Tests.csproj
cd client && npm test -- --watch=false
```

Current baseline: 37 backend tests and 17 Angular tests.

## Troubleshooting

| Issue | Fix |
|-------|-----|
| SQL connection refused | Wait for `docker compose` health check; confirm port 1433 is free |
| HTTPS certificate errors in browser | Trust dev cert: `dotnet dev-certs https --trust` |
| CORS errors from Angular | Ensure API allows `http://localhost:4200` (configured in `API/Program.cs`) |
| Redis connection failed | Set `CacheProvider` to `Memory` or start the Redis container |
