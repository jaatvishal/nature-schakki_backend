# Admin Portal

The Admin Portal is fully included in `cursor/auth-cart-security-fixes-702b`, on top of the OTP/COD/customer workflow. Its child `cursor/azure-single-app-production-702b` adds production hosting without changing these Admin contracts.

## Security Boundary

- Angular protects every `/admin/**` route with `adminGuard`.
- `AdminController` is protected by `[Authorize(Roles = "Admin")]`; hiding navigation is not treated as authorization.
- Public product mutations are also Admin-only.
- `ActiveUserMiddleware` immediately blocks deactivated accounts, including direct API calls made with an existing JWT.
- Admin write endpoints accept dedicated DTOs to prevent mass assignment.
- Responses never contain password hashes, OTP records, refresh tokens, or JWTs.

## Admin Areas

| Area | Route | API |
|------|-------|-----|
| Dashboard | `/admin` | `GET /api/v1/admin/dashboard` |
| Customers | `/admin/users` | `GET /admin/users`, `PUT /admin/users/{id}/status`, order history |
| Products | `/admin/products` | paged CRUD, activate/deactivate, archive, image upload |
| Categories | `/admin/categories` | paged create/edit/activate |
| Orders | `/admin/orders` | paged search/filter, detail, validated status updates |
| Inventory | `/admin/inventory` | stock levels, optimistic adjustment, movement history |
| Payments | `/admin/payments` | COD and future online-payment history |
| Reports | `/admin/reports` | date-filtered sales, revenue, COD, cancellations, best sellers |
| Audit | `/admin/audit` | paged important administrative actions |
| Alerts | `/admin/alerts` | low/out-of-stock, new/pending/cancelled orders, payment failures |

All collection APIs use server-side filtering, ordering, projection, and bounded pagination (maximum 100 rows).

## Dashboard Queries

The backend computes KPIs and summaries:

- users, products, orders, pending/completed/cancelled/COD orders
- collected revenue and payment counts
- orders by status and 30-day revenue trend
- best-selling and low-stock products
- recent orders and registrations

Angular renders the returned projection and does not download complete tables to calculate analytics.

## Product Images

`IFileStorageService` remains the storage boundary.

```text
Admin upload → signature/size validation → IFileStorageService
    └── LocalFileStorage (implemented on this branch)
```

Current validation permits genuine JPEG, PNG or WebP signatures up to 5 MB. Original paths are discarded, generated names are used and deletion is constrained to the storage root. Internal SKU generation is automatic. The Azure child branch adds persistent App Service local storage and an optional Azure Blob provider.

## Order, Inventory, Payment, and Audit Flow

```text
Customer places COD order
  → backend price/stock transaction
  → InventoryTransaction: Order deduction
  → Admin reviews order
  → validated OrderStatusRules transition
  → OrderStatusHistory records admin and timestamp
  → cancellation restores stock with movement history
  → delivered COD appears as Collected to Admin and Paid to Customer
  → AuditLog records significant admin actions
```

Invalid transitions such as `Delivered → Processing` are rejected. Inventory updates require the quantity observed by the Admin, preventing silent overwrites after concurrent changes.

## Future Extensions

The current interfaces and records allow additional storage providers, payment providers, warehouses, variants, suppliers, and asynchronous alert delivery without changing customer-facing checkout contracts. Those features are intentionally not implemented yet.
