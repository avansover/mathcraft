# API Contracts: Authentication & User Management

**Feature**: 001-user-auth
**Date**: 2026-04-08
**Base URL**: `/api`

---

## Authentication Endpoints

### POST /api/auth/register
Register a new family account.

**Request**:
```json
{
  "email": "parent@example.com",
  "password": "securePassword123"
}
```

**Response 201**:
```json
{
  "accountId": "uuid",
  "email": "parent@example.com"
}
```
Sets HttpOnly refresh token cookie.

**Errors**:
- `400` — Invalid email format or password too weak
- `409` — Email already registered

---

### POST /api/auth/login
Log in with email and password.

**Request**:
```json
{
  "email": "parent@example.com",
  "password": "securePassword123"
}
```

**Response 200**:
```json
{
  "accessToken": "jwt...",
  "accountId": "uuid"
}
```
Sets HttpOnly refresh token cookie (30-day Max-Age).

**Errors**:
- `400` — Missing fields
- `401` — Invalid credentials

---

### POST /api/auth/logout
Revoke the current refresh token.

**Auth**: Access token required

**Response 204**: No content. Clears the refresh token cookie.

---

### GET /api/auth/refresh
Exchange the refresh token cookie for a new access token.

**Auth**: HttpOnly cookie (sent automatically by browser)

**Response 200**:
```json
{
  "accessToken": "jwt...",
  "accountId": "uuid"
}
```
Issues a new rotated refresh token cookie.

**Errors**:
- `401` — Cookie missing, expired, or revoked

---

### POST /api/auth/request-password-reset
Request a password reset email.

**Request**:
```json
{
  "email": "parent@example.com"
}
```

**Response 200**: Always returns 200 (do not reveal if email exists)
```json
{
  "message": "If this email is registered, a reset link has been sent."
}
```

---

### POST /api/auth/reset-password
Reset password using a valid reset token.

**Request**:
```json
{
  "token": "reset-token-from-email",
  "newPassword": "newSecurePassword123"
}
```

**Response 200**:
```json
{
  "message": "Password updated successfully."
}
```

**Errors**:
- `400` — Token invalid, expired, or already used
- `400` — New password too weak

---

## Player Profile Endpoints

All profile endpoints require a valid access token.

### GET /api/profiles
Get all player profiles for the authenticated family account.

**Response 200**:
```json
[
  {
    "id": "uuid",
    "displayName": "Noa",
    "avatarId": 3,
    "age": 9,
    "gold": 150
  }
]
```

---

### POST /api/profiles
Create a new player profile.

**Request**:
```json
{
  "displayName": "Noa",
  "avatarId": 3,
  "age": 9
}
```

**Response 201**:
```json
{
  "id": "uuid",
  "displayName": "Noa",
  "avatarId": 3,
  "age": 9,
  "gold": 0
}
```

**Errors**:
- `400` — Validation error (missing name, invalid age, invalid avatarId)
- `409` — Maximum profile limit reached

---

### PUT /api/profiles/{id}
Update an existing player profile.

**Request** (all fields optional):
```json
{
  "displayName": "Noa",
  "avatarId": 5,
  "age": 10
}
```

**Response 200**: Updated profile object (same shape as POST response)

**Errors**:
- `400` — Validation error
- `403` — Profile does not belong to this account
- `404` — Profile not found

---

### DELETE /api/profiles/{id}
Permanently delete a player profile and all associated data.

**Response 204**: No content

**Errors**:
- `403` — Profile does not belong to this account
- `404` — Profile not found

---

## Account Settings Endpoints

### POST /api/account/set-pin
Set or update the account PIN.

**Auth**: Access token required

**Request**:
```json
{
  "pin": "1234"
}
```

**Response 200**:
```json
{
  "message": "PIN set successfully."
}
```

**Errors**:
- `400` — PIN must be 4–6 digits

---

### POST /api/account/verify-pin
Verify the PIN before accessing protected settings.

**Auth**: Access token required

**Request**:
```json
{
  "pin": "1234"
}
```

**Response 200**:
```json
{
  "verified": true
}
```

**Errors**:
- `401` — Incorrect PIN
- `429` — Too many failed attempts, locked for 30 seconds

---

### DELETE /api/account/pin
Remove the PIN from the account.

**Auth**: Access token required + must pass PIN verification first

**Response 204**: No content
