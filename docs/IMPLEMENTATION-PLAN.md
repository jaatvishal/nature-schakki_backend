# Implementation Plan

Natures Chakki e-commerce platform — phased delivery tracker.

**Last updated:** October 2026
**Overall status:** Customer, Admin, security and single-App-Service production work implemented; Azure resource configuration remains manual.

## Current Workflow Baseline

The implemented branch chain is:

```text
cursor/auth-cart-security-fixes-702b
  → cursor/azure-single-app-production-702b
  → PR to main
```

The auth/cart branch contains the finalized OTP, COD, Admin and customer security/UX baseline. The Azure branch adds .NET 10, Angular-in-ASP.NET hosting, controlled Azure SQL deployment, production validation, APIM OpenAPI generation, persistent local uploads and optional Redis/Blob providers. After the production PR is merged, use `main`; older phase/UI branches are historical only.

Current customer flow:

`Register → Email OTP verification → reactive session restoration → claim-owned kg cart → finalized COD checkout → transactional stock update → cart clear → polling order timeline → Delivered/Paid`

---

## Phase 1 — Foundation

| Item | Status | Notes |
|------|--------|-------|
| Solution structure (API, Core, Infrastructure, Tests) | ✅ Complete | `Natures_Chakki_Backend.sln` |
| Angular 21 client scaffold | ✅ Complete | `client/` |
| Docker Compose (SQL + Redis) | ✅ Complete | `docker-compose.yml` |
| Git ignore / repo hygiene | ✅ Complete | `.gitignore`, `.vs` untracked |

---

## Phase 2 — Identity & Auth

| Item | Status | Notes |
|------|--------|-------|
| ASP.NET Identity (`AppUser`, `AppRole`) | ✅ Complete | `Infrastructure/Identity/` |
| JWT + refresh tokens | ✅ Complete | `TokenService`, `AccountController` |
| Role seeding (Admin, Customer) | ✅ Complete | `IdentitySeed.cs` |
| Angular auth service + guards | ✅ Complete | `auth.service.ts`, guards |
| Auth interceptor with token refresh | ✅ Complete | `auth-interceptor.ts` |

---

## Phase 3 — Catalog

| Item | Status | Notes |
|------|--------|-------|
| Product entity + EF configuration | ✅ Complete | `Core/Entities/Product.cs` |
| Product repository + specifications | ✅ Complete | `ProductSpecification` |
| Product API (list, filter, CRUD) | ✅ Complete | `ProductController` |
| Shop UI + product details | ✅ Complete | `features/shop/` |
| Seed data | ✅ Complete | `StoreContextSeed.cs` |

---

## Phase 4 — Cart & Caching

| Item | Status | Notes |
|------|--------|-------|
| `ICartService` abstraction | ✅ Complete | Memory + Redis implementations |
| `CacheProvider` configuration | ✅ Complete | `InfrastructureServiceRegistration.cs` |
| Cart API | ✅ Complete | `CartController` |
| Angular cart service + UI | ✅ Complete | `cart.service.ts`, cart components |

---

## Phase 5 — Orders & Inventory

| Item | Status | Notes |
|------|--------|-------|
| Order entity + owned address | ✅ Complete | `StoreContext` |
| `OrderService` (create, status, cancel) | ✅ Complete | COD deduction, cancellation reversal, transition history |
| `InventoryService` | ✅ Complete | Unit tested |
| Orders API | ✅ Complete | `OrdersController` |
| Checkout + order history UI | ✅ Complete | `checkout/`, `account/orders/` |
| Customer order timeline | ✅ Complete | Account order detail UI |

---

## Phase 6 — Payments (Stripe)

| Item | Status | Notes |
|------|--------|-------|
| COD checkout/payment state | ✅ Active | Pending → Paid on Delivered |
| `StripePaymentService` and intent API | ✅ Backend-ready | Future online checkout capability |
| Webhook handler + Payment uniqueness | ✅ Backend-ready | Idempotent future Stripe processing |

---

## Phase 7 — Coupons

| Item | Status | Notes |
|------|--------|-------|
| Coupon entity + usage tracking | ✅ Complete | `Coupon`, `CouponUsage` |
| `CouponService` validation + discount calc | ✅ Complete | Unit tested |
| Validate API + checkout integration | ✅ Complete | `CouponsController` |
| Admin coupon management | ✅ Complete | `AdminController` |

---

## Phase 8 — Wishlist & Reviews

| Item | Status | Notes |
|------|--------|-------|
| Wishlist entity + service | ✅ Complete | One per user |
| Wishlist API + UI | ✅ Complete | `WishlistController`, `wishlist/` |
| Product reviews + moderation | ✅ Complete | `ReviewService`, admin moderate |
| Reviews API + product detail display | ✅ Complete | `ReviewsController` |

---

## Phase 9 — Admin Panel

| Item | Status | Notes |
|------|--------|-------|
| Admin API (products, orders, users, coupons, reviews) | ✅ Complete | `AdminController` |
| Admin guard + routes | ✅ Complete | `admin.guard.ts` |
| Operational dashboard and reporting APIs | ✅ Complete | KPIs, trends, top products, recent activity |
| Users, products, categories, orders, inventory, payments UI | ✅ Complete | `features/admin/` |
| Product image upload | ✅ Complete | validated Local default; optional Azure Blob |
| Inventory and order status history | ✅ Complete | movement/history tables |
| Audit logging and operational alerts | ✅ Complete | `AuditService`, `/admin/audit`, `/admin/alerts` |

---

## Phase 10 — Testing

| Item | Status | Notes |
|------|--------|-------|
| Unit tests (inventory, orders, coupons, timezone) | ✅ Complete | 12 tests |
| Integration tests (API) | ✅ Complete | 25 tests |
| Angular tests | ✅ Complete | 17 tests |
| Test factory (in-memory DB) | ✅ Complete | `CustomWebApplicationFactory` |

---

## Phase 11 — DevOps & Documentation

| Item | Status | Notes |
|------|--------|-------|
| Architecture documentation | ✅ Complete | `docs/01-Architecture.md` |
| Getting started guide | ✅ Complete | `docs/02-Getting-Started.md` |
| Database, auth, caching, payments docs | ✅ Complete | `docs/03`–`06` |
| Testing, deployment, security docs | ✅ Complete | `docs/07`–`09` |
| Feature documentation | ✅ Complete | `docs/10-Feature-Documentation.md` |
| Dockerfile (multi-stage) | ✅ Complete | Root `Dockerfile` |
| Docker Compose with API service | ✅ Complete | `docker-compose.yml` |
| Manual single-App-Service publish | ✅ Complete | `scripts/publish-single-app.ps1` |
| Azure SQL deployment tools | ✅ Complete | migration apply + idempotent script generation |
| APIM OpenAPI generation | ✅ Complete | Swagger 2/3 via `SwaggerHostFactory` |
| Legacy Azure Pipeline | 🗄 Archived | incompatible two-service .NET 9 pipeline in `docs/legacy` |
| `.env.example` | ✅ Complete | Root template |
| README | ✅ Complete | Portfolio-ready overview |

---

## Phase 12 — Production Hardening

| Item | Status | Notes |
|------|--------|-------|
| Global rate limiting | ✅ Complete | 100 req/min in `Program.cs` |
| Health checks | ✅ Complete | `/health` |
| Serilog structured logging | ✅ Complete | Console + config |
| Exception middleware | ✅ Complete | `ExceptionMiddleware.cs` |
| API versioning | ✅ Complete | Asp.Versioning v1 |
| FluentValidation | ✅ Complete | Account validators |
| CORS configuration | ✅ Complete | config-driven; same-origin Production |
| One App Service Angular + API | ✅ Complete | same-origin `/api`, SPA fallback |
| .NET 10 / patched Angular 21 | ✅ Complete | pinned SDK/toolchains; production audits clean |
| Production settings validation | ✅ Complete | SQL/JWT/URL/SMTP + selected providers |
| Persistent local uploads | ✅ Complete | App Service `%HOME%/data`; Azure Blob optional |
| Affordable Memory cache default | ✅ Complete | Redis optional before scale-out |
| E2E Playwright tests | 🔲 Planned | Full checkout flow |
| Angular bundle optimization | 🔲 Planned | initial bundle exceeds warning budget |

---

## Remaining Work (Low Priority)

1. Complete Azure App Service settings, SQL identity/firewall and initial bootstrap
2. Verify manually copied Azure SQL includes complete `__EFMigrationsHistory`
3. Add Playwright/Cypress deployment E2E tests
4. Optimize the Angular initial bundle
5. Production-gate or remove `BuggyController`
6. Add Redis/Blob only when restart persistence or scale-out is required
7. Add Application Insights/OpenTelemetry and an email provider beyond SMTP

---

## Quick Reference

| Environment | API | Frontend | SQL | Redis |
|-------------|-----|----------|-----|-------|
| Local dev | https://localhost:5001 | http://localhost:4200 | localhost:1433 | localhost:6379 |
| Docker | http://localhost:8080 (Angular + API) | same origin | sql:1433 | redis:6379 |
| Azure | one App Service (Angular + API) | same origin `/api` | Azure SQL | Memory default / optional Redis |

**Development accounts:** Optional and configured through User Secrets when `SeedUsers:Enabled=true`.
