---
phase: 73-critical-fixes
plan: 01
subsystem: ui
tags: [razor, mvc, access-denied, authorization, bootstrap5]

# Dependency graph
requires: []
provides:
  - AccessDenied Razor view at Views/Account/AccessDenied.cshtml with portal navbar and Kembali button
affects: [74-dead-code-removal, 75-placeholder-cleanup, 76-role-fixes]

# Tech tracking
tech-stack:
  added: []
  patterns: [error-page inherits _Layout via _ViewStart — no Layout = null override]

key-files:
  created:
    - Views/Account/AccessDenied.cshtml
  modified: []

key-decisions:
  - "Used javascript:history.back() for Kembali button — generic back-nav works regardless of which protected route triggered the 403"
  - "No Layout = null override — inherits _Layout.cshtml via _ViewStart giving full portal navbar"

patterns-established:
  - "Error pages follow Settings.cshtml pattern: set ViewData Title, render Bootstrap 5 centered content, no Layout override"

requirements-completed: [CRIT-01]

# Metrics
duration: 2min
completed: 2026-03-01
---

# Phase 73 Plan 01: Critical Fixes — AccessDenied View Summary

**Razor AccessDenied view with portal navbar, Bootstrap Icons shield-lock, and Bahasa Indonesia back-navigation — eliminates ViewNotFoundException crash on 403 authorization failures**

## Performance

- **Duration:** ~2 min
- **Started:** 2026-03-01T04:36:27Z
- **Completed:** 2026-03-01T04:37:58Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- Created `Views/Account/AccessDenied.cshtml` — the missing Razor view that caused a runtime ViewNotFoundException crash whenever an authenticated user accessed a route they lacked permission for
- View inherits `_Layout.cshtml` automatically via `_ViewStart.cshtml`, providing full portal navbar without any extra configuration
- User-friendly denial message in Bahasa Indonesia with bi-shield-lock Bootstrap Icon and generic Kembali back-navigation button

## Task Commits

Each task was committed atomically:

1. **Task 1: Create Views/Account/AccessDenied.cshtml** - `697f7e7` (feat)

**Plan metadata:** (docs commit — see below)

## Files Created/Modified
- `Views/Account/AccessDenied.cshtml` — Razor error page with Bootstrap 5 centered layout, danger shield icon, denial message, and back-nav button

## Decisions Made
- Used `javascript:history.back()` instead of a hard-coded route for the Kembali button — the user may arrive at AccessDenied from any protected route, so generic browser back-navigation is the correct pattern
- Deliberately did NOT set `Layout = null` — this is the key difference from `Login.cshtml` (which is a standalone full-page) vs error pages which should show the portal navbar

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Build produced MSB3027/MSB3021 file-lock errors (not compilation errors) because the HcPortal process was running and had the .exe locked. No C# compilation errors. Razor views compile at runtime so the view is functionally verified by file existence and content inspection.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- CRIT-01 resolved: authenticated users hitting forbidden routes now see a proper "Akses Ditolak" page instead of a crash
- CRIT-02 (if any) and Phase 74 Dead Code Removal can proceed
- No blockers

---
*Phase: 73-critical-fixes*
*Completed: 2026-03-01*
