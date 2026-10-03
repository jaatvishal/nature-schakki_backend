# Natures Chakki

A full-stack e-commerce platform for artisan flour and grain products — built as a **modular monolith** with ASP.NET Core 9 and Angular 21.

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-21-DD0031)](https://angular.dev/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927)](https://www.microsoft.com/sql-server)
[![Stripe](https://img.shields.io/badge/Stripe-Payments-635BFF)](https://stripe.com/)

## Overview

This branch contains the finalized customer/Admin application baseline: email OTP activation, reactive JWT sessions, secure password reset, claims-owned carts priced per kilogram, transactional COD checkout, inventory/order history and a role-protected Admin Portal. Azure single-App-Service deployment work continues on `cursor/azure-single-app-production-702b`.

```mermaid
flowchart TB
    subgraph Frontend["Angular 21 SPA"]
        SHOP[Shop & Catalog]
        CART[Cart & Wishlist]
        CHK[Checkout]
        ADM[Admin Panel]
    end

    subgraph Backend["ASP.NET Core 9 API"]
        API[REST Controllers v1]
        ID[Identity + JWT]
        SVC[Domain Services]
    end

    subgraph Data["Persistence"]
        SQL[(SQL Server)]
        REDIS[(Redis)]
    end

    STRIPE[Stripe]

    SHOP & CART & CHK & ADM -->|HTTPS + JWT| API
    API --> ID & SVC
    SVC --> SQL
    SVC --> REDIS
    CHK --> STRIPE
    STRIPE -->|Webhooks| API
```

## Features

- **Catalog** — Paginated product listing with brand/type filters and specifications pattern
- **Cart** — Claims-owned customer carts, trusted database prices, kg validation, duplicate merging, Memory/Redis storage
- **Orders** — Finalized transactional COD checkout, stock deduction/reversal, delivery timeline
- **Payments** — COD is active; Stripe backend/webhooks remain future online-payment capability
- **Coupons** — Percentage and fixed discounts with usage limits
- **Wishlist** — Per-user saved products
- **Reviews** — Customer reviews with admin moderation
- **Admin** — Dashboard, customers, products/images, categories, orders, inventory, COD payments, reports, audit and alerts
- **Auth** — OTP activation, reactive JWT/refresh session, hashed single-use password reset, Admin/Customer roles
- **Order tracking** — Customer and Admin status timelines

## Tech Stack

| Layer | Technology |
|-------|------------|
| API | ASP.NET Core 9, EF Core 9, Serilog, FluentValidation |
| Auth | ASP.NET Identity, JWT Bearer, refresh tokens |
| Frontend | Angular 21, Angular Material, Tailwind CSS 4 |
| Database | SQL Server 2022 |
| Cache | In-memory / Redis (StackExchange.Redis) |
| Payments | Stripe.net |
| Tests | xUnit, WebApplicationFactory, Vitest |
| DevOps | Docker, Docker Compose, Azure Pipelines |

## Project Structure

```
├── API/                 # Controllers, middleware, hubs, Program.cs
├── Core/                # Entities, DTOs, interfaces, specifications
├── Infrastructure/      # EF contexts, services, migrations, validators
├── Tests/               # Unit + integration tests
├── client/              # Angular 21 SPA
├── docs/                # Architecture & feature documentation
├── Dockerfile           # Multi-stage API image
├── docker-compose.yml   # SQL Server + Redis + API
└── azure-pipelines.yml  # CI/CD pipeline
```

## Quick Start

### Prerequisites

- .NET 9 SDK
- Node.js 20+
- Docker

### 1. Start infrastructure

```bash
docker compose up -d
```

### 2. Run the API

```bash
dotnet run --project API
```

API: https://localhost:5001 · Swagger: https://localhost:5001/swagger

### 3. Run the frontend

```bash
cd client && npm ci && npm start
```

App: http://localhost:4200

### 4. Login

Roles are created by `IdentitySeed`. Optional users are created only when `SeedUsers:Enabled=true` and credentials are supplied through User Secrets/environment variables; no default credentials are documented or required.

## Configuration

Key settings in `API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=NaturesChakki;...",
    "Redis": "localhost:6379"
  },
  "CacheProvider": "Memory",
  "JwtSettings": { "Key": "...", "DurationInMinutes": 60 },
  "StripeSettings": { "SecretKey": "...", "WebhookSecret": "..." }
}
```

Copy `.env.example` for local environment variable overrides.

## Testing

```bash
dotnet test Tests/Tests.csproj
cd client && npm test -- --watch=false
```

34 backend tests and 16 Angular tests pass on this branch. See [docs/07-Testing.md](docs/07-Testing.md).

## Documentation

| Doc | Topic |
|-----|-------|
| [01-Architecture](docs/01-Architecture.md) | Layers, bounded contexts, diagrams |
| [02-Getting-Started](docs/02-Getting-Started.md) | Setup, migrations, run commands |
| [03-Database](docs/03-Database.md) | Schema, ER diagram, indexes |
| [04-Authentication](docs/04-Authentication.md) | Identity, JWT, refresh tokens |
| [05-Caching](docs/05-Caching.md) | Memory vs Redis |
| [06-Payments](docs/06-Payments.md) | Stripe flow, webhooks |
| [07-Testing](docs/07-Testing.md) | Test suites and scenarios |
| [08-Deployment](docs/08-Deployment.md) | Docker, Azure architecture |
| [09-Security](docs/09-Security.md) | CORS, rate limiting, secrets |
| [10-Features](docs/10-Feature-Documentation.md) | Feature reference |
| [11-Admin Portal](docs/11-Admin-Portal.md) | Admin operations, security, inventory, uploads, reports |
| [Implementation Plan](docs/IMPLEMENTATION-PLAN.md) | Phase status tracker |

## Docker

```bash
docker compose up -d          # SQL + Redis + API
docker build -t natureschakki-api .
```

API container: http://localhost:8080

## License

This project is for portfolio and educational purposes.
