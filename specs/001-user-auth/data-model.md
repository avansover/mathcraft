# Data Model: Authentication & User Management

**Feature**: 001-user-auth
**Date**: 2026-04-08

---

## Entities

### FamilyAccount

Represents the parent-owned account. Root of all data in the system.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | UUID | PK | Auto-generated |
| Email | string | Unique, required, max 255 | Lowercased before storage |
| PasswordHash | string | Required | PBKDF2/HMAC-SHA256 via IPasswordHasher |
| PinHash | string? | Nullable | Set when parent enables PIN protection |
| PinFailedAttempts | int | Default 0 | Reset to 0 on successful verify |
| PinLockedUntil | DateTime? | Nullable | Set after 5 failed PIN attempts |
| CreatedAt | DateTime | Required | UTC |
| UpdatedAt | DateTime | Required | UTC, updated on every change |

**Relationships**:
- Has many → PlayerProfile (cascade delete)
- Has many → RefreshToken (cascade delete)

**Validation rules**:
- Email MUST match valid email format
- Password MUST be minimum 8 characters before hashing
- PIN MUST be 4–6 digits if set

---

### PlayerProfile

Represents a child's identity within a family account. No password — selected by tapping.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | UUID | PK | Auto-generated |
| FamilyAccountId | UUID | FK → FamilyAccount, required | |
| DisplayName | string | Required, max 50 | Visible on profile selection screen |
| AvatarId | int | Required | References a fixed set of avatar options |
| Age | int | Required, 4–18 | Determines starting unlocked skill ranks |
| Gold | int | Default 0 | In-game currency |
| CreatedAt | DateTime | Required | UTC |
| UpdatedAt | DateTime | Required | UTC |

**Relationships**:
- Belongs to → FamilyAccount
- Has many → Character (cascade delete, defined in Character feature)
- Has many → InventoryItem (cascade delete, defined in Inventory feature)

**Validation rules**:
- DisplayName MUST NOT be empty or whitespace
- Age MUST be between 4 and 18 (inclusive)
- AvatarId MUST reference a valid avatar in the fixed set
- Max PlayerProfiles per FamilyAccount: configurable (default 10)

---

### RefreshToken

Represents an issued refresh token for a family account session.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | UUID | PK | Auto-generated |
| FamilyAccountId | UUID | FK → FamilyAccount, required | |
| TokenHash | string | Required | SHA-256 hash of the actual token |
| ExpiresAt | DateTime | Required | UTC, 30 days from issuance |
| IsRevoked | bool | Default false | Set on logout or token rotation |
| CreatedAt | DateTime | Required | UTC |

**Relationships**:
- Belongs to → FamilyAccount

**Validation rules**:
- Only one active (non-revoked, non-expired) token per account at a time
- On refresh: old token is revoked, new token is issued (rotation)

---

### PasswordResetToken

Represents a one-time token for password reset.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | UUID | PK | Auto-generated |
| FamilyAccountId | UUID | FK → FamilyAccount, required | |
| TokenHash | string | Required | HMAC-SHA256 hash of the token sent by email |
| ExpiresAt | DateTime | Required | UTC, 1 hour from issuance |
| IsUsed | bool | Default false | Set to true after successful reset |
| CreatedAt | DateTime | Required | UTC |

---

## State Transitions

### FamilyAccount Session
```
[No session] → Register/Login → [Active session]
[Active session] → Logout / Token expired → [No session]
[Active session] → Refresh → [Active session] (token rotated)
```

### PIN Protection
```
[No PIN] → Parent sets PIN → [PIN active]
[PIN active] → Parent removes PIN → [No PIN]
[PIN active] → 5 failed attempts → [Locked 30s] → [PIN active]
```

### Password Reset
```
[Has account] → Request reset → [Reset token issued]
[Reset token issued] → Click link + set password → [Reset complete, token marked used]
[Reset token issued] → 1 hour passes → [Token expired]
```

---

## Avatar Reference

Avatars are a fixed set — no custom uploads in Phase 1.
AvatarId maps to a pre-defined list of character images stored as static assets.
The avatar set and IDs are defined in frontend configuration — not stored in the database beyond the integer ID.
