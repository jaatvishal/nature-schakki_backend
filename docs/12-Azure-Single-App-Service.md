# Azure Production Deployment — Single App Service

This is the authoritative manual deployment guide. Azure DevOps is not required.

## Architecture

```text
Azure App Service (.NET 10)
├── /                 Angular 21 application from ASP.NET Core wwwroot
├── /shop, /cart...   Angular SPA fallback
├── /api/*            ASP.NET Core controllers
└── /health           ASP.NET Core health check

Managed dependencies
├── Azure SQL Database
├── Azure Cache for Redis (recommended)
└── Azure Blob Storage for product images
```

Only one App Service hosts the frontend and API. Angular uses the relative production API URL `/api`.

## Readiness audit

| Area | Initial status | Resolution |
|------|----------------|------------|
| .NET 9 target | BLOCKER | Upgraded and pinned to .NET 10 |
| Separate Angular API hostname | BLOCKER | Production environment now uses `/api` |
| Angular absent from .NET publish | BLOCKER | Release publish runs `npm ci`, builds Angular, and includes it under `wwwroot` |
| SPA deep links | BLOCKER | Non-API fallback serves `index.html`; unmatched `/api/*` remains 404 |
| Automatic production migrations | BLOCKER | Disabled by default; controlled scripts apply both contexts |
| Legacy SQL dump | BLOCKER | Archived and explicitly marked non-deployable |
| Localhost-only CORS | HIGH | Configuration-driven; same-origin production requires no CORS origin |
| App Service proxy/HTTPS | HIGH | Forwarded headers and production HSTS configured |
| Local product image files | HIGH | `AzureBlob` provider added |
| Hardcoded development credentials | HIGH | Removed from appsettings and Docker templates |
| SDK/runtime reproducibility | MEDIUM | `global.json`, `.nvmrc`, npm engines, and local `dotnet-ef` manifest added |
| API, JWT, errors, health checks | READY | Existing behavior retained and validated |

## Database source of truth

EF Core migrations are authoritative:

1. `Infrastructure/Migrations/Store`
2. `Infrastructure/Migrations/Identity`

The historical `Infrastructure/Data/Legacy/Database_Data_Schema.LEGACY.sql` must not be executed against Azure SQL. It creates a local database, contains machine paths, represents an older schema, and is not idempotent.

### Option A — Apply migrations directly

Allow your current public IP through the Azure SQL firewall, then run:

```powershell
$env:ConnectionStrings__DefaultConnection = "<Azure SQL connection string>"
./scripts/deploy-database.ps1
Remove-Item Env:ConnectionStrings__DefaultConnection
```

The script applies Store migrations first and Identity migrations second. Re-running it is safe because EF records applied migrations in `__EFMigrationsHistory`.

### Option B — Generate reviewable SQL

```powershell
./scripts/generate-database-scripts.ps1
```

Execute these idempotent scripts in order using SSMS, Azure Data Studio, or the Azure Portal query editor:

1. `artifacts/database/01-store-context.sql`
2. `artifacts/database/02-identity-context.sql`

Do not run the two scripts simultaneously. Do not run the legacy SQL export.

## Build the single application package

Requirements:

- .NET SDK from `global.json`
- Node.js from `.nvmrc`
- npm 10

Run:

```powershell
./scripts/publish-single-app.ps1
```

The command performs:

1. `dotnet restore`
2. `dotnet build --configuration Release`
3. `dotnet test`
4. `npm ci`
5. Angular production build
6. `dotnet publish --configuration Release`
7. verification that `wwwroot/index.html` exists

Output: `artifacts/publish`

Create a ZIP whose root contains `API.dll` (not another nested publish directory):

```powershell
Compress-Archive -Path ./artifacts/publish/* -DestinationPath ./artifacts/natures-chakki.zip -Force
```

## Required App Service settings

Set these under **App Service → Configuration → Application settings** or through Key Vault references:

```text
ASPNETCORE_ENVIRONMENT=Production
SCM_DO_BUILD_DURING_DEPLOYMENT=false
WEBSITE_RUN_FROM_PACKAGE=1

ConnectionStrings__DefaultConnection=<Azure SQL connection string>

JwtSettings__Key=<at least 32 random bytes>
JwtSettings__Issuer=https://<app-name>.azurewebsites.net
JwtSettings__Audience=https://<app-name>.azurewebsites.net
ClientUrl=https://<app-name>.azurewebsites.net

CacheProvider=Redis
ConnectionStrings__Redis=<Azure Redis TLS connection string>

FileStorage__Provider=AzureBlob
FileStorage__ContainerName=product-images
ConnectionStrings__BlobStorage=<Azure Storage connection string>

Email__Provider=Smtp
Email__FromAddress=<verified sender>
Email__Smtp__Host=smtp.gmail.com
Email__Smtp__Port=587
Email__Smtp__EnableSsl=true
Email__Smtp__Username=<SMTP username>
Email__Smtp__Password=<SMTP app password>

Database__ApplyMigrationsOnStartup=false
Database__SeedStoreDataOnStartup=false
Database__SeedIdentityRolesOnStartup=true
SeedUsers__Enabled=false
```

No production secret belongs in Git or `appsettings*.json`.

If Redis is intentionally deferred for the first single instance, set `CacheProvider=Memory` and omit the Redis connection. Carts will be lost on restart/deployment, so Redis remains the production recommendation.

## First production bootstrap

After applying migrations and before the first start:

1. Set `Database__SeedStoreDataOnStartup=true` if the existing product/delivery seed data is required.
2. Set:

```text
SeedUsers__Enabled=true
SeedUsers__AdminEmail=<initial Admin email>
SeedUsers__AdminPassword=<temporary strong password>
```

3. Start the App Service.
4. Confirm Admin login and store data.
5. Immediately set:

```text
Database__SeedStoreDataOnStartup=false
SeedUsers__Enabled=false
```

6. Remove `SeedUsers__AdminPassword` from App Service settings and restart.

Roles are idempotently created on startup. Production does not seed a customer account.

## Azure resource configuration

### Azure SQL

- Use the existing database rather than `CREATE DATABASE`.
- The supplied SQL-authentication format is valid for Azure SQL:

```text
Server=tcp:desktop-109ejkm.database.windows.net,1433;Initial Catalog=NaturesChakki;Persist Security Info=False;User ID=adminvishal;Password=<secret>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

- Enable **Encrypt=True** and **TrustServerCertificate=False**.
- Add the deployment workstation IP to the SQL firewall, or use private networking.
- Use a migration identity with schema-change permissions.
- Prefer a separate least-privilege runtime identity after deployment.
- Enable automated backups and auditing.

### Blob Storage

- Create or allow creation of the `product-images` container.
- The current storefront requires public read access for product images.
- Keep the storage connection string in App Service/Key Vault only.

### App Service

- Runtime stack: .NET 10
- HTTPS Only: On
- Minimum TLS: 1.2 or newer
- Always On: On
- Health check path: `/health`
- Startup command: leave empty for a framework-dependent ZIP deployment
- Configure log retention and Application Insights if desired

## Manual deployment

Using Azure CLI:

```powershell
az webapp deploy `
  --resource-group <resource-group> `
  --name <app-name> `
  --src-path ./artifacts/natures-chakki.zip `
  --type zip
```

Then verify:

```text
GET https://<app-name>.azurewebsites.net/health        → 200
GET https://<app-name>.azurewebsites.net/              → Angular index
GET https://<app-name>.azurewebsites.net/shop          → Angular index after refresh
GET https://<app-name>.azurewebsites.net/api/product   → API response
GET https://<app-name>.azurewebsites.net/api/not-found → API 404, not Angular
```

Test registration/OTP, login, cart, COD checkout, Admin product upload, orders, and logout after deployment.

## Rollback

- Use an App Service deployment slot or retain the previous ZIP.
- Swap/redeploy the previous application package for application rollback.
- Do not automatically roll back database migrations after production data is written.
- Take an Azure SQL backup/restore point before schema deployment.
