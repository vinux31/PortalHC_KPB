---
phase: 72-dual-auth-login-flow
plan: 01
subsystem: auth
tags: [ldap, aspnet-identity, iauthorize, entity-framework, login]

# Dependency graph
requires:
  - phase: 71-ldap-auth-service-foundation
    provides: IAuthService interface, LocalAuthService, LdapAuthService, Program.cs DI factory registration

provides:
  - AccountController.Login POST using IAuthService abstraction (Local + AD transparent)
  - ApplicationUser without AuthSource property (global config is sole routing mechanism)
  - EF migration RemoveAuthSourceField (DROP AuthSource column from Users table)
  - AD mode profile sync (FullName/Email null-safe, before SignInAsync)
  - Unknown-user rejection with Indonesian message

affects: [73-ad-user-provisioning, any phase touching AccountController or ApplicationUser]

# Tech tracking
tech-stack:
  added: []
  patterns: [IAuthService DI abstraction for dual-mode login, AD profile sync on successful auth, null-safe property sync before session creation]

key-files:
  created:
    - Migrations/20260228113655_RemoveAuthSourceField.cs
    - Migrations/20260228113655_RemoveAuthSourceField.Designer.cs
  modified:
    - Controllers/AccountController.cs
    - Models/ApplicationUser.cs
    - Migrations/ApplicationDbContextModelSnapshot.cs

key-decisions:
  - "AuthSource field removed from ApplicationUser — global config (Authentication:UseActiveDirectory) is the sole routing mechanism, per-user source is obsolete"
  - "AD profile sync is non-fatal — try/catch around UpdateAsync ensures auth succeeds even if sync fails"
  - "FindByEmailAsync called AFTER IAuthService success, not before — prevents timing oracle attacks on email existence"
  - "SignInAsync called last, after sync — guarantees claims reflect latest AD data at session creation"
  - "IConfiguration injected directly into AccountController (not AuthenticationConfig POCO) — simpler, no additional DI registration needed"

patterns-established:
  - "IAuthService abstraction: Login POST calls AuthenticateAsync, then FindByEmailAsync, then profile sync, then SignInAsync"
  - "AD sync guard: !string.IsNullOrEmpty() on both sides before assigning — skips null AD attributes"

requirements-completed: [AUTH-05, AUTH-06, AUTH-07]

# Metrics
duration: 15min
completed: 2026-02-28
---

# Phase 72 Plan 01: Dual Auth Login Flow Summary

**AccountController.Login POST rewritten to use IAuthService abstraction with AD profile sync, unknown-user rejection, and AuthSource field removed from model and database**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-02-28T11:23:00Z
- **Completed:** 2026-02-28T11:38:47Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments
- Login POST now calls `_authService.AuthenticateAsync` instead of `PasswordSignInAsync` — transparently supports both Local and AD modes based on global config
- AuthSource field removed from ApplicationUser model and the `Users` SQL table (EF migration applied)
- Unknown DB users (AD auth succeeds, no HC pre-registration) receive "Akun Anda belum terdaftar. Hubungi HC." and cannot proceed
- AD mode: FullName and Email synced from AuthResult before SignInAsync, null values skipped, sync failure non-fatal

## Task Commits

Each task was committed atomically:

1. **Task 1: Remove AuthSource from ApplicationUser + create DROP migration** - `5b1f6cf` (feat)
2. **Task 2: Rewrite AccountController.Login POST with IAuthService** - `6301580` (feat)

**Plan metadata:** (docs commit below)

## Files Created/Modified
- `Controllers/AccountController.cs` - Login POST rewritten with IAuthService + AD sync; constructor updated with IAuthService + IConfiguration injection
- `Models/ApplicationUser.cs` - AuthSource property and [MaxLength(10)] attribute removed; unused DataAnnotations using directive removed
- `Migrations/20260228113655_RemoveAuthSourceField.cs` - EF migration: migrationBuilder.DropColumn(AuthSource, Users)
- `Migrations/20260228113655_RemoveAuthSourceField.Designer.cs` - EF migration designer snapshot
- `Migrations/ApplicationDbContextModelSnapshot.cs` - Updated model snapshot (AuthSource removed)

## Decisions Made
- AuthSource field removed entirely — global config (`Authentication:UseActiveDirectory`) is the sole routing mechanism; per-user AuthSource made the system brittle and is now obsolete
- AD profile sync uses non-fatal try/catch — auth succeeded, so login proceeds regardless of sync outcome
- `FindByEmailAsync` called AFTER IAuthService success (not before) — this also prevents leaking email existence via timing
- IConfiguration injected directly rather than AuthenticationConfig POCO — avoids additional DI setup for one boolean read

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Phase 72-01 complete: Login POST transparently routes Local vs AD via global config
- Phase 72 has no further plans — dual auth login flow complete
- Phase 73 (AD user provisioning) can now rely on the IAuthService abstraction in AccountController

## Self-Check: PASSED

- Controllers/AccountController.cs: FOUND
- Models/ApplicationUser.cs: FOUND (no AuthSource)
- Migrations/20260228113655_RemoveAuthSourceField.cs: FOUND
- Commit 5b1f6cf: FOUND
- Commit 6301580: FOUND
- AuthenticateAsync in AccountController: FOUND
- "belum terdaftar" message: FOUND
- PasswordSignInAsync removed from Login POST: CONFIRMED

---
*Phase: 72-dual-auth-login-flow*
*Completed: 2026-02-28*
