# Testing

Test project: `Tests/Tests.csproj` (xUnit, .NET 9)

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
│   └── IntegrationTests.cs        (24 tests)
└── GlobalUsings.cs
```

## Current automated baseline

| Suite | Count | Coverage |
|-------|------:|----------|
| Backend unit | 12 | inventory, coupons, orders, UTC/IST conversion and UTC JSON normalization |
| Backend integration | 24 | catalog, OTP, login, secure reset, cart price/ownership/kg validation, COD checkout, Admin CRUD/upload/inventory/audit/payment/status and health |
| Angular | 17 | auth/Admin/cart/checkout/search behavior and centralized IST formatting |

Total: **36 backend tests + 17 Angular tests**.

The Azure production child branch adds a single-App-Service SPA fallback/API 404 integration test, bringing its backend total to 37.

## Covered areas

- OTP activation, expiry, resend cooldown, attempt limits and email failure
- login/logout/refresh state, Admin redirect and Angular guards/interceptor
- secure password-reset privacy, revocation, expiry and single use
- trusted cart price, quantity, duplicate merging, ownership and totals
- COD transaction, stock deduction/reversal, duplicate checkout and cross-user order protection
- Admin authorization, dashboard, product upload/CRUD, users, inventory, audit, payments and order transitions
- checkout finalization, distinct cart badge, Shop search clear/reset and app boot

## Deployment testing

This branch is the functional test baseline. The child `cursor/azure-single-app-production-702b` adds manual Release publish, Azure SQL migration/OpenAPI generation and single-App-Service smoke validation.

## Adding Tests

```bash
# New unit test class
# Add to Tests/Unit/MyServiceTests.cs

# New integration test
# Extend IntegrationTests.cs or add new fixture class
```

Follow existing patterns: in-memory `StoreContext` for unit tests, `CustomWebApplicationFactory` for HTTP-level integration tests.
