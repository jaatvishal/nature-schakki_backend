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
npm test
```

Uses Vitest (configured via `@angular/build`).

## Test Structure

```
Tests/
├── Unit/
│   ├── CouponServiceTests.cs      (4 tests)
│   ├── InventoryServiceTests.cs   (3 tests)
│   └── OrderServiceTests.cs       (2 tests)
├── Integration/
│   └── IntegrationTests.cs        (11 tests)
└── GlobalUsings.cs
```

## 25 Test Scenarios Covered

### Unit — Inventory (`InventoryServiceTests`)

| # | Test | Validates |
|---|------|-----------|
| 1 | `ReserveStockAsync_SucceedsWhenStockAvailable` | Stock reservation increments `ReservedQuantity` |
| 2 | `ReserveStockAsync_FailsWhenInsufficientStock` | Returns false when stock insufficient |
| 3 | `AdjustStockAsync_UpdatesProductQuantity` | Stock adjustment updates `Product.QuantityInStock` |

### Unit — Orders (`OrderServiceTests`)

| # | Test | Validates |
|---|------|-----------|
| 4 | `UpdateOrderStatusAsync_Cancelled_ReleasesReservedStock` | Cancellation releases reserved inventory |
| 5 | `CreateOrderAsync_ThrowsWhenCartEmpty` | Empty cart throws `BadRequestException` |

### Unit — Coupons (`CouponServiceTests`)

| # | Test | Validates |
|---|------|-----------|
| 6 | `CalculateDiscount_Percentage_ReturnsCorrectAmount` | 10% discount on $100 = $10 |
| 7 | `CalculateDiscount_FixedAmount_CapsAtOrderTotal` | Fixed discount capped at order total |
| 8 | `CalculateDiscount_RespectsMinimumOrderAmount` | No discount below minimum order |
| 9 | `ValidateCouponAsync_ThrowsWhenExpired` | Expired coupon rejected |

### Integration — API (`IntegrationTests`)

| # | Test | Validates |
|---|------|-----------|
| 10 | `GetProducts_ReturnsOkWithPagination` | `GET /api/product` returns `data` + `count` |
| 11 | `GetProductById_ReturnsProduct` | `GET /api/product/1` returns seeded product |
| 12 | `GetBrands_ReturnsStringArray` | `GET /api/product/brands` |
| 13 | `GetTypes_ReturnsStringArray` | `GET /api/product/types` |
| 14 | `Register_ReturnsUserWithToken` | `POST /api/v1/account/register` returns JWT |
| 15 | `Login_WithInvalidCredentials_ReturnsUnauthorized` | Bad credentials → 401 |
| 16 | `GetCart_ReturnsEmptyCartForNewId` | New cart ID returns empty cart |
| 17 | `UpdateCart_PersistsItems` | `POST /api/v1/cart` persists items |
| 18 | `HealthCheck_ReturnsHealthy` | `GET /health` → 200 |
| 19 | `GetDeliveryMethods_ReturnsList` | `GET /api/v1/deliverymethods` |
| 20 | `SearchProducts_WithBrandFilter_ReturnsFilteredResults` | Brand filter query param |

Additional critical integration coverage includes registration activation by OTP, unverified login rejection, invalid/expired OTP, maximum attempts, resend cooldown and prior-code invalidation, safe email-provider failure, backend price recalculation, COD stock deduction/cart clearing, failed-checkout atomicity, and cross-customer order-detail protection.

Admin integration coverage verifies Admin/customer role boundaries, dashboard access, product create/edit/activate/archive, image signature validation, inventory adjustment/history, audit output, COD payment history, and valid/invalid order status transitions. Angular guard tests cover direct Admin URL access for both Admin and Customer roles.

### Angular (`app.spec.ts`)

| # | Test | Validates |
|---|------|-----------|
| 21 | `should create the app` | Root component bootstraps |
| 22 | `should render title` | Template renders expected heading |

### Documented Manual / E2E Scenarios

| # | Scenario | How to verify |
|---|----------|---------------|
| 23 | Checkout requires authentication | Navigate to `/checkout` unauthenticated → redirect to login |
| 24 | Admin routes require Admin role | Customer JWT cannot access `/api/v1/admin/products` |
| 25 | Payment intent mock mode | Create order + `POST /api/v1/payments/create-intent/{id}` returns `pi_mock_*` |

## CI Integration

See `azure-pipelines.yml` — Build stage runs `dotnet test` on every push.

## Adding Tests

```bash
# New unit test class
# Add to Tests/Unit/MyServiceTests.cs

# New integration test
# Extend IntegrationTests.cs or add new fixture class
```

Follow existing patterns: in-memory `StoreContext` for unit tests, `CustomWebApplicationFactory` for HTTP-level integration tests.
