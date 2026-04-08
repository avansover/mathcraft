# Quickstart: Authentication & User Management

**Feature**: 001-user-auth
**Date**: 2026-04-08

This guide validates that the Authentication & User Management feature is working correctly end to end.

---

## Prerequisites

- Backend running on `http://localhost:5000`
- Frontend running on `http://localhost:3000`
- PostgreSQL running with migrations applied
- SendGrid API key configured in environment variables (or use a mock for local testing)

---

## Validation Steps

### 1. Register a new family account
1. Open the app at `http://localhost:3000`
2. Click **Register**
3. Enter a valid email and password (min 8 characters)
4. Submit
5. ✅ Expected: Redirected to the profile selection screen (empty)

### 2. Create a player profile
1. From the profile selection screen, click **Add Profile**
2. Enter a display name, select an avatar, and set age to `9`
3. Submit
4. ✅ Expected: Profile appears on the profile selection screen

### 3. Select a profile as a kid
1. On the profile selection screen, tap the profile just created
2. ✅ Expected: Redirected to the game Hub with no password prompt

### 4. Persistent session (remember me)
1. Close and reopen the browser
2. Navigate to `http://localhost:3000`
3. ✅ Expected: Profile selection screen shown immediately (no login required)

### 5. Log out
1. From account settings, click **Log Out**
2. ✅ Expected: Redirected to parent login screen, cookie cleared

### 6. Log back in
1. Enter the registered email and password
2. ✅ Expected: Profile selection screen with the previously created profile

### 7. Password reset
1. On the login screen, click **Forgot Password**
2. Enter the registered email
3. ✅ Expected: Message shown (regardless of whether email exists)
4. Check email inbox for a reset link
5. Click the link, enter a new password
6. ✅ Expected: Password updated, can log in with new password

### 8. PIN protection
1. Log in as parent
2. Go to account settings → Enable PIN
3. Set PIN to `1234`
4. Navigate away, return to account settings
5. ✅ Expected: PIN prompt appears before settings are accessible
6. Enter wrong PIN 5 times
7. ✅ Expected: Locked for 30 seconds with appropriate message

### 9. Edit and delete a profile
1. Log in as parent
2. Go to profile management
3. Edit the profile's age from `9` to `10`
4. ✅ Expected: Age updated and reflected immediately
5. Delete the profile
6. ✅ Expected: Profile removed from the selection screen

### 10. Profile limit
1. Create 10 player profiles
2. Attempt to create an 11th
3. ✅ Expected: Error message shown, profile not created

---

## Environment Variables Required

```
DATABASE_URL=postgresql://...
JWT_SECRET=<random 256-bit secret>
SENDGRID_API_KEY=<your key>
SENDGRID_FROM_EMAIL=noreply@mathcraft.app
REFRESH_TOKEN_EXPIRY_DAYS=30
MAX_PROFILES_PER_ACCOUNT=10
```
