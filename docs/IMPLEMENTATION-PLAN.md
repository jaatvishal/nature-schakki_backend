# Implementation Plan — Natures Chakki E-Commerce Platform

> **Status:** Phase 1 complete (analysis). Phases 2–13 are incremental delivery.
> **Branches:** `main` (backend), `Frontend` (Angular client). Backend work stays on `main` feature branches; frontend on `Frontend`.

---

## 1. Current State Summary

### Solution layout (actual)

```
/workspace
├── API/                 # ASP.NET Core 9 Web API
├── Core/                # Domain entities, interfaces, specifications
├── Infrastructure/      # EF Core, repositories, services
├── Natures_Chakki_Backend.sln
└── docker-compose.yml   # SQL Server only (incomplete)
```

**Target layout (evolutionary, not big-bang rewrite):**

```
src/
├── API/
├── Core/
│   ├── Domain/          # Entities, enums, value objects
│   └── Application/     # DTOs, interfaces, specifications, validators
├── Infrastructure/
└── Tests/
client/                  # On Frontend branch
```

We will **extend** existing projects rather than relocate immediately. New folders/namespaces inside `Core` will separate Domain vs Application concerns.

### Technology baseline

| Layer | Current | Target |
|-------|---------|--------|
| .NET | 9.0 | 9.0 |
| EF Core | 9.0, SQL Server | + Identity, migrations, indexes |
| Auth | None | Identity + JWT + refresh tokens |
| Cart | Redis-only (`CartService`), DI commented out | `ICartStorage` → Memory / Redis |
| Cache | None | `ICacheService` → Memory / Redis |
| Logging | Default | Serilog |
| API docs | Commented OpenAPI | Swagger + versioning |
| Frontend | Angular 21 on `Frontend` branch | Full e-commerce UI |
| Tests | None | xUnit unit + integration + Angular |
| Docs | Placeholder README | `/docs` (10 guides) |

### Build status

- **Backend:** builds clean (`dotnet build` — 0 errors).
- **Frontend:** on `origin/Frontend` — shop, home, product details, interceptors; no auth/cart/checkout yet.
- **Runtime blockers:** Redis DI disabled but `CartService` requires it; connection string is Windows-local in `appsettings.Development.json`; root `docker-compose.yml` has typo (`plateform`) and no Redis.

---

## 2. Existing Features (Course Progress)

### Backend — implemented

| Feature | Location | Notes |
|---------|----------|-------|
| Product catalog CRUD | `API/Controllers/ProductController.cs` | Uses `IGenericRepository<Product>` + specs |
| Filtering / search / sort / pagination | `Core/Specifications/ProductSpecification.cs` | Brand, type, search, sort |
| Brand & type lists | `BrandListSpecification`, `TypeListSpecification` | Distinct queries |
| Repository pattern | `Infrastructure/Data/GenericRepository.cs` | Generic + spec evaluator |
| Specification pattern | `Core/Specifications/*` | Full criteria/order/paging |
| Unit of Work | Implicit via `StoreContext.SaveChanges` | **Gap:** no `IUnitOfWork` abstraction |
| Global exception middleware | `API/Middleware/ExceptionMiddleware.cs` | ProblemDetails-like JSON, not RFC 7807 |
| CORS | `Program.cs` | localhost:4200, 5001 |
| EF migrations + seed | `StoreContextSeed`, `products.json` | ~18 products; `delivery.json` unused |
| Cart API (skeleton) | `CartController`, `CartService` | Broken without Redis registration |
| Buggy test endpoints | `BuggyController` | Dev error testing |

### Backend — entities

| Entity | Persisted | Purpose |
|--------|-----------|---------|
| `Product` | SQL Server | Name, price, brand, type, stock, image URL |
| `ShoppingCart`, `CartItem` | Redis only | Cart session |

### Frontend (`Frontend` branch) — implemented

| Feature | Location |
|---------|----------|
| Home page | `features/home/` |
| Shop listing + filters dialog | `features/shop/` |
| Product details | `product-details/` |
| Header layout | `layout/header/` |
| HTTP interceptors | `core/interceptors/` (loading, error) |
| Shop service | `core/services/shop.service.ts` |
| Error pages | `shared/components/not-found`, `server-error` |
| Angular Material + Tailwind | `material-theme.scss`, postcss |

### Frontend — missing

Auth, guards, cart, wishlist, checkout, orders, profile, admin, SignalR, lazy-loaded feature modules, configurable header components.

---

## 3. Gap Analysis (Course → Production)

### Critical (Phase 2 — stabilize)

| # | Gap | Impact | Action |
|---|-----|--------|--------|
| 1 | Redis cart hard-required, DI off | Cart API crashes | Introduce `ICartStorage` with `InMemoryCartStorage` default |
| 2 | No connection string in base `appsettings.json` | Non-dev deploy fails | Env-based config + Docker connection strings |
| 3 | `docker-compose.yml` broken/incomplete | Local infra unreliable | Fix typo, add Redis (optional), volumes, API service |
| 4 | `ProductController` catch returns `null` | Silent 204/empty responses | Remove local catches; rely on middleware |
| 5 | Dead `IProductRespository` | Confusion | Consolidate or use for complex queries |
| 6 | No Swagger | API undiscoverable | Enable OpenAPI/Swagger |
| 7 | Seed path relative to CWD | Fragile seeding | Use `IHostEnvironment.ContentRootPath` |

### Architecture (Phase 3)

| Gap | Action |
|-----|--------|
| No Application layer | Add `Core/Application` for DTOs, validators, service interfaces |
| No `IUnitOfWork` | Add thin UoW over `StoreContext` |
| Entities exposed in API | Response/request DTOs + mapping (manual or minimal AutoMapper) |
| No FluentValidation | Add validators for commands/DTOs |
| No Serilog | Replace default logging |
| No health checks | Add `/health` |
| No rate limiting | Add ASP.NET rate limiter policies |
| No ProblemDetails | Standardize on `ProblemDetails` |

### E-Commerce features (Phases 4–8)

| Feature | Priority | Depends on |
|---------|----------|------------|
| Identity + JWT + refresh | P0 | — |
| Address management | P0 | Identity |
| Cart (memory + Redis) | P0 | Storage abstraction |
| Checkout + orders | P0 | Cart, Identity, inventory |
| Inventory (reserve/sold) | P0 | Orders |
| Stripe + webhooks | P0 | Orders |
| Categories / brands (normalized) | P1 | DB migration |
| Product images / variants | P1 | File storage abstraction |
| Wishlist | P1 | Identity |
| Coupons | P1 | Orders |
| Reviews / ratings | P2 | Orders (verified purchase rule) |
| Notifications + SignalR | P2 | Orders, admin |
| Admin APIs + UI | P1 | Roles |
| Audit logging | P2 | All write ops |
| Email (IEmailService) | P2 | Orders, auth |
| Background jobs (email, low-stock) | P3 | IEmailService |

### Database model gaps

Current: single `Products` table with denormalized `Brand`/`Type` strings.

**Target entities** (add incrementally):

```
User (Identity) ─┬─ Address
                 ├─ Wishlist ─ WishlistItem ─ Product
                 ├─ Order ─ OrderItem ─ Product
                 ├─ ProductReview
                 └─ Notification

ProductCategory ─ Product
ProductBrand ─ Product
Product ─ ProductImage, ProductVariant, Inventory
Order ─ Payment, CouponUsage
Coupon
AuditLog
```

**DbContext boundaries (course requirement):**

- `StoreContext` — catalog, orders, inventory (extend)
- `AppIdentityDbContext` — Identity (separate context, shared DB)

### Security gaps

- No auth/authz
- No input validation pipeline
- No rate limiting
- Secrets in `appsettings.Development.json` (dev-only; use User Secrets / env vars)
- CORS allows credentials-less any method (tighten for prod)
- No HTTPS enforcement config for prod

### Performance gaps

- No `AsNoTracking` on read queries
- No projection DTOs (loads full entities)
- No caching for catalog metadata
- No indexes beyond PK (add with normalized schema)

### Testing gaps

- Zero test projects
- No CI pipeline

### Documentation gaps

- Placeholder README
- No `/docs` directory

---

## 4. Architectural Decisions (upfront)

### 4.1 Modular monolith

Single deployable API with clear layer boundaries. Enables future extraction (e.g., Payments, Notifications) without premature microservices.

**Dependency flow:** `API → Infrastructure → Core.Application → Core.Domain`

### 4.2 Cart / cache abstraction

```
ICartStorage
├── InMemoryCartStorage   (CacheProvider=Memory, default for local dev)
└── RedisCartStorage      (CacheProvider=Redis, production)

ICacheService
├── MemoryCacheService
└── DistributedCacheService (Redis)
```

**Why:** `IMemoryCache` is per-instance; carts must be shared across scaled API instances in production. Memory is fine for single-instance local dev.

**TTL:** Cart 30 days sliding; catalog cache 5–15 min with invalidation on admin writes.

### 4.3 Payment abstraction

```
IPaymentService
└── StripePaymentService
```

Order marked `Paid` only after verified Stripe webhook (`checkout.session.completed` / `payment_intent.succeeded`). Idempotency via `Payment.StripeEventId` unique index.

### 4.4 Search abstraction

```
IProductSearchService
└── SqlProductSearchService   (initial)
    └── (future) AzureSearchProductSearchService
```

### 4.5 File storage abstraction

```
IFileStorageService
├── LocalFileStorage      (dev)
└── AzureBlobFileStorage  (prod)
```

### 4.6 Order state machine

```
Pending → PaymentPending → Paid → Processing → Shipped → Delivered
                ↓              ↓
           Cancelled      Refunded
```

Valid transitions enforced in `OrderService`, not controllers.

---

## 5. Phased Roadmap

### Phase 2 — Stabilize (next)

- [ ] Fix docker-compose (SQL + optional Redis)
- [ ] Environment-based connection strings
- [ ] `ICartStorage` + in-memory default; wire Redis via config
- [ ] Fix `ProductController` error handling
- [ ] Enable Swagger
- [ ] Serilog bootstrap
- [ ] Health checks
- [ ] Verify API starts against Docker SQL

### Phase 3 — Architecture hardening

- [ ] `IUnitOfWork`, Application DTOs
- [ ] FluentValidation
- [ ] ProblemDetails + exception middleware upgrade
- [ ] API versioning (`/api/v1/`)
- [ ] Rate limiting policies
- [ ] `ICacheService` for categories/brands
- [ ] Normalize `ProductCategory`, `ProductBrand` entities

### Phase 4 — Identity & security

- [ ] `AppIdentityDbContext`, Identity seed (admin + customer via config)
- [ ] JWT access + refresh token rotation
- [ ] Auth controllers, policies
- [ ] Frontend: auth module, interceptors, guards (`Frontend` branch)

### Phase 5 — Cart & wishlist

- [ ] Cart persistence (memory/Redis)
- [ ] Wishlist entity + API
- [ ] Frontend cart drawer/page

### Phase 6 — Orders, inventory, checkout

- [ ] Order/OrderItem entities + state machine
- [ ] Inventory reservation (optimistic concurrency via `RowVersion`)
- [ ] Checkout flow (multi-step on frontend)
- [ ] Address management

### Phase 7 — Payments

- [ ] `IPaymentService` + Stripe Checkout
- [ ] Webhook endpoint with signature validation
- [ ] Idempotent event processing
- [ ] Frontend payment return URLs

### Phase 8 — Admin

- [ ] Admin policies + controllers (products, orders, users, coupons, reviews)
- [ ] Admin dashboard (frontend lazy module)
- [ ] SignalR hub for order notifications

### Phase 9 — Extended catalog

- [ ] Product images, variants, SKU
- [ ] Coupons (server-side validation)
- [ ] Reviews (moderation, verified purchase)
- [ ] `IEmailService` (dev logger + SendGrid-ready)
- [ ] Notifications entity

### Phase 10 — Testing

- [ ] `Tests.Unit`, `Tests.Integration` xUnit projects
- [ ] Cover 25 mandatory scenarios (see requirements)
- [ ] Angular tests for services, guards, forms

### Phase 11 — Documentation

- [ ] `docs/01-Architecture.md` … `10-Feature-Documentation.md`
- [ ] Professional README with Mermaid diagrams
- [ ] Testing strategy doc

### Phase 12 — DevOps / Azure

- [ ] Dockerfile (API), docker-compose full stack
- [ ] Azure DevOps pipeline example
- [ ] `08-Deployment.md`, Key Vault / App Insights guidance

### Phase 13 — Production readiness review

- [ ] Checklist from requirements
- [ ] Security review, N+1 audit, async audit
- [ ] Load smoke tests

---

## 6. Immediate Next Actions (Phase 2 detail)

1. **Create `Core/Interfaces/ICartStorage.cs`** — same contract as current `ICartService`.
2. **Implement `InMemoryCartStorage`** using `ConcurrentDictionary` + `IMemoryCache` or in-process dict with TTL.
3. **Refactor `RedisCartStorage`** from current `CartService`.
4. **Register in `Program.cs`:**
   ```json
   "CacheProvider": "Memory"  // or "Redis"
   ```
5. **Update `appsettings.Development.json`** for Docker SQL:
   `Server=localhost,1433;Database=NaturesChakki;User Id=sa;Password=...;TrustServerCertificate=True`
6. **Fix root `docker-compose.yml`** — add Redis service, fix `platform`, named volumes.
7. **Add `appsettings.json` connection string placeholder** (overridden by env).

---

## 7. Frontend Strategy (`Frontend` branch)

Continue on `Frontend` branch for all Angular work. Merge backend API changes to `main` first; frontend consumes versioned API.

**Next frontend milestones (after Phase 4 auth):**

1. Lazy-loaded feature modules: `catalog`, `cart`, `checkout`, `account`, `admin`
2. Configurable header sub-components
3. JWT interceptor + refresh interceptor
4. Reactive forms for checkout (multi-step)
5. SignalR client for order updates

---

## 8. Risk & Trade-offs

| Decision | Alternative | Trade-off |
|----------|-------------|-----------|
| Extend existing 3-project solution | New `src/` folder restructure | Faster delivery; rename later if needed |
| SQL search first | Elasticsearch now | Simpler; abstraction allows swap |
| In-memory cart default | Require Redis locally | Better DX without Redis installed |
| Single `StoreContext` + Identity context | Full CQRS | Course-aligned; sufficient for monolith |
| Manual mapping over AutoMapper everywhere | AutoMapper all DTOs | Less magic; AutoMapper only for complex maps |

---

## 9. Production Readiness Tracker

| Area | Current | Target Phase |
|------|---------|--------------|
| Backend builds | ✅ | — |
| Frontend builds | ⚠️ not verified on branch | 2 |
| Migrations | ✅ | — |
| Auth | ❌ | 4 |
| Cart w/o Redis | ❌ | 2 |
| Checkout | ❌ | 6 |
| Stripe | ❌ | 7 |
| Tests | ❌ | 10 |
| Docs | 🔄 this file | 11 |
| Docker | ❌ | 12 |

---

## 10. Assumptions

- SQL Server available via Docker for local dev.
- Stripe test keys via User Secrets / env vars (never committed).
- Admin seed credentials in `appsettings.Development.json` only.
- Frontend development continues on `Frontend` branch; backend on `main` feature branches.
- .NET 9 and Angular 21 remain the target versions.

---

*Last updated: Phase 1 analysis — repository inspected, backend builds, Frontend branch catalogued.*
