# Azure Production Deployment — Single App Service

This is the authoritative manual deployment guide. Azure DevOps is not required.

Current implementation lineage: `cursor/auth-cart-security-fixes-702b` → `cursor/azure-single-app-production-702b` → production PR targeting `main`. After merge, `main` is the only future baseline.

## Architecture

```text
Azure App Service (.NET 10)
├── /                 Angular 21 application from ASP.NET Core wwwroot
├── /shop, /cart...   Angular SPA fallback
├── /api/*            ASP.NET Core controllers
└── /health           ASP.NET Core health check

Managed dependencies
├── Azure SQL Database
├── Azure Cache for Redis (optional future upgrade)
└── Azure Blob Storage (optional future upgrade)
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
| Product image persistence | HIGH | App Service `%HOME%/data` local storage default; optional `AzureBlob` provider available |
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

If schema/data was copied manually, confirm the destination also contains all Store and Identity rows in `__EFMigrationsHistory`. Tables without matching history are not a valid EF baseline; do not run migrations blindly against that state.

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

ConnectionStrings__DefaultConnection=<Azure SQL connection string>

JwtSettings__Key=<at least 32 random bytes>
JwtSettings__Issuer=https://<app-name>.azurewebsites.net
JwtSettings__Audience=https://<app-name>.azurewebsites.net
ClientUrl=https://<app-name>.azurewebsites.net

CacheProvider=Memory

FileStorage__Provider=Local
# Optional override; otherwise App Service uses %HOME%/data/NaturesChakki/uploads
FileStorage__LocalPath=

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
Swagger__Enabled=false
```

No production secret belongs in Git or `appsettings*.json`.

For ZIP deployment, set `WEBSITE_RUN_FROM_PACKAGE=1`. For Visual Studio **Web Deploy**, remove `WEBSITE_RUN_FROM_PACKAGE`; do not combine the two deployment modes.

The affordable single-instance defaults require neither Redis nor Blob Storage. Memory carts are lost during App Service restart/deployment. Local product images use persistent App Service `%HOME%/data` storage and survive Web Deploy, but Azure Blob remains recommended before scale-out.

Optional upgrades:

```text
CacheProvider=Redis
ConnectionStrings__Redis=<Azure Redis TLS connection string>

FileStorage__Provider=AzureBlob
FileStorage__ContainerName=product-images
ConnectionStrings__BlobStorage=<Azure Storage connection string>
```

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

## Visual Studio publish

Azure API Management is supported, but its API definition is deployed separately from the App Service ZIP. Visual Studio's legacy post-publish hook invokes `dotnet swagger --serializeasv2`, which is incompatible with the .NET 10 CLI. `UpdateApiOnPublish` is therefore disabled so it cannot turn a successful App Service publish into a failed publish result.

If an existing local publish profile still prints **Starting to update your API**, open:

```text
API/Properties/PublishProfiles/<profile>.pubxml
```

and set:

```xml
<UpdateApiOnPublish>false</UpdateApiOnPublish>
```

Generate a current OpenAPI document:

```powershell
./scripts/generate-openapi.ps1 -OpenApiVersion 2.0
```

Import `artifacts/openapi/swagger.json` through **API Management → APIs → Add API → OpenAPI**, or use:

```powershell
az apim api import `
  --resource-group <resource-group> `
  --service-name <api-management-service> `
  --api-id natures-chakki `
  --path natures-chakki `
  --specification-format OpenApiJson `
  --specification-path ./artifacts/openapi/swagger.json `
  --service-url https://<app-name>.azurewebsites.net
```

Because the OpenAPI operations already begin with `/api`, this produces APIM routes such as:

```text
https://<apim-name>.azure-api.net/natures-chakki/api/product
```

The repository pins Swashbuckle CLI 10.2.3 and uses `SwaggerHostFactory`, so OpenAPI generation does not start the application, connect to Azure SQL, run migrations, or require production secrets.

Alternatively set `Swagger__Enabled=true` temporarily in App Service and import:

```text
https://<app-name>.azurewebsites.net/swagger/v1/swagger.json
```

Set it back to `false` after import if public production Swagger is not required.

`HTTP Error 500.30` is a separate startup failure. For this project it normally means required Production App Service settings are missing. Confirm every setting in **Required App Service settings**, restart the app, and inspect **App Service → Log stream**. The application intentionally refuses to start with an empty Azure SQL connection, JWT key, production URL, SMTP credentials, Redis connection (when Redis is selected), or Blob Storage connection (when AzureBlob is selected).

## Rollback

- Use an App Service deployment slot or retain the previous ZIP.
- Swap/redeploy the previous application package for application rollback.
- Do not automatically roll back database migrations after production data is written.
- Take an Azure SQL backup/restore point before schema deployment.
