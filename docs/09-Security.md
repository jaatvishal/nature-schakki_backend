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

- Development: User Secrets or environment variable `JwtSettings__Key` (an ephemeral key is generated only when omitted)
- Production: `JwtSettings__Key` from Azure Key Vault (min 256-bit random key)

Never commit production keys. Rotate keys with a planned token invalidation window.

## CORS

`Cors:AllowedOrigins` is configuration-driven. Development permits the Angular localhost origin. Production Angular and API share one origin, so no CORS origin is required by default. Wildcard origin with credentials is never enabled.

## Rate Limiting

Global fixed-window limiter in `Program.cs`:

- **100 requests per minute** per authenticated user name or host header
- Returns `429 Too Many Requests`

Forgot/reset password additionally allows 5 requests per 15 minutes per source IP.

## Secrets & Configuration

| Secret | Storage |
|--------|---------|
| SQL password | Key Vault / App Service settings |
| JWT key | Key Vault |
| Stripe keys | Key Vault |
| Redis connection | App Service/Key Vault only when Redis is selected |
| Blob Storage connection | App Service/Key Vault only when AzureBlob is selected |
| SMTP App Password | App Service/Key Vault |

`.env` is gitignored (`.gitignore`). Use `.env.example` as a template.

`SeedUsers` is disabled by default. Development users or a one-time Production Admin require explicit enablement and external credentials; the production Admin password must be removed after bootstrap.

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

`BuggyController` remains a known test-only endpoint surface and should be removed or production-gated before public launch.

## HTTPS

- `UseHttpsRedirection()` enabled
- Development cert: `dotnet dev-certs https --trust`
- Production: forwarded headers are processed before HTTPS redirection; App Service terminates TLS and HSTS is enabled

## Stripe Webhook Security

- Signature validation via `EventUtility.ConstructEvent`
- Raw body required (do not parse before validation)
- `WebhookSecret` from Stripe Dashboard — never in source control

## Data Protection

| Data | Protection |
|------|------------|
| Passwords | Hashed by Identity |
| Refresh tokens | Opaque random database records, rotated and revocable (currently stored as token values) |
| Password reset tokens | Only SHA-256 hashes are stored; short-lived, revoked on replacement and single-use |
| Payment data | PCI scope minimized — Stripe handles card data |
| PII (email, address) | SQL encryption at rest (Azure SQL TDE) recommended |

## HTTP Security Headers (Recommended)

Implemented: HSTS in Production, `X-Content-Type-Options: nosniff`, and `Referrer-Policy: no-referrer`. CSP and explicit frame policy remain recommended hardening.

## Audit Trail

`IAuditService` / `AuditLog` entity records admin actions for compliance review.

## Dependency Security

```bash
dotnet list package --vulnerable
cd client && npm audit
```

Run before manual production publish. Current validation reports no vulnerable NuGet packages and no production npm dependency vulnerabilities; development-only npm advisories remain.

## Security Testing Checklist

- [ ] Unauthenticated access blocked on protected endpoints
- [ ] Customer cannot call admin APIs
- [ ] Invalid JWT rejected
- [ ] Expired refresh token rejected
- [ ] CORS blocks unknown origins
- [ ] Rate limit triggers at threshold
- [ ] Stripe webhook rejects invalid signatures
