# Database

Natures Chakki uses **SQL Server** with two EF Core DbContexts sharing one database (`NaturesChakki`).

## Contexts

| Context | File | Migrations folder |
|---------|------|-------------------|
| Commerce | `Infrastructure/Data/StoreContext.cs` | `Infrastructure/Migrations/Store/` |
| Identity | `Infrastructure/Data/AppIdentityDbContext.cs` | `Infrastructure/Migrations/Identity/` |

Connection string key: `ConnectionStrings:DefaultConnection`

## Commerce Schema (`StoreContext`)

### Entities

| Entity | Table | Description |
|--------|-------|-------------|
| `Product` | Products | Catalog items with price, stock, brand/type |
| `ProductCategory` | ProductCategories | Category taxonomy |
| `ProductBrand` | ProductBrands | Brand taxonomy |
| `ProductImage` | ProductImages | Additional product images |
| `Inventory` | Inventories | On-hand and reserved stock per product |
| `Order` | Orders | Customer orders with owned `ShipToAddress` |
| `OrderItem` | OrderItems | Line items |
| `Payment` | Payments | Stripe payment records |
| `Coupon` | Coupons | Discount codes |
| `CouponUsage` | CouponUsages | Per-user coupon redemption tracking |
| `ProductReview` | ProductReviews | Customer reviews (moderation status) |
| `Wishlist` | Wishlists | One wishlist per user |
| `WishlistItem` | WishlistItems | Wishlist line items |
| `DeliveryMethod` | DeliveryMethods | Shipping options |
| `Address` | Addresses | Saved user addresses |
| `Notification` | Notifications | In-app notifications |
| `AuditLog` | AuditLogs | Admin audit trail |

### Relationships

- `Product` → `ProductCategory`, `ProductBrand` (FK: `CategoryId`, `BrandId`)
- `Product` → `ProductImage` (1:N)
- `Product` → `Inventory` (1:1)
- `Product` → `ProductReview` (1:N)
- `Order` → `OrderItem` (1:N)
- `Order` → `DeliveryMethod` (N:1)
- `Order` → `Coupon` (optional N:1)
- `Order.ShipToAddress` — owned entity (`OwnsOne` in `StoreContext`)
- `Wishlist` → `WishlistItem` (1:N), one wishlist per `UserId`

Identity tables (`AspNetUsers`, `AspNetRoles`, etc.) live in `AppIdentityDbContext` with `AppUser` (`IdentityUser<int>`) and `RefreshToken`.

## Indexes

Configured in `Infrastructure/Config/EntityConfigurations.cs`:

| Table | Index | Type |
|-------|-------|------|
| ProductCategories | Name | Unique |
| ProductBrands | Name | Unique |
| Coupons | Code | Unique |
| Payments | PaymentIntentId | Unique |
| Inventories | ProductId | Unique |
| Wishlists | UserId | Unique |
| RefreshTokens | Token | Unique |

## Decimal Precision

Money columns use `decimal(18,2)` for `Order`, `OrderItem`, `Payment`, `Coupon`, `DeliveryMethod`.

## Cart Storage

Shopping carts are **not** persisted in SQL. They use `ICartService`:

- `InMemoryCartStorage` when `CacheProvider=Memory`
- `RedisCartStorage` when `CacheProvider=Redis`

Cart entities (`ShoppingCart`, `CartItem`) are serialized JSON documents keyed by cart ID.

## ER Diagram

```mermaid
erDiagram
    ProductCategory ||--o{ Product : categorizes
    ProductBrand ||--o{ Product : brands
    Product ||--o{ ProductImage : has
    Product ||--|| Inventory : tracks
    Product ||--o{ ProductReview : receives
    Product ||--o{ OrderItem : "ordered as"

    DeliveryMethod ||--o{ Order : ships
    Coupon ||--o{ Order : applies
    Order ||--|{ OrderItem : contains
    Order ||--o| Payment : paid_by

    Wishlist ||--|{ WishlistItem : contains
    Product ||--o{ WishlistItem : saved_in

    Coupon ||--o{ CouponUsage : tracked

    AppUser ||--o{ RefreshToken : has
    AppUser ||--o| Wishlist : owns
    AppUser ||--o{ Address : saves
    AppUser ||--o{ Notification : receives

    Product {
        int Id PK
        string Name
        decimal Price
        int QuantityInStock
        int CategoryId FK
        int BrandId FK
    }

    Order {
        int Id PK
        int UserId
        int DeliveryMethodId FK
        int CouponId FK
        decimal Total
        string PaymentIntentId
        int Status
    }

    Inventory {
        int Id PK
        int ProductId FK
        int QuantityOnHand
        int ReservedQuantity
    }

    Coupon {
        int Id PK
        string Code UK
        int Type
        decimal Value
    }
```

## Seeding

- **Commerce**: `Infrastructure/Data/StoreContextSeed.cs` — products, brands, categories, delivery methods from `Infrastructure/Data/SeedData/`
- **Identity**: `Infrastructure/Data/IdentitySeed.cs` — Admin and Customer roles + dev users (Development only)

## Migration Commands Reference

```bash
# List migrations
dotnet ef migrations list --project Infrastructure --startup-project API --context StoreContext

# Roll back
dotnet ef database update <PreviousMigration> --project Infrastructure --startup-project API --context StoreContext
```
