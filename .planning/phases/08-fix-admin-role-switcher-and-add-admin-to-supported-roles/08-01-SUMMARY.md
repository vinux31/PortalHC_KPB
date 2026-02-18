---
phase: 08-fix-admin-role-switcher-and-add-admin-to-supported-roles
plan: 01
subsystem: ui, auth
tags: [aspnet, razor, identity, role-switcher, dropdown, seed-data]

# Dependency graph
requires:
  - phase: 07-development-dashboard
    provides: _Layout.cshtml role-switcher dropdown and nav structure
provides:
  - AccountController.SwitchView accepts "Admin" as valid view value
  - _Layout.cshtml dropdown shows Admin View option with shield icon and checkmark
  - Admin seed user defaults to SelectedView = "Admin"
affects:
  - 08-02-CDPController-admin-view-fixes

# Tech tracking
tech-stack:
  added: []
  patterns:
    - allowedViews array in SwitchView extended by appending new string literal
    - Admin View separated from role-simulation options by dropdown-divider

key-files:
  created: []
  modified:
    - Controllers/AccountController.cs
    - Data/SeedData.cs
    - Views/Shared/_Layout.cshtml

key-decisions:
  - "Admin View separated from role-simulation views (HC, Atasan, Coach, Coachee) by dropdown-divider — Admin View is 'return to real Admin mode' not a simulated role"
  - "SeedData Admin default changed to SelectedView='Admin' — only affects fresh seeds; existing DB users unaffected"

patterns-established:
  - "Admin View dropdown item: bi-shield-fill icon, checkmark when SelectedView=='Admin', divider above it"

# Metrics
duration: 8min
completed: 2026-02-18
---

# Phase 8 Plan 01: Add Admin to allowedViews, Seed Default, and Dropdown Summary

**Admin added to SwitchView allowedViews array, role-switcher dropdown gains Admin View option with shield icon, and seed Admin user defaults to SelectedView="Admin"**

## Performance

- **Duration:** ~8 min
- **Started:** 2026-02-18T (session start)
- **Completed:** 2026-02-18
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- AccountController.SwitchView no longer returns BadRequest when Admin switches to "Admin" view — allowedViews now contains 5 entries
- _Layout.cshtml role-switcher dropdown shows Admin View item with bi-shield-fill icon, checkmark when active, separated from role-simulation options by a divider
- Admin seed user starts in "Admin" view rather than "HC" view on fresh seeds

## Task Commits

Each task was committed atomically:

1. **Task 1: Add "Admin" to allowedViews and fix seed default** - `b547ec6` (feat)
2. **Task 2: Add "Admin View" option to _Layout.cshtml dropdown** - `addaad1` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified
- `Controllers/AccountController.cs` - Added "Admin" to allowedViews array (line 138)
- `Data/SeedData.cs` - Changed Admin seed SelectedView from "HC" to "Admin"
- `Views/Shared/_Layout.cshtml` - Added Admin View dropdown item with divider after Coachee entry

## Decisions Made
- Admin View separated from role-simulation options (HC, Atasan, Coach, Coachee) by a dropdown-divider, since Admin View is "return to real Admin mode" not a simulated role
- SeedData change only affects fresh database seeds; existing Admin accounts in the DB retain their current SelectedView

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None - both tasks executed cleanly, build succeeded with 0 errors after each change.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- 08-01 prerequisites are complete: Admin can switch to Admin View without BadRequest, dropdown shows the option
- 08-02 (CDPController admin view fixes) can now proceed — all Admin view routing infrastructure is in place
- Existing Admin accounts in the DB will still have SelectedView="HC" until they manually switch; the seed only affects fresh installs

---
*Phase: 08-fix-admin-role-switcher-and-add-admin-to-supported-roles*
*Completed: 2026-02-18*

## Self-Check: PASSED

- Controllers/AccountController.cs: FOUND (contains "Admin" in allowedViews)
- Data/SeedData.cs: FOUND (SelectedView = "Admin" for Admin seed user)
- Views/Shared/_Layout.cshtml: FOUND (asp-route-view="Admin" present in dropdown)
- Commit b547ec6: FOUND (feat(08-01): add Admin to allowedViews and fix seed default)
- Commit addaad1: FOUND (feat(08-01): add Admin View option to role-switcher dropdown)
- Build: PASSED (0 errors)
