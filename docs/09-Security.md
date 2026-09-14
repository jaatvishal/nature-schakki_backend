# Security

Security controls across the Natures Chakki platform.

## Authentication & Authorization

| Control | Implementation |
|---------|----------------|
| Password hashing | ASP.NET Identity (PBKDF2) |
| API auth | JWT Bearer (`JwtBearerDefaults.AuthenticationScheme`) |
| Token validation | Issuer, audience, lifetime, signing key (`Program.cs`) |
| Role-based access | `[Authorize(Roles = "Admin")]` on `AdminController` |
| Protected routes | `[Authorize]` on orders, payments, wishlist, account |
| Refresh token rotation | Old token revoked on refresh (`AccountController`) |
| Logout | Revokes all active refresh tokens for user |
| Registration activation | Hashed, expiring email OTP with attempt and resend limits |
| Account deactivation | `ActiveUserMiddleware` rejects existing JWT sessions for inactive users |
| Admin APIs | Server-side Admin role plus Angular route guard |
| Product uploads | 5 MB limit, image signature verification, generated safe file names |

### JWT Secret Management

- Development: `appsettings.json` → `JwtSettings:Key`
- Production: `JwtSettings__Key` from Azure Key Vault (min 256-bit random key)

Never commit production keys. Rotate keys with a planned token invalidation window.

## CORS

Configured in `API/Program.cs`:

```csharp
policy.AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithOrigins(
        "http://localhost:5001",
        "https://localhost:5001",
        "http://localhost:4200",
        "https://localhost:4200");
```

**Production:** restrict to exact frontend origin(s). Do not use `AllowAnyOrigin()` with credentials.

## Rate Limiting

Global fixed-window limiter in `Program.cs`:

- **100 requests per minute** per authenticated user name or host header
- Returns `429 Too Many Requests`

Consider tighter limits on auth endpoints (`/account/login`, `/account/register`) in production.

## Secrets & Configuration

| Secret | Storage |
|--------|---------|
| SQL password | Key Vault / App Service settings |
| JWT key | Key Vault |
| Stripe keys | Key Vault |
| Redis connection | Key Vault |

`.env` is gitignored (`.gitignore`). Use `.env.example` as a template.

`SeedUsers` credentials are Development-only (`IdentitySeed` checks `IsDevelopment()`).

## Input Validation

- **FluentValidation** — `Infrastructure/Validators/AccountValidators.cs` (register/login DTOs)
- Registered via `AddValidatorsFromAssemblyContaining<RegisterDtoValidator>()`
- **Model binding** — ASP.NET Core automatic validation on DTOs
- **EF Core** — column length constraints in `EntityConfigurations.cs`
- **Admin DTOs** — dedicated write contracts prevent entity overposting

Email OTP values are generated with a cryptographic RNG, stored only as salted hashes, replaced on resend, and never logged. SMTP credentials must be supplied through environment configuration or Key Vault.

## Error Handling

`API/Middleware/ExceptionMiddleware.cs` maps exceptions to consistent JSON responses without leaking stack traces in production:

- `NotFoundException` → 404
- `BadRequestException` → 400
- `UnauthorizedException` → 401
- `ValidationException` → 400 with field errors

`BuggyController` exists for integration/error testing only — disable or remove in production.

## HTTPS

- `UseHttpsRedirection()` enabled
- Development cert: `dotnet dev-certs https --trust`
- Production: TLS termination at App Service / reverse proxy

## Stripe Webhook Security

- Signature validation via `EventUtility.ConstructEvent`
- Raw body required (do not parse before validation)
- `WebhookSecret` from Stripe Dashboard — never in source control

## Data Protection

| Data | Protection |
|------|------------|
| Passwords | Hashed by Identity |
| Refresh tokens | Stored hashed-equivalent (opaque random), revocable |
| Payment data | PCI scope minimized — Stripe handles card data |
| PII (email, address) | SQL encryption at rest (Azure SQL TDE) recommended |

## HTTP Security Headers (Recommended)

Add in production reverse proxy or middleware:

- `Strict-Transport-Security`
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Content-Security-Policy` on Angular static host

## Audit Trail

`IAuditService` / `AuditLog` entity records admin actions for compliance review.

## Dependency Security

```bash
dotnet list package --vulnerable
cd client && npm audit
```

Run in CI (see `azure-pipelines.yml`).

## Security Testing Checklist

- [ ] Unauthenticated access blocked on protected endpoints
- [ ] Customer cannot call admin APIs
- [ ] Invalid JWT rejected
- [ ] Expired refresh token rejected
- [ ] CORS blocks unknown origins
- [ ] Rate limit triggers at threshold
- [ ] Stripe webhook rejects invalid signatures
