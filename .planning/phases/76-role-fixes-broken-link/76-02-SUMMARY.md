---
phase: 76-role-fixes-broken-link
plan: 02
subsystem: ui
tags: [razor, aspnet-identity, role-check, navbar]

# Dependency graph
requires: []
provides:
  - "Kelola Data navbar item visible to all HC Identity role users regardless of SelectedView value"
affects: [76-role-fixes-broken-link]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Use User.IsInRole() for authorization decisions in Razor views — not SelectedView (profile field)"

key-files:
  created: []
  modified:
    - Views/Shared/_Layout.cshtml

key-decisions:
  - "76-02: Used User.IsInRole() for nav visibility — SelectedView is a profile/cosmetic field, not an auth source; HC users may have SelectedView set to a unit name during role-switching, causing nav to disappear incorrectly"

patterns-established:
  - "Role checks in Razor views: User.IsInRole(\"Admin\") || User.IsInRole(\"HC\") — never compare SelectedView string"

requirements-completed: [ROLE-02]

# Metrics
duration: 1min
completed: 2026-03-01
---

# Phase 76 Plan 02: Role Fixes & Broken Link — Kelola Data Nav Visibility Summary

**Navbar Kelola Data visibility switched from SelectedView string comparison to User.IsInRole() so HC users always see the menu item regardless of their active role-switch state.**

## Performance

- **Duration:** ~1 min
- **Started:** 2026-03-01T05:25:59Z
- **Completed:** 2026-03-01T05:27:00Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- Replaced `userRole == "Admin" || userRole == "HC"` with `User.IsInRole("Admin") || User.IsInRole("HC")` in the Kelola Data nav conditional
- HC users now see the Kelola Data nav item regardless of their SelectedView value (e.g., unit name from role-switching)
- SelectedView badge display (cosmetic) is unchanged — still shows current view context
- `dotnet build` passes with 0 errors, 0 warnings

## Task Commits

Each task was committed atomically:

1. **Task 1: Replace SelectedView navbar check with Identity role check** - `cc5abd0` (fix)

**Plan metadata:** (docs commit — see below)

## Files Created/Modified
- `Views/Shared/_Layout.cshtml` - Changed Kelola Data nav conditional from SelectedView string comparison to User.IsInRole() call

## Decisions Made
- Used `User.IsInRole()` instead of checking `SelectedView` because `SelectedView` is a user profile field that reflects the currently selected view context (Admin/HC/unit name), not the actual assigned Identity role. HC users who switch to a unit view would have SelectedView set to the unit name, causing the Kelola Data nav to disappear despite still holding the HC Identity role.
- The `userRole` variable (from SelectedView) is retained for the badge display — this is cosmetic and intentionally shows the current view context, not the Identity role.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Phase 76 now has both plans complete (76-01: ROLE-01 fixed, 76-02: ROLE-02 fixed, LINK-01 covered in 76-01)
- v2.6 Codebase Cleanup milestone complete — all 4 phases done

---
*Phase: 76-role-fixes-broken-link*
*Completed: 2026-03-01*
