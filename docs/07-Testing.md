# Testing

Test project: `Tests/Tests.csproj` (xUnit, .NET 10)

| Suite | Path | Framework |
|-------|------|-----------|
| Unit | `Tests/Unit/` | xUnit + EF Core InMemory |
| Integration | `Tests/Integration/` | WebApplicationFactory + InMemory DB |
| Angular | `client/src/app/app.spec.ts` | Vitest + TestBed |

## Running Tests

### Backend

```bash
# All tests
dotnet test Tests/Tests.csproj

# With coverage
dotnet test Tests/Tests.csproj --collect:"XPlat Code Coverage"

# Single class
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~OrderServiceTests"
```

Integration tests use `ASPNETCORE_ENVIRONMENT=Testing` (`CustomWebApplicationFactory`), which switches to in-memory databases and skips startup migrations/seeding.

### Frontend

```bash
cd client
npm test -- --watch=false
```

Uses Vitest (configured via `@angular/build`).

## Test Structure

```
Tests/
├── Unit/
│   ├── CouponServiceTests.cs      (4 tests)
│   ├── InventoryServiceTests.cs   (3 tests)
│   ├── OrderServiceTests.cs       (3 tests)
│   └── IndiaTimeZoneTests.cs      (2 tests)
├── Integration/
│   └── IntegrationTests.cs        (25 tests)
└── GlobalUsings.cs
```

## Current automated baseline

| Suite | Count | Coverage |
|-------|------:|----------|
| Backend unit | 12 | inventory, coupons, orders, UTC/IST conversion and UTC JSON normalization |
| Backend integration | 25 | catalog, OTP registration, login, secure reset, claim-owned cart, trusted prices/kg validation, transactional COD checkout, cross-user authorization, Admin CRUD/upload/inventory/audit/payment/status, health, SPA fallback and API 404 |
| Angular | 17 | app/auth/cart/checkout/search behavior plus centralized IST formatting |

Total: **37 backend tests + 17 Angular tests**.

The integration factory uses EF InMemory and a temporary Angular `index.html`; Azure SQL/Redis/Blob/SMTP require deployment smoke tests.

## Production validation

Manual production validation is defined in `scripts/publish-single-app.ps1` and `docs/12-Azure-Single-App-Service.md`. The legacy Azure DevOps pipeline is archived and is not the supported deployment path.

## Adding Tests

```bash
# New unit test class
# Add to Tests/Unit/MyServiceTests.cs

# New integration test
# Extend IntegrationTests.cs or add new fixture class
```

Follow existing patterns: in-memory `StoreContext` for unit tests, `CustomWebApplicationFactory` for HTTP-level integration tests.
