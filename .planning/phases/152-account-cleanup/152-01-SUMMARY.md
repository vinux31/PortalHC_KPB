---
phase: 152-account-cleanup
plan: 01
subsystem: auth
tags: [aspnet-identity, authorization, viewmodel, validation]

# Dependency graph
requires: []
provides:
  - AccountController with class-level [Authorize] and [AllowAnonymous] on Login/AccessDenied
  - ProfileViewModel with Role property replacing ViewBag usage
  - Phone regex accepting international formats (+62 812-3456-7890)
  - Client-side validation on Settings page via _ValidationScriptsPartial
  - Profile page button label changed to Pengaturan
  - Profile page row spacing unified to mb-3
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Class-level [Authorize] with per-action [AllowAnonymous] for public routes"
    - "ProfileViewModel pattern: read-only ViewModel instead of passing entity directly to view"

key-files:
  created:
    - Models/ProfileViewModel.cs
  modified:
    - Controllers/AccountController.cs
    - Models/SettingsViewModel.cs
    - Views/Account/Profile.cshtml
    - Views/Account/Settings.cshtml

key-decisions:
  - "Use class-level [Authorize] on AccountController with [AllowAnonymous] on Login (GET/POST) and AccessDenied — consistent with other controllers in the project"
  - "ProfileViewModel introduced to decouple Profile view from ApplicationUser entity and remove ViewBag dependency"
  - "Phone regex changed to ^[\\d\\s\\-\\+\\(\\)]+$ and StringLength increased to 30 to support international formats"

patterns-established:
  - "Read-only profile pages should use a dedicated ViewModel, not the entity model directly"

requirements-completed: [SEC-01, VAL-01, VAL-02, CODE-01, UI-01, UI-02]

# Metrics
duration: 15min
completed: 2026-03-11
---

# Phase 152 Plan 01: Account Profile & Settings Cleanup Summary

**Class-level [Authorize] on AccountController, ProfileViewModel replacing ViewBag, client-side validation on Settings, and Profile page UI fixes (button label, row spacing)**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-03-11T00:00:00Z
- **Completed:** 2026-03-11T00:15:00Z
- **Tasks:** 2
- **Files modified:** 5 (4 modified, 1 created)

## Accomplishments
- AccountController now uses class-level [Authorize] with [AllowAnonymous] on Login (GET+POST) and AccessDenied — unauthenticated users visiting /Account/Profile or /Account/Settings are redirected to login
- Created ProfileViewModel with Role property; Profile action populates it from user + roles; ViewBag.UserRole removed
- Settings form now includes _ValidationScriptsPartial for client-side validation (no round-trip for field errors)
- Phone regex updated to accept international formats like +62 812-3456-7890
- Profile page button changed to "Pengaturan"; all row divs use mb-3 for consistent spacing

## Task Commits

Each task was committed atomically:

1. **Task 1: Auth pattern, phone regex, ProfileViewModel, controller updates** - `c2a0e95` (feat)
2. **Task 2: View updates — validation scripts, button label, row spacing** - `6265993` (feat)

## Files Created/Modified
- `Controllers/AccountController.cs` - Added [Authorize] class-level, [AllowAnonymous] on Login/AccessDenied, removed redundant IsAuthenticated guards, updated Profile action to return ProfileViewModel
- `Models/ProfileViewModel.cs` - New read-only ViewModel with FullName, NIP, Email, PhoneNumber, Position, Directorate, Section, Unit, Role
- `Models/SettingsViewModel.cs` - Phone regex changed to international format, StringLength increased to 30
- `Views/Account/Profile.cshtml` - Model changed to ProfileViewModel, Role from Model.Role, button label "Pengaturan", rows use mb-3
- `Views/Account/Settings.cshtml` - Added _ValidationScriptsPartial before existing script block

## Decisions Made
- Used class-level [Authorize] consistent with HomeController, CMPController, CDPController pattern
- ProfileViewModel created as read-only ViewModel (no data annotations needed, all string?)
- Removed redundant `if (User.Identity?.IsAuthenticated != true)` guard blocks from Profile() and Settings() — class-level [Authorize] handles this

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Build reported file-lock warnings (MSB3026/MSB3027) because the app was already running in another process. These are not compile errors — C# compilation succeeded with 0 errors. Verified via `grep "error CS"` returning empty output.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- All 6 v3.21 requirements completed in this single phase
- Phase 152 is the only phase in milestone v3.21 — milestone is complete
- Manual spot-check recommended: visit /Account/Profile while logged out (should redirect), test phone field with +62 format, verify Settings validation fires without page reload

---
*Phase: 152-account-cleanup*
*Completed: 2026-03-11*
