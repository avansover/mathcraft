# Research: Authentication & User Management

**Feature**: 001-user-auth
**Date**: 2026-04-08

---

## 1. Authentication Mechanism

**Decision**: JWT — refresh token in an HttpOnly cookie, access token in memory (React state)

**Rationale**: The family device scenario requires persistence across browser restarts. Raw localStorage JWT is XSS-vulnerable. Server-side sessions need a session store (Redis/DB). The right balance: a long-lived refresh token stored in an HttpOnly, SameSite=Strict cookie + a short-lived (15min) access token held in memory. On React startup, silently call `/api/auth/refresh` — the browser auto-sends the cookie and returns a fresh access token.

**Alternatives considered**:
- Server-side sessions: simpler revocation but requires a session store — added infrastructure.
- localStorage JWT: easy but XSS-vulnerable, ruled out for any app with children.
- ASP.NET Core cookie Identity: viable but less clean for a separate React SPA consuming a REST API.

---

## 2. Password Hashing

**Decision**: `IPasswordHasher<T>` from ASP.NET Core Identity — used standalone, without the full Identity stack.

**Rationale**: Uses PBKDF2/HMAC-SHA256 with a random salt. Secure, well-maintained, and ships with the framework. No additional NuGet packages needed. The full `UserManager`/`RoleManager` machinery is not needed for a simple email+password app — just inject `IPasswordHasher<FamilyAccount>` and use `HashPassword` / `VerifyHashedPassword`.

**Alternatives considered**:
- `BCrypt.Net-Next`: solid algorithm but adds a dependency for something the framework already provides.
- Manual PBKDF2 via `KeyDerivation.Pbkdf2`: Microsoft explicitly discourages for new apps.
- Argon2: strongest, but overkill and requires a third-party library for a low-traffic family app.

---

## 3. PIN Protection

**Decision**: Store a nullable `PinHash` column on the `FamilyAccount` entity, hashed with the same `IPasswordHasher<T>`. Add API-level rate limiting (5 attempts, then 30-second lockout) to compensate for the small keyspace of a 4-6 digit PIN.

**Rationale**: A PIN is functionally a short password — treat it identically. Never store plain or encrypted (reversible). A distinct `POST /api/account/verify-pin` endpoint validates the PIN and returns a short-lived "settings access" boolean — does not reuse the main auth token.

**Alternatives considered**:
- Separate PIN table: unnecessary normalization for a single nullable field.
- AES encryption: wrong tool — you never need to recover the original PIN, only verify it.
- Plain SHA256: no salt, brute-forceable for short PINs — ruled out.

---

## 4. Persistent Session Strategy

**Decision**: HttpOnly cookie with `Max-Age=30 days` for the refresh token. Refresh token hash stored in PostgreSQL with expiry timestamp. Rotate refresh token on each use.

**Rationale**: "Remember me by default" means the cookie must survive browser restarts — requires a concrete `Max-Age`, not a session cookie. On React app startup, call `GET /api/auth/refresh` before rendering. If it succeeds → user is logged in (profile selection screen). If it fails → parent login screen. Refresh token rotation limits exposure if a token is ever captured.

**Alternatives considered**:
- Sliding server-side sessions: more infrastructure (session store needed).
- Long-lived access tokens: eliminates refresh but makes revocation impossible.

---

## 5. Password Reset Emails

**Decision**: SendGrid free tier via the `SendGrid` NuGet package.

**Rationale**: Free tier gives 100 emails/day — sufficient for a family app. No mail server setup, handles deliverability/SPF/DKIM. Flow: request reset → generate HMAC token → store hash + 1hr expiry in DB → email reset link → validate token → allow password change. Token is single-use and expires in 1 hour.

**Alternatives considered**:
- Raw SMTP (Gmail): free but rate-limited, poor deliverability, fragile in production.
- Resend.com: modern API, 3,000/month free — strong alternative if SendGrid dashboard is painful.
- Self-hosted SMTP: operational overhead, overkill for a family app.

---

## 6. Backend Architecture — MediatR (Full CQRS)

**Decision**: MediatR for all commands and queries across the entire backend — including auth, profiles, and all future game features. No traditional service layer.

**Rationale**: Every controller action dispatches a command or query via `IMediator.Send()`. Handlers contain all business logic. Pipeline behaviors (validation, logging, error handling) apply universally — one place to add cross-cutting concerns across the entire app. This is especially valuable for game actions (UseSkill, MoveCharacter, EnterRoom) which are naturally discrete commands, but applying it everywhere ensures a consistent, predictable codebase.

**Pattern**:
```
Controller → IMediator → [ValidationBehavior → LoggingBehavior] → Handler → DbContext
```

**Unified response wrapper** — all handlers return `Result<T>`:
```
Result<T>
├── Success: bool
├── Data: T?
├── Error: string?
└── ErrorCode: ErrorCode enum
```

The frontend always receives the same response shape regardless of endpoint.

**Alternatives considered**:
- Traditional Controller → Service → Repository: simpler for CRUD but inconsistent once game logic grows. Cross-cutting concerns require repetition across every service.
- MediatR for game only, services for CRUD: hybrid approach, two patterns to maintain. Ruled out in favour of full consistency.

---

## 7. Database Approach — EF Core Code-First

**Decision**: Entity Framework Core with code-first migrations against PostgreSQL.

**Rationale**: Entities are defined as C# classes. EF Core generates and manages the schema via migrations, which are version-controlled alongside the code. `AppDbContext` owns all `DbSet<>` entities. Connection string stored in environment variables only — never committed.

**Alternatives considered**:
- DB-first (generate entities from existing schema): no existing schema, so code-first is the natural starting point.
- Dapper (raw SQL): more control but more boilerplate for CRUD. EF Core is sufficient for this scale.
- Raw ADO.NET: unnecessary complexity for a family-scale app.

---

## Summary

| Topic | Decision |
|-------|----------|
| Auth mechanism | JWT — refresh token in HttpOnly cookie, access token in memory |
| Password hashing | `IPasswordHasher<T>` from ASP.NET Core (standalone) |
| PIN storage | `PinHash` on `FamilyAccount` + rate limiting |
| Session persistence | HttpOnly cookie, 30-day Max-Age, token rotation |
| Password reset emails | SendGrid free tier, HMAC token, 1hr expiry |
| Backend architecture | MediatR full CQRS — all commands and queries, unified `Result<T>` response |
| Database | EF Core code-first migrations, PostgreSQL, connection string in env vars |
