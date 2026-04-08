# Tasks: Authentication & User Management

**Feature**: 001-user-auth**Date**: 2026-04-08**Branch**: `001-user-auth`**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Contracts**: [contracts/api.md](contracts/api.md)

---

## How to Read This File

-   Tasks are grouped by layer, ordered by dependency.
-   Each task has a unique ID, a clear deliverable, and the files it creates/touches.
-   Work top-to-bottom within a group; groups can overlap once their blockers are done.
-   Mark tasks `[x]` as you complete them.

---

## Group 0 — Project Bootstrap

> Do once. Everything else depends on this.

-    **T-001** — Scaffold the .NET 8 Web API project
    
    -   `dotnet new webapi -n Mathcraft.Server --use-controllers -o server`
    -   Delete the `WeatherForecast` example files
    -   Creates: `server/`
-    **T-002** — Add NuGet packages
    
    -   `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Tools`
    -   `MediatR`, `MediatR.Extensions.Microsoft.DependencyInjection`
    -   `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`
    -   `Microsoft.AspNetCore.Identity.Core` (for `IPasswordHasher<T>`)
    -   `Microsoft.IdentityModel.Tokens`, `System.IdentityModel.Tokens.Jwt`
    -   `SendGrid`
    -   Creates: `server/Mathcraft.Server.csproj` (updated)
-    **T-003** — Scaffold the React + TypeScript frontend
    
    -   `npm create vite@latest client -- --template react-ts`
    -   Add `react-router-dom`, `axios`
    -   Creates: `client/`
-    **T-004** — Configure environment variables
    
    -   Add `appsettings.Development.json` with placeholder keys (no real secrets)
    -   Add `.env.local` for the frontend
    -   Add both to `.gitignore`
    -   Required vars: `DATABASE_URL`, `JWT_SECRET`, `SENDGRID_API_KEY`, `SENDGRID_FROM_EMAIL`, `REFRESH_TOKEN_EXPIRY_DAYS`, `MAX_PROFILES_PER_ACCOUNT`
    -   Creates: `server/appsettings.Development.json`, `client/.env.local`, `.gitignore`

---

## Group 1 — Common Infrastructure

> Build once, used by every feature forever.

-    **T-005** — Implement `Result<T>` unified response wrapper
    
    -   `Success: bool`, `Data: T?`, `Error: string?`, `ErrorCode: ErrorCode enum`
    -   Add `ErrorCode` enum: `None`, `NotFound`, `Conflict`, `Unauthorized`, `Forbidden`, `Validation`, `RateLimited`
    -   Creates: `server/src/Common/Result.cs`, `server/src/Common/ErrorCode.cs`
-    **T-006** — Implement `ValidationBehavior` pipeline behavior
    
    -   Runs all `IValidator<TRequest>` validators before the handler
    -   Returns `Result<T>` with `ErrorCode.Validation` on failure
    -   Creates: `server/src/Pipeline/ValidationBehavior.cs`
-    **T-007** — Implement `LoggingBehavior` pipeline behavior
    
    -   Logs request name, duration, and success/failure
    -   Creates: `server/src/Pipeline/LoggingBehavior.cs`
-    **T-008** — Implement `ErrorHandlingBehavior` pipeline behavior
    
    -   Wraps unhandled exceptions and returns a safe `Result<T>` error
    -   Creates: `server/src/Pipeline/ErrorHandlingBehavior.cs`
-    **T-009** — Register MediatR, pipeline behaviors, and FluentValidation in `Program.cs`
    
    -   Order: `ValidationBehavior` → `LoggingBehavior` → `ErrorHandlingBehavior`
    -   Touches: `server/src/Program.cs`

---

## Group 2 — Data Models & Database

> Defines the schema. Migration must be applied before any handler can run.

-    **T-010** — Implement `FamilyAccount` entity
    
    -   Fields per `data-model.md`: `Id`, `Email`, `PasswordHash`, `PinHash?`, `PinFailedAttempts`, `PinLockedUntil?`, `CreatedAt`, `UpdatedAt`
    -   Creates: `server/src/Models/FamilyAccount.cs`
-    **T-011** — Implement `PlayerProfile` entity
    
    -   Fields: `Id`, `FamilyAccountId` (FK), `DisplayName`, `AvatarId`, `Age`, `Gold`, `CreatedAt`, `UpdatedAt`
    -   Creates: `server/src/Models/PlayerProfile.cs`
-    **T-012** — Implement `RefreshToken` entity
    
    -   Fields: `Id`, `FamilyAccountId` (FK), `TokenHash`, `ExpiresAt`, `IsRevoked`, `CreatedAt`
    -   Creates: `server/src/Models/RefreshToken.cs`
-    **T-013** — Implement `PasswordResetToken` entity
    
    -   Fields: `Id`, `FamilyAccountId` (FK), `TokenHash`, `ExpiresAt`, `IsUsed`, `CreatedAt`
    -   Creates: `server/src/Models/PasswordResetToken.cs`
-    **T-014** — Implement `AppDbContext`
    
    -   Register all four `DbSet<>`s
    -   Configure cascade deletes: `FamilyAccount` → `PlayerProfile`, `RefreshToken`, `PasswordResetToken`
    -   Unique index on `FamilyAccount.Email`
    -   Creates: `server/src/Data/AppDbContext.cs`
-    **T-015** — Generate and apply initial EF Core migration
    
    -   `dotnet ef migrations add InitialSchema`
    -   `dotnet ef database update`
    -   Creates: `server/src/Data/Migrations/`

---

## Group 3 — Auth Feature (Backend)

> Depends on Groups 1 and 2.

-    **T-016** — Implement JWT token generation helper
    
    -   Signs a short-lived access token (15 min) using `JWT_SECRET`
    -   Creates: `server/src/Common/JwtTokenService.cs`
-    **T-017** — Implement refresh token helpers
    
    -   Generate a cryptographically random token string
    -   Hash it (SHA-256) for storage
    -   Set as HttpOnly, SameSite=Strict cookie with 30-day Max-Age
    -   Creates: `server/src/Common/RefreshTokenService.cs`
-    **T-018** — Implement `RegisterCommand` handler
    
    -   Validates email format, password min 8 chars, checks for duplicate email
    -   Hashes password with `IPasswordHasher<FamilyAccount>`
    -   Creates `FamilyAccount`, issues access token + refresh token cookie
    -   Returns `201` with `{ accountId, email }`
    -   Errors: `400` validation, `409` duplicate email
    -   Creates: `server/src/Features/Auth/RegisterCommand.cs`
-    **T-019** — Implement `LoginCommand` handler
    
    -   Verifies credentials via `IPasswordHasher<T>.VerifyHashedPassword`
    -   Issues new access token + rotated refresh token cookie
    -   Returns `200` with `{ accessToken, accountId }`
    -   Errors: `400` missing fields, `401` invalid credentials
    -   Creates: `server/src/Features/Auth/LoginCommand.cs`
-    **T-020** — Implement `LogoutCommand` handler
    
    -   Revokes the current `RefreshToken` record (set `IsRevoked = true`)
    -   Clears the cookie
    -   Returns `204`
    -   Creates: `server/src/Features/Auth/LogoutCommand.cs`
-    **T-021** — Implement `RefreshTokenQuery` handler
    
    -   Reads refresh token from HttpOnly cookie
    -   Validates: not expired, not revoked, matches a stored `TokenHash`
    -   Rotates: revokes old token, issues new token + cookie
    -   Returns `200` with `{ accessToken, accountId }`
    -   Error: `401` on any invalid state
    -   Creates: `server/src/Features/Auth/RefreshTokenQuery.cs`
-    **T-022** — Implement `RequestPasswordResetCommand` handler
    
    -   Looks up account by email; always returns `200` regardless of match
    -   If found: generates HMAC token, stores hash + 1hr expiry, sends SendGrid email with reset link
    -   Creates: `server/src/Features/Auth/RequestPasswordResetCommand.cs`
-    **T-023** — Implement `ResetPasswordCommand` handler
    
    -   Validates token (not expired, not used, hash matches)
    -   Validates new password strength
    -   Updates `PasswordHash`, marks token `IsUsed = true`
    -   Returns `200`
    -   Errors: `400` invalid/expired token, `400` weak password
    -   Creates: `server/src/Features/Auth/ResetPasswordCommand.cs`
-    **T-024** — Implement `AuthController`
    
    -   Routes: `POST /register`, `POST /login`, `POST /logout`, `GET /refresh`, `POST /request-password-reset`, `POST /reset-password`
    -   Each action calls `IMediator.Send()` and maps `Result<T>` to HTTP response
    -   `[Authorize]` on `logout` only
    -   Creates: `server/src/Controllers/AuthController.cs`

---

## Group 4 — Profiles Feature (Backend)

> Depends on Groups 1 and 2. All endpoints require `[Authorize]`.

-    **T-025** — Implement `GetProfilesQuery` handler
    
    -   Returns all `PlayerProfile` records for `FamilyAccountId` from JWT claim
    -   Returns `200` with profile array
    -   Creates: `server/src/Features/Profiles/GetProfilesQuery.cs`
-    **T-026** — Implement `CreateProfileCommand` handler
    
    -   Validates: `DisplayName` non-empty, `Age` 4–18, `AvatarId` in valid set, under max profile limit
    -   Creates `PlayerProfile` with `Gold = 0`
    -   Returns `201` with profile object
    -   Errors: `400` validation, `409` at limit
    -   Creates: `server/src/Features/Profiles/CreateProfileCommand.cs`
-    **T-027** — Implement `UpdateProfileCommand` handler
    
    -   Validates profile belongs to calling account
    -   Updates any provided fields (all optional)
    -   Returns `200` with updated profile
    -   Errors: `400` validation, `403` wrong account, `404` not found
    -   Creates: `server/src/Features/Profiles/UpdateProfileCommand.cs`
-    **T-028** — Implement `DeleteProfileCommand` handler
    
    -   Validates profile belongs to calling account
    -   Hard deletes (cascade removes all child records via EF Core)
    -   Returns `204`
    -   Errors: `403` wrong account, `404` not found
    -   Creates: `server/src/Features/Profiles/DeleteProfileCommand.cs`
-    **T-029** — Implement `ProfilesController`
    
    -   Routes: `GET /api/profiles`, `POST /api/profiles`, `PUT /api/profiles/{id}`, `DELETE /api/profiles/{id}`
    -   All actions `[Authorize]`
    -   Creates: `server/src/Controllers/ProfilesController.cs`

---

## Group 5 — Account Settings Feature (Backend)

> Depends on Groups 1 and 2. All endpoints require `[Authorize]`.

-    **T-030** — Implement `SetPinCommand` handler
    
    -   Validates PIN is 4–6 digits
    -   Hashes with `IPasswordHasher<FamilyAccount>`, stores in `FamilyAccount.PinHash`
    -   Returns `200`
    -   Error: `400` invalid PIN format
    -   Creates: `server/src/Features/Account/SetPinCommand.cs`
-    **T-031** — Implement `VerifyPinCommand` handler
    
    -   Checks `PinLockedUntil` — if locked, return `429`
    -   Verifies PIN against `PinHash`
    -   On failure: increment `PinFailedAttempts`; on 5th failure set `PinLockedUntil = now + 30s`
    -   On success: reset `PinFailedAttempts = 0`
    -   Returns `200 { verified: true }` or `401` incorrect PIN
    -   Creates: `server/src/Features/Account/VerifyPinCommand.cs`
-    **T-032** — Implement `DeletePinCommand` handler
    
    -   Clears `PinHash`, `PinFailedAttempts`, `PinLockedUntil`
    -   Returns `204`
    -   Note: caller must verify PIN first (frontend responsibility; no server-side gate needed here)
    -   Creates: `server/src/Features/Account/DeletePinCommand.cs`
-    **T-033** — Implement `AccountController`
    
    -   Routes: `POST /api/account/set-pin`, `POST /api/account/verify-pin`, `DELETE /api/account/pin`
    -   All actions `[Authorize]`
    -   Creates: `server/src/Controllers/AccountController.cs`

---

## Group 6 — Backend Tests

> Write tests after the handler is implemented. Unit tests first, integration second.

-    **T-034** — Unit test: `RegisterCommand` — valid input, duplicate email, weak password
    
    -   Creates: `server/tests/Unit/Auth/RegisterCommandTests.cs`
-    **T-035** — Unit test: `LoginCommand` — valid credentials, wrong password, missing fields
    
    -   Creates: `server/tests/Unit/Auth/LoginCommandTests.cs`
-    **T-036** — Unit test: `RefreshTokenQuery` — valid token, expired token, revoked token
    
    -   Creates: `server/tests/Unit/Auth/RefreshTokenQueryTests.cs`
-    **T-037** — Unit test: `CreateProfileCommand` — valid, age out of range, over limit
    
    -   Creates: `server/tests/Unit/Profiles/CreateProfileCommandTests.cs`
-    **T-038** — Unit test: `UpdateProfileCommand` — valid, wrong account (403), not found (404)
    
    -   Creates: `server/tests/Unit/Profiles/UpdateProfileCommandTests.cs`
-    **T-039** — Unit test: `DeleteProfileCommand` — valid, wrong account (403), not found (404)
    
    -   Creates: `server/tests/Unit/Profiles/DeleteProfileCommandTests.cs`
-    **T-040** — Unit test: `VerifyPinCommand` — correct PIN, wrong PIN, lockout at 5 failures
    
    -   Creates: `server/tests/Unit/Account/VerifyPinCommandTests.cs`
-    **T-041** — Integration test: full register → create profile → profile select flow
    
    -   Uses real PostgreSQL (test database), real HTTP stack
    -   Covers quickstart steps 1–3
    -   Creates: `server/tests/Integration/Auth/RegistrationFlowTests.cs`
-    **T-042** — Integration test: persistent session (refresh token rotation)
    
    -   Issues token, calls `/refresh`, verifies old token is revoked
    -   Creates: `server/tests/Integration/Auth/RefreshTokenTests.cs`

---

## Group 7 — Frontend

> Depends on Group 3 and 4 backend endpoints being running.

-    **T-043** — Implement `authService.ts`
    
    -   Wraps all API calls: `register`, `login`, `logout`, `refreshToken`, `requestPasswordReset`, `resetPassword`
    -   Stores access token in React state (never localStorage)
    -   On startup: calls `/api/auth/refresh` silently; routes based on result
    -   Creates: `client/src/services/authService.ts`
-    **T-044** — Implement `RegisterPage.tsx`
    
    -   Form: email + password fields, submit button
    -   On success: navigate to `/profiles`
    -   Shows field-level validation errors from API
    -   Creates: `client/src/pages/RegisterPage.tsx`
-    **T-045** — Implement `LoginPage.tsx`
    
    -   Form: email + password fields, "Forgot password?" link
    -   On success: navigate to `/profiles`
    -   Shows error on invalid credentials
    -   Creates: `client/src/pages/LoginPage.tsx`
-    **T-046** — Implement `ProfileSelectPage.tsx`
    
    -   Fetches and displays all profiles as tappable `ProfileCard` components
    -   "Add Profile" button visible to parent
    -   Tapping a profile navigates to `/hub` (stub route for now)
    -   Creates: `client/src/pages/ProfileSelectPage.tsx`, `client/src/components/ProfileCard.tsx`
-    **T-047** — Implement `ProfileManagePage.tsx`
    
    -   Create, edit, delete profiles
    -   Uses `AvatarPicker` component (fixed avatar set)
    -   Enforces max-profile limit with UI message
    -   Creates: `client/src/pages/ProfileManagePage.tsx`, `client/src/components/AvatarPicker.tsx`
-    **T-048** — Wire up React Router
    
    -   Routes: `/` (redirect to `/profiles` or `/login` based on session), `/register`, `/login`, `/profiles`, `/profiles/manage`
    -   Protected route wrapper: redirect to `/login` if no access token
    -   Touches: `client/src/main.tsx` or `App.tsx`
-    **T-049** — Implement forgot password / reset password UI
    
    -   `ForgotPasswordPage.tsx`: email input, submit shows confirmation message
    -   `ResetPasswordPage.tsx`: reads `?token=` from URL, new password form
    -   Creates: `client/src/pages/ForgotPasswordPage.tsx`, `client/src/pages/ResetPasswordPage.tsx`

---

## Group 8 — End-to-End Validation

> Final gate. Follow `quickstart.md` exactly.

-    **T-050** — Run quickstart step 1: Register and reach profile selection screen
-    **T-051** — Run quickstart step 2: Create a player profile
-    **T-052** — Run quickstart step 3: Kid selects profile → game Hub (stub)
-    **T-053** — Run quickstart step 4: Close browser, reopen → auto-login via refresh token
-    **T-054** — Run quickstart step 5 & 6: Logout, log back in
-    **T-055** — Run quickstart step 7: Password reset email flow
-    **T-056** — Run quickstart step 8: PIN setup, wrong PIN lockout
-    **T-057** — Run quickstart step 9: Edit and delete profile
-    **T-058** — Run quickstart step 10: Profile limit enforcement

---

## Progress Summary

Group

Total

Done

0 — Bootstrap

4

0

1 — Common Infra

5

0

2 — Data Models

6

0

3 — Auth Backend

9

0

4 — Profiles Backend

5

0

5 — Account Backend

4

0

6 — Backend Tests

9

0

7 — Frontend

7

0

8 — E2E Validation

9

0

**Total**

**58**

**0**