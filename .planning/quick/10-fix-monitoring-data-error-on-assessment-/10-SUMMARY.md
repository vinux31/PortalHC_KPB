---
phase: quick-10
plan: "01"
subsystem: api
tags: [cmp, assessment, monitoring, role-check, ajax, usermanager]

# Dependency graph
requires:
  - phase: quick-6
    provides: GetMonitorData AJAX endpoint (lazy-loaded monitoring tab)
provides:
  - Working monitoring tab on /CMP/Assessment?view=manage for HC/Admin users
  - Correct role resolution in GetMonitorData via _userManager.GetRolesAsync()
  - Hardened JS fetch with res.ok guard before JSON parsing
affects: [quick-6, cmp-monitoring]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "All CMPController actions resolve role via _userManager.GetRolesAsync() — never HttpContext.Session"
    - "JS fetch chains guard res.ok before calling res.json() to surface HTTP errors clearly"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/Assessment.cshtml

key-decisions:
  - "GetMonitorData role check replaced: Session.GetString('UserRole') was always null (key never set anywhere in app) — now uses _userManager.GetRolesAsync(user) matching all other actions"
  - "res.ok guard added in JS fetch before res.json() — prevents misleading JSON parse failure when server returns non-200 (403, 500)"

patterns-established:
  - "CMPController role pattern: var user = await _userManager.GetUserAsync(User); var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>(); var userRole = userRoles.FirstOrDefault();"

# Metrics
duration: 5min
completed: 2026-02-20
---

# Quick Task 10: Fix Monitoring Data Error on Assessment Summary

**Replaced broken session-based role check in GetMonitorData with _userManager.GetRolesAsync() — session key "UserRole" was never written anywhere in the application, causing permanent 403 Forbid() for all users**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-02-20T~current
- **Completed:** 2026-02-20
- **Tasks:** 1
- **Files modified:** 2

## Accomplishments

- Fixed "Failed to load monitoring data. Please refresh the page." error on Monitoring tab
- GetMonitorData now uses `_userManager.GetRolesAsync(user)` consistent with every other action in CMPController
- Uses `UserRoles.Admin` / `UserRoles.HC` constants instead of raw string literals
- Added `res.ok` guard in JS fetch so non-200 HTTP responses produce a clear error message instead of a confusing JSON parse failure in the catch handler

## Task Commits

1. **Task 1: Fix GetMonitorData role check and harden JS fetch error handling** - `5a1ddcd` (fix)

## Files Created/Modified

- `Controllers/CMPController.cs` - GetMonitorData role check replaced from Session.GetString to _userManager.GetRolesAsync
- `Views/CMP/Assessment.cshtml` - fetch() chain now checks res.ok before res.json()

## Decisions Made

- Used `UserRoles.Admin` and `UserRoles.HC` constants (not string literals) to match the convention used by Assessment(), Kkj(), Records(), and all other actions in CMPController.
- No other changes needed in JS — the existing `.catch()` handler at line 925 already displays the error message; only the guard condition needed adding.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

Build showed MSB3027/MSB3021 file-lock warnings (running HcPortal process holds the exe). Zero C# compiler errors (`dotnet build 2>&1 | grep "error CS"` returns empty). This is a normal development environment condition unrelated to the code change.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Monitoring tab on /CMP/Assessment?view=manage fully functional for HC/Admin users
- No blockers

---
*Phase: quick-10*
*Completed: 2026-02-20*
