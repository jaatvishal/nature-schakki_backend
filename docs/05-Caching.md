# Caching

Natures Chakki uses a pluggable cache strategy controlled by the `CacheProvider` configuration key.

## Configuration

```json
{
  "CacheProvider": "Memory",
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

Set in `API/appsettings.json`, `API/appsettings.Development.json`, or environment variable `CacheProvider`.

Registration logic: `Infrastructure/InfrastructureServiceRegistration.cs`

## Providers

| `CacheProvider` | `ICartService` | `ICacheService` | Registration |
|-----------------|----------------|-----------------|--------------|
| `Memory` (default) | `InMemoryCartStorage` | `MemoryCacheService` | `AddMemoryCache()` |
| `Redis` | `RedisCartStorage` | `RedisCacheService` | `IConnectionMultiplexer` via `ConnectionStrings:Redis` |

## ICartService

Interface: `Core/Interfaces/ICartService.cs`

Controllers use `CartWorkflow` above this storage interface to enforce authenticated ownership, authoritative product prices, active/stock checks, configurable kg increments, duplicate merging and per-cart concurrency locks.

| Method | Description |
|--------|-------------|
| `GetCartAsync(key)` | Retrieve cart by anonymous or session ID |
| `SetCartAsync(cart)` | Persist cart |
| `DeleteCartAsync(key)` | Remove cart |

### InMemoryCartStorage

- Static `ConcurrentDictionary<string, string>` in `Infrastructure/Services/InMemoryCartStorage.cs`
- JSON-serialized `ShoppingCart` objects
- **Single-instance option** — data is lost on restart/deployment and is not shared across instances

### RedisCartStorage

- `Infrastructure/Services/RedisCartStorage.cs`
- Uses StackExchange.Redis `IDatabase`
- 30-day expiry
- Recommended before restart persistence or scale-out is required

Cart API includes backward-compatible GET/replace/clear plus authenticated `/items` add/set/remove endpoints. Authenticated cart ids come from JWT claims.

## ICacheService

Interface: `Core/Interfaces/ICacheService.cs`

Used for general-purpose caching (product lookups, etc.):

- `MemoryCacheService` — wraps `IMemoryCache`
- `RedisCacheService` — distributed cache via Redis

## Switching to Redis Locally

1. Start Redis: `docker compose up -d redis`
2. Update `appsettings.Development.json`:

```json
{
  "CacheProvider": "Redis",
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

3. Restart the API

## Architecture

```mermaid
flowchart LR
    CFG[CacheProvider config]
    CFG -->|Memory| MEM[InMemoryCartStorage]
    CFG -->|Memory| MC[MemoryCacheService]
    CFG -->|Redis| RC[RedisCartStorage]
    CFG -->|Redis| RS[RedisCacheService]
    MEM --> API[CartController]
    RC --> REDIS[(Redis)]
    RS --> REDIS
```

## Production Recommendations

| Concern | Recommendation |
|---------|----------------|
| Cart persistence | Set `CacheProvider=Redis` |
| Connection string | Use Azure Cache for Redis with SSL (`rediss://`) |
| TTL | Add expiration on cart keys (e.g. 7–30 days) for abandoned carts |
| Failover | Configure Redis connection multiplexer with retry and timeout |
| Secrets | Store `ConnectionStrings__Redis` in Key Vault |
| Monitoring | Track Redis memory, hit rate, and connection errors |
| Scale-out | Never use `Memory` provider when running multiple API instances |

## Health Checks

`Program.cs` registers EF Core health checks for both DbContexts at `/health`. Consider adding a Redis health check when using the Redis provider in production.
