# Implementation Plan

Natures Chakki e-commerce platform — phased delivery tracker.

**Last updated:** October 2026
**Overall status:** Customer, Admin, authentication/cart security and COD workflow complete on this branch.

## Current Workflow Baseline

`cursor/auth-cart-security-fixes-702b` is the finalized functional baseline. `cursor/azure-single-app-production-702b` is its direct child and adds .NET 10, one-App-Service hosting, Azure SQL/APIM deployment and Production configuration. Use the Azure branch for production work; after its PR merges, use `main`. Older phase/UI branches are historical only.

Current customer flow:

`Register → Email OTP verification → reactive session restoration → claim-owned kg cart → finalized COD checkout → transactional stock update → cart clear → polling timeline → Delivered/Paid`

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
| `OrderService` (create, status, cancel) | ✅ Complete | COD deduction, cancellation reversal, status history |
| `InventoryService` | ✅ Complete | Unit tested |
| Orders API | ✅ Complete | `OrdersController` |
| Checkout + order history UI | ✅ Complete | `checkout/`, `account/orders/` |
| Customer order timeline | ✅ Complete | Account order detail UI |

---

## Phase 6 — Payments (Stripe)

| Item | Status | Notes |
|------|--------|-------|
| COD checkout/payment status | ✅ Active | Pending → Paid on Delivered |
| Stripe service/intent/webhook | ✅ Backend-ready | Future online checkout capability |

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
| Product image upload | ✅ Complete | validated Local storage; internal SKU generation |
| Inventory and order status history | ✅ Complete | movement/history tables |
| Audit logging and operational alerts | ✅ Complete | `AuditService`, `/admin/audit`, `/admin/alerts` |

---

## Phase 10 — Testing

| Item | Status | Notes |
|------|--------|-------|
| Unit tests | ✅ Complete | 12 tests |
| Integration tests | ✅ Complete | 24 tests |
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
| Legacy Azure Pipeline | ⚠ Historical | replaced by manual single-App-Service deployment on child Azure branch |
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
| CORS configuration | ✅ Complete | Dev origins configured |
| Azure single-App-Service deployment | ➡ Child branch | `cursor/azure-single-app-production-702b` |
| Persistent App Service uploads / optional Blob | ➡ Child branch | affordable Local default, Blob optional |
| Memory default / optional Redis | ➡ Child branch | no mandatory Redis cost |
| E2E Playwright tests | 🔲 Planned | Full checkout flow |
| Production CDN deploy | 🔲 Planned | Static Web Apps / CDN |

---

## Remaining Work (Low Priority)

1. Merge/deploy the child Azure production branch
2. Add Playwright/Cypress deployment E2E tests
3. Optimize the Angular initial bundle
4. Production-gate/remove `BuggyController`
5. Add Application Insights/OpenTelemetry
6. Upgrade to Redis/Blob only when persistence/scale requires them

---

## Quick Reference

| Environment | API | Frontend | SQL | Redis |
|-------------|-----|----------|-----|-------|
| Local dev | https://localhost:5001 | http://localhost:4200 | localhost:1433 | localhost:6379 |
| Docker | http://localhost:8080 | — | sql:1433 | redis:6379 |

**Development accounts:** Optional and configured through User Secrets when `SeedUsers:Enabled=true`.
