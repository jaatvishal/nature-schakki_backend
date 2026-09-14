# Getting Started

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 9.0+ | Build and run the API |
| [Node.js](https://nodejs.org/) | 20+ | Angular CLI and frontend |
| [Docker](https://www.docker.com/) | Latest | SQL Server and Redis containers |
| Git | Latest | Clone the repository |

Optional: [Stripe CLI](https://stripe.com/docs/stripe-cli) for local webhook testing.

## 1. Clone and Start Infrastructure

```bash
git clone <repository-url>
cd <repo-root>
docker compose up -d
```

This starts:

- **SQL Server** on `localhost:1433` (SA password: `Admin@123`)
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
    "DefaultConnection": "Server=localhost,1433;Database=NaturesChakki;User Id=sa;Password=Admin@123;TrustServerCertificate=True",
    "Redis": "localhost:6379"
  },
  "CacheProvider": "Memory"
}
```

For Redis-backed carts, set `"CacheProvider": "Redis"` in `appsettings.Development.json` or via environment variable.

Copy `.env.example` to `.env` for local secret overrides (see Deployment docs).

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

Migrations run automatically on API startup. To apply manually:

```bash
dotnet ef database update --project Infrastructure --startup-project API --context StoreContext
dotnet ef database update --project Infrastructure --startup-project API --context AppIdentityDbContext
```

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

On first run in Development, seed data and default users are created automatically.

## 5. Run the Frontend

```bash
cd client
npm install
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

Production build uses `client/src/environments/environment.prod.ts`.

## 6. Verify the Stack

```bash
# Health
curl -k https://localhost:5001/health

# Products
curl -k "https://localhost:5001/api/product?pageIndex=1&pageSize=6"

# Login (customer)
curl -k -X POST https://localhost:5001/api/v1/account/login \
  -H "Content-Type: application/json" \
  -d '{"email":"customer@natureschakki.com","password":"Customer@123!"}'
```

## 7. Run Tests

```bash
dotnet test Tests/Tests.csproj
cd client && npm test
```

## Troubleshooting

| Issue | Fix |
|-------|-----|
| SQL connection refused | Wait for `docker compose` health check; confirm port 1433 is free |
| HTTPS certificate errors in browser | Trust dev cert: `dotnet dev-certs https --trust` |
| CORS errors from Angular | Ensure API allows `http://localhost:4200` (configured in `API/Program.cs`) |
| Redis connection failed | Set `CacheProvider` to `Memory` or start the Redis container |
