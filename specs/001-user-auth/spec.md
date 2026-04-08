# Feature Specification: Authentication & User Management

**Feature Branch**: `001-user-auth`
**Created**: 2026-04-08
**Status**: Draft
**Input**: Authentication and User Management

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Parent Creates Family Account (Priority: P1)

A parent visits Mathcraft for the first time and creates a family account using their email and a password. Once registered, they are taken to the profile management screen where they can start setting up player profiles for their kids.

**Why this priority**: Nothing else in the system works without a family account. It is the root of all user data, profiles, and progress.

**Independent Test**: A new visitor can register, log in, and reach the profile selection screen. Delivers a working account with no profiles yet.

**Acceptance Scenarios**:

1. **Given** a visitor on the registration screen, **When** they submit a valid email and password, **Then** a family account is created and they are redirected to the profile selection screen.
2. **Given** a visitor attempting to register, **When** they submit an email already in use, **Then** they see a clear error message and the account is not created.
3. **Given** a visitor attempting to register, **When** they submit an invalid email format or a password below minimum strength, **Then** they see specific validation errors and no account is created.

---

### User Story 2 - Parent Manages Player Profiles (Priority: P2)

A logged-in parent creates one or more player profiles under their family account. Each profile represents one child and holds a display name, an avatar, and the child's age. The parent can also edit or delete existing profiles.

**Why this priority**: Player profiles are the second prerequisite — without them, no child can play. Must exist before any game session can start.

**Independent Test**: A logged-in parent can create a profile with a name, avatar, and age, and see it appear in the profile list. Delivers a usable player profile.

**Acceptance Scenarios**:

1. **Given** a logged-in parent, **When** they create a profile with a display name, avatar, and age, **Then** the profile is saved and appears in the profile selection screen.
2. **Given** a logged-in parent, **When** they edit a player profile's age, **Then** the updated age is saved and reflected in the profile immediately.
3. **Given** a logged-in parent, **When** they delete a player profile, **Then** the profile and all its associated characters and progress are permanently removed.
4. **Given** a logged-in parent, **When** they attempt to create more than the maximum allowed profiles, **Then** they are prevented and shown an appropriate message.

---

### User Story 3 - Kid Selects Profile to Play (Priority: P3)

A child opens the app (or the parent is already logged in) and sees the profile selection screen. The child taps their own profile and enters the game — no password required.

**Why this priority**: This is the moment the child actually accesses the game. Depends on P1 and P2 being complete.

**Independent Test**: Given an account with at least one profile, a user can tap a profile and enter the game Hub. Delivers playable access for a child.

**Acceptance Scenarios**:

1. **Given** a family account with at least one profile, **When** a child taps their profile on the selection screen, **Then** they are taken directly to the game Hub as that player — no password required.
2. **Given** the app is reopened after a previous session, **When** the family account session is still active, **Then** the profile selection screen is shown immediately without requiring re-login.
3. **Given** the family account session has expired, **When** someone opens the app, **Then** the parent login screen is shown.

---

### User Story 4 - Parent Logs In to Existing Account (Priority: P4)

A returning parent enters their email and password to log back into their family account and reach the profile selection screen.

**Why this priority**: Returning users must be able to access their existing data. Depends on P1.

**Independent Test**: A parent with an existing account can log in and reach the profile selection screen with all their profiles intact.

**Acceptance Scenarios**:

1. **Given** a parent with an existing account, **When** they submit correct credentials, **Then** they are taken to the profile selection screen.
2. **Given** a parent attempting to log in, **When** they submit incorrect credentials, **Then** they see a clear error message and are not logged in.
3. **Given** a parent who has forgotten their password, **When** they request a password reset, **Then** they receive a reset email and can set a new password.

---

### Edge Cases

- What happens when a parent deletes the only profile on an account? → Account remains active, profile list is empty, parent is prompted to create a new profile.
- What happens if a child's age is changed after characters are created? → Age update affects future rank unlocks only; existing unlocked ranks are not removed.
- What happens if the family account session expires while a child is mid-session? → Child can finish the current game action, then is redirected to the parent login screen.
- What if two kids try to select the same profile simultaneously in hot-seat? → Each party slot requires deliberate profile selection — simultaneous conflict is not possible by design.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow a new user to register a family account with a valid email address and password.
- **FR-002**: System MUST validate email format and enforce minimum password strength on registration.
- **FR-003**: System MUST prevent duplicate accounts using the same email address.
- **FR-004**: System MUST allow a registered parent to log in with their email and password.
- **FR-005**: System MUST support password reset via a link sent to the registered email address.
- **FR-006**: System MUST maintain a persistent family account session so returning users skip the login screen.
- **FR-007**: System MUST allow a logged-in parent to create a player profile with a display name, avatar, and age.
- **FR-008**: System MUST allow a logged-in parent to edit or delete any player profile under their account.
- **FR-009**: System MUST display all player profiles on the profile selection screen after login.
- **FR-010**: System MUST allow a child to enter the game by tapping their profile — no password required at the profile level.
- **FR-011**: System MUST associate each player profile with: display name, avatar, age, characters, inventory, gold, and progress.
- **FR-012**: System MUST limit the number of player profiles per family account to a configurable maximum (default: 10).
- **FR-013**: System MUST use the player profile's age to determine starting unlocked skill ranks at character creation.
- **FR-014**: System MUST allow the parent to PIN-protect account settings to prevent children from modifying ages or deleting profiles.

### Key Entities

- **Family Account**: The parent-owned account. Holds credentials (email, password) and owns all player profiles. One account per family.
- **Player Profile**: A child's identity within the account. Holds display name, avatar, age, and links to characters, inventory, gold, and progress. No password — selected by tapping.
- **Session**: Represents an authenticated family account login. Persists across app restarts until explicitly logged out or expired.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new parent can complete registration and reach the profile selection screen in under 2 minutes.
- **SC-002**: A returning parent can log in and reach the profile selection screen in under 30 seconds.
- **SC-003**: A child can select their profile and reach the game Hub in under 5 seconds.
- **SC-004**: 100% of player profile deletions result in complete removal of all associated characters and progress.
- **SC-005**: Age changes to a player profile are reflected in skill rank availability within the same session.
- **SC-006**: Password reset emails are delivered and functional within 5 minutes of request.

---

## Assumptions

- Parents have access to their registered email address for password reset.
- The app is used on a shared family device — the family account session persists by default.
- Social login (Google, Apple) is out of scope for Phase 1 — email and password only.
- Email verification on registration is out of scope for Phase 1 — accounts are active immediately upon registration.
- A maximum of 10 player profiles per account is sufficient for all realistic family sizes.
- Avatar selection is from a fixed set of pre-defined options — custom image upload is out of scope for Phase 1.
- Deleting a player profile is a destructive, irreversible action — no soft delete or recovery mechanism.
