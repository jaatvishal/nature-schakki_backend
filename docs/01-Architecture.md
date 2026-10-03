# Architecture

Natures Chakki is a **modular monolith** e-commerce platform: a single deployable ASP.NET Core 9 API with a clear layered structure and two bounded database contexts.

## Solution Structure

| Layer | Project | Responsibility |
|-------|---------|----------------|
| Presentation | `API/` | Controllers, middleware, Swagger, CORS, rate limiting |
| Application contracts | `Core/` | Entities, DTOs, interfaces, specifications, enums, exceptions |
| Infrastructure | `Infrastructure/` | EF Core contexts, repositories, services, migrations, validators |
| Frontend | `client/` | Angular 21 SPA (shop, cart, checkout, account, admin) |
| Tests | `Tests/` | xUnit unit and integration tests |

## Layered Dependency Flow

Dependencies point **inward** only:

```
client (Angular)  →  API  →  Infrastructure  →  Core
Tests             →  API, Infrastructure, Core
```

- `Core` has no references to other projects.
- `Infrastructure` implements `Core` interfaces and registers services in `InfrastructureServiceRegistration.cs`.
- `API` wires authentication, versioning, health checks, and maps endpoints.

## Modular Monolith

The API is one process with **feature-oriented modules** sharing a SQL Server database but separated by bounded contexts:

| Bounded Context | DbContext | Tables / Concerns |
|-----------------|-----------|-------------------|
| **Commerce** | `StoreContext` | Products, orders, payments, coupons, reviews, wishlists, inventory/history, audit |
| **Identity** | `AppIdentityDbContext` | ASP.NET Identity, refresh tokens, email OTPs, password-reset tokens |

Both contexts use the same `ConnectionStrings:DefaultConnection` but maintain separate EF migrations under `Infrastructure/Migrations/Store/` and `Infrastructure/Migrations/Identity/`.

## Key Patterns

- **Repository + Unit of Work** — `IGenericRepository<T>`, `IUnitOfWork`, `IProductRespository`
- **Specification** — `BaseSpecification<T>`, `ProductSpecification`, `OrderWithItemsSpecification`
- **Strategy (cache)** — `CacheProvider` switches `ICartService` / `ICacheService` between Memory and Redis
- **Cart workflow** — authenticated ownership, authoritative product prices, kg validation, duplicate merging and per-cart locking
- **Domain services** — `OrderService`, `InventoryService`, `CouponService`, `PasswordResetService`, `StripePaymentService`
- **Admin projections** — paged DTO-based operational APIs for users, products, orders, inventory, payments, reports, audit, and alerts
- **Storage strategy** — `IFileStorageService` isolates local product uploads from a future Azure Blob implementation
- **Time boundary** — UTC persistence/security, explicit UTC API JSON, named `Asia/Kolkata` report conversion and global Angular IST display

## Architecture Diagram

```mermaid
flowchart TB
    subgraph Client["client/ (Angular 21)"]
        UI[Components & Routes]
        SVC[Core Services]
        INT[HTTP Interceptors]
    end

    subgraph API["API/ (ASP.NET Core 9)"]
        CTRL[Controllers v1]
        MW[ExceptionMiddleware]
        HC[/health]
    end

    subgraph Core["Core/"]
        ENT[Entities & DTOs]
        IF[Interfaces]
        SPEC[Specifications]
    end

    subgraph Infra["Infrastructure/"]
        SC[StoreContext]
        IC[AppIdentityDbContext]
        SRV[Services]
        VAL[FluentValidation]
    end

    subgraph External["External Systems"]
        SQL[(SQL Server)]
        REDIS[(Redis - optional)]
        STRIPE[Stripe API]
    end

    UI --> SVC --> INT
    INT -->|HTTPS + JWT| CTRL
    CTRL --> MW
    CTRL --> IF
    IF --> SRV
    SRV --> SC
    SRV --> IC
    SRV --> REDIS
    SRV --> STRIPE
    SC --> SQL
    IC --> SQL
```

## API Surface

| Controller | Base route | Notes |
|------------|------------|-------|
| `ProductController` | `/api/product` | Catalog, brands, types (unversioned + v1) |
| `AccountController` | `/api/v1/account` | Register, login, refresh, logout |
| `CartController` | `/api/v1/cart` | Guest compatibility plus claim-owned authenticated item writes |
| `OrdersController` | `/api/v1/orders` | Create, list, cancel (authorized) |
| `PaymentsController` | `/api/v1/payments` | Future online-payment intent + Stripe webhook |
| `WishlistController` | `/api/v1/wishlist` | User wishlist |
| `ReviewsController` | `/api/v1/reviews` | Product reviews |
| `CouponsController` | `/api/v1/coupons` | Validate coupon codes |
| `AdminController` | `/api/v1/admin` | Admin role only |
| `DeliveryMethodsController` | `/api/v1/deliverymethods` | Shipping options |

## Startup & Seeding

On startup (`API/Program.cs`), unless `ASPNETCORE_ENVIRONMENT=Testing`:

1. Migrate `StoreContext` and `AppIdentityDbContext`
2. Seed commerce data via `StoreContextSeed.SeedAsync`
3. Seed dev users via `IdentitySeed.SeedUsersAsync` (Development only)

Development users are optional and require `SeedUsers:Enabled=true` plus external credentials. No default credentials are committed.

## Branch handoff

`cursor/auth-cart-security-fixes-702b` is the functional application baseline. Its child `cursor/azure-single-app-production-702b` upgrades to .NET 10 and adds one-App-Service Angular hosting, Azure SQL deployment scripts, APIM OpenAPI generation and Production configuration validation.
