# Architecture

Natures Chakki is a **modular monolith** e-commerce platform: a single deployable ASP.NET Core 10 API with a clear layered structure and two bounded database contexts.

## Solution Structure

| Layer | Project | Responsibility |
|-------|---------|----------------|
| Presentation | `API/` | Controllers, middleware, Swagger, static Angular hosting, SPA fallback, rate limiting |
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
- **Strategy (cache)** — `CacheProvider` switches cart/cache storage between Memory and optional Redis
- **Cart workflow** — claim-based ownership, product/price reload, quantity validation, duplicate merging and per-cart locking
- **Domain services** — `OrderService`, `InventoryService`, `CouponService`, `PasswordResetService`, `StripePaymentService`
- **Admin projections** — paged DTO-based operational APIs for users, products, orders, inventory, payments, reports, audit, and alerts
- **Storage strategy** — `IFileStorageService` selects persistent App Service local storage or Azure Blob
- **Time boundary** — UTC persistence/security, explicit UTC API serialization, named `Asia/Kolkata` report conversion and global Angular IST display

## Architecture Diagram

```mermaid
flowchart TB
    subgraph Client["client/ (Angular 21)"]
        UI[Components & Routes]
        SVC[Core Services]
        INT[HTTP Interceptors]
    end

    subgraph API["API/ (ASP.NET Core 10)"]
        CTRL[Controllers v1]
        MW[ExceptionMiddleware]
        STATIC[Angular wwwroot + SPA fallback]
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
        FILES[(Persistent local files / optional Blob)]
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
    SRV --> FILES
    SRV --> STRIPE
    SC --> SQL
    IC --> SQL
```

## API Surface

| Controller | Base route | Notes |
|------------|------------|-------|
| `ProductController` | `/api/product` | Catalog, brands, types (unversioned + v1) |
| `AccountController` | `/api/v1/account` | Register, login, refresh, logout |
| `CartController` | `/api/v1/cart` | Guest read/replace by non-user id; authenticated writes are claim-owned |
| `OrdersController` | `/api/v1/orders` | Create, list, cancel (authorized) |
| `PaymentsController` | `/api/v1/payments` | Dormant/future online-payment intent + Stripe webhook |
| `WishlistController` | `/api/v1/wishlist` | User wishlist |
| `ReviewsController` | `/api/v1/reviews` | Product reviews |
| `CouponsController` | `/api/v1/coupons` | Validate coupon codes |
| `AdminController` | `/api/v1/admin` | Admin role only |
| `DeliveryMethodsController` | `/api/v1/deliverymethods` | Shipping options |

## Hosting, startup and seeding

- Release `dotnet publish` runs `npm ci`, builds Angular production assets, and includes them in API `wwwroot`.
- `/api/*` stays controller-owned; non-API deep links fall back to Angular `index.html`.
- Production migrations are external and controlled (`Database:ApplyMigrationsOnStartup=false`).
- Role, catalog and initial Admin seeding are independently configurable and idempotent.
- Development users are optional and come from User Secrets/environment variables; no credentials are committed.
