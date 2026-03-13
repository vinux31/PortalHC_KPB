---
phase: 170-security-review
plan: 01
subsystem: auth
tags: [csrf, authorization, antiforgery, security]

requires: []
provides:
  - CSRF protection on all NotificationController POST endpoints
  - Full authorization audit across all 7 controllers
affects: [any future controller adding POST endpoints]

tech-stack:
  added: []
  patterns:
    - "AJAX POST pattern: read __RequestVerificationToken from DOM, send as RequestVerificationToken header"

key-files:
  created: []
  modified:
    - Controllers/NotificationController.cs
    - Views/Shared/Components/NotificationBell/Default.cshtml

key-decisions:
  - "NotificationController CSRF gap closed — [IgnoreAntiforgeryToken] removed, all 3 POST actions now have [ValidateAntiForgeryToken], JS updated to pass token header"
  - "All 7 controllers confirmed: every POST action has [ValidateAntiForgeryToken]; every admin action has [Authorize(Roles='Admin, HC')]"

patterns-established:
  - "AJAX CSRF pattern: getAntiforgeryToken() reads input[name='__RequestVerificationToken'] injected by _Layout.cshtml logout form; pass as RequestVerificationToken header in fetch"

requirements-completed: [SEC-01, SEC-02]

duration: 8min
completed: 2026-03-13
---

# Phase 170 Plan 01: Security Review — Authorization & CSRF Audit Summary

**CSRF gap in NotificationController closed: [IgnoreAntiforgeryToken] removed, all 3 POST endpoints now protected, JS updated to send RequestVerificationToken header**

## Performance

- **Duration:** 8 min
- **Started:** 2026-03-13T07:30:00Z
- **Completed:** 2026-03-13T07:38:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Audited all 7 controllers — 6 of 7 were already correctly protected; 1 gap found and fixed
- Removed class-level `[IgnoreAntiforgeryToken]` from NotificationController
- Added `[ValidateAntiForgeryToken]` to MarkAsRead, MarkAllAsRead, and Dismiss POST actions
- Updated notification bell JavaScript to send antiforgery token via `RequestVerificationToken` header, following the existing pattern from StartExam.cshtml

## Task Commits

1. **Task 1: Audit authorization attributes** — No code changes (audit only; all controllers passed)
2. **Task 2: Fix CSRF in NotificationController** — `4d6e3c8` (fix)

**Plan metadata:** (pending docs commit)

## Files Created/Modified

- `Controllers/NotificationController.cs` — Removed `[IgnoreAntiforgeryToken]`, added `[ValidateAntiForgeryToken]` to 3 POST actions
- `Views/Shared/Components/NotificationBell/Default.cshtml` — Added `getAntiforgeryToken()` and `postWithToken()` helpers; updated all 3 fetch calls

## Decisions Made

- Chose option (a) from plan: add proper CSRF tokens rather than keep [IgnoreAntiforgeryToken]. Consistency matters and the fix is simple — the `_Layout.cshtml` logout form already injects `__RequestVerificationToken` on every page, so no additional token injection is needed.
- Task 1 confirmed all AdminController POST actions have `[Authorize(Roles = "Admin, HC")]` — no action relies solely on class-level `[Authorize]` for admin endpoints.
- CDPController approval actions correctly use `[Authorize(Roles = "Sr Supervisor, Section Head, HC, Admin")]` — intentional role scoping.

## Deviations from Plan

None — plan executed exactly as written. The CSRF fix in Task 2 was the planned work.

## Issues Encountered

None. Build succeeded with 0 errors after the fix.

## User Setup Required

None — no external service configuration required.

## Audit Summary: All 7 Controllers

| Controller | Class [Authorize] | POST has [ValidateAntiForgeryToken] | Role Scoping |
|---|---|---|---|
| AccountController | [Authorize] | All 4 POST actions | Login/AccessDenied are [AllowAnonymous] |
| AdminController | [Authorize] | All POST actions | Every action has [Authorize(Roles = "Admin, HC")] |
| CDPController | [Authorize] | All 8 POST actions | Approval actions have Sr Supervisor/Section Head/HC/Admin |
| CMPController | [Authorize] | All 9 POST actions | No per-action roles — correct (all auth users) |
| HomeController | [Authorize] | No POST actions | N/A |
| NotificationController | [Authorize] | **Fixed** — all 3 POST actions | User-scoped via claims (correct) |
| ProtonDataController | [Authorize(Roles = "Admin,HC")] | All 9 POST actions | Class-level covers all |

## Next Phase Readiness

- All controllers fully protected (auth + CSRF)
- Phase 170 Plan 02 can proceed to next security concerns if any

---
*Phase: 170-security-review*
*Completed: 2026-03-13*
