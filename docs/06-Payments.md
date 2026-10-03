# Payments

The active customer checkout supports **Cash on Delivery (COD) only**. Stripe services, intent APIs and idempotent webhook handling remain implemented for a future online-payment UI, but Angular checkout does not currently invoke Stripe.

## Components

| File | Role |
|------|------|
| `Infrastructure/Services/StripePaymentService.cs` | Creates payment intents and refunds |
| `API/Controllers/PaymentsController.cs` | Intent creation + webhook endpoint |
| `Core/Interfaces/IPaymentService.cs` | Payment abstraction |

## Configuration

`API/appsettings.json` → `StripeSettings`:

| Key | Description |
|-----|-------------|
| `StripeSettings:PublishableKey` | Reserved for a future online-payment frontend |
| `StripeSettings:SecretKey` | Server-side API key |
| `StripeSettings:WebhookSecret` | Webhook signing secret (`whsec_...`) |

When `SecretKey` is empty or `sk_test_placeholder`, the API runs in **mock mode** and returns `pi_mock_{guid}` intents.

## Active COD flow

```mermaid
sequenceDiagram
    participant U as User
    participant FE as Angular Checkout
    participant API as PaymentsController
    participant OS as OrderService
    participant DB as StoreContext

    U->>FE: Place COD order
    FE->>API: POST /api/v1/orders
    API->>OS: CreateOrderAsync
    OS->>DB: Recalculate prices, save order/items, deduct stock
    API->>DB: Clear authenticated cart
    API-->>FE: Processing order / COD Pending
    Note over API,DB: Admin progresses Packed → Shipped → OutForDelivery → Delivered
    DB-->>FE: Delivered COD displays Paid
```

Order creation and stock movement execute transactionally. Duplicate submissions are serialized per customer and the second request sees an empty cart.

## API Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/v1/payments/create-intent/{orderId}` | Bearer | Creates Stripe intent for order total |
| POST | `/api/v1/payments/webhook` | Anonymous | Stripe event handler |

These endpoints are future online-payment capabilities. Current COD checkout uses `/api/v1/orders`.

## Webhook Handling

`PaymentsController.Webhook`:

1. Reads raw request body
2. Validates signature with `EventUtility.ConstructEvent` and `StripeSettings:WebhookSecret`
3. On `payment_intent.succeeded`:
   - Finds order by `PaymentIntentId`
   - Calls `orderService.UpdateOrderStatusAsync(orderId, OrderStatus.PaymentReceived)`
   - Inserts `Payment` record with `PaymentStatus.Succeeded`

Mock mode: if webhook secret is `whsec_placeholder`, returns `200 OK` without processing.

## Idempotency

| Mechanism | Implementation |
|-----------|----------------|
| Unique payment intent | `Payments.PaymentIntentId` has unique index (`EntityConfigurations.cs`) |
| Order lookup | Webhook finds order by `PaymentIntentId` — duplicate events update same order |
| Status guard | `OrderService.UpdateOrderStatusAsync` validates transitions before updating |
| Mock intents | Prefixed `pi_mock_` for local dev without Stripe |

**Recommendation for production:** use Stripe idempotency keys on `PaymentIntent.Create` and check `Payment` table before inserting duplicate webhook records.

## Refunds

`StripePaymentService.RefundPaymentAsync` — creates Stripe refund; mock intents return `true` immediately.

## Local Webhook Testing

```bash
stripe listen --forward-to https://localhost:5001/api/v1/payments/webhook
# Copy whsec_... to StripeSettings:WebhookSecret
```

## Order Status Lifecycle

Relevant statuses (`Core/Enums/OrderStatus.cs`):

General lifecycle:

`Pending` → `PaymentReceived` → `Processing` → `Packed` → `Shipped` → `OutForDelivery` → `Delivered`

COD timeline:

`Pending` → `Processing` → `Packed` → `Shipped` → `OutForDelivery` → `Delivered`

COD is Pending before delivery, Paid when Delivered, and Failed when Cancelled/Failed. Cancellation restores deducted inventory.

## Security

- Webhook endpoint is `[AllowAnonymous]` but **must** validate Stripe signature in production
- Never expose `SecretKey` or `WebhookSecret` to the frontend
- Use `PublishableKey` only in Angular for Stripe Elements
