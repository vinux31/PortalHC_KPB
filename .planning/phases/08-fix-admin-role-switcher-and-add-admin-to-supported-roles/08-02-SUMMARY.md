---
phase: 08-fix-admin-role-switcher-and-add-admin-to-supported-roles
plan: 02
subsystem: auth
tags: [role-switcher, admin, authorization, cdp, csharp, aspnet]

# Dependency graph
requires:
  - phase: 08-fix-admin-role-switcher-and-add-admin-to-supported-roles
    provides: Plan 01 built AccountController allowedViews, _Layout dropdown Admin View option, and SeedData default SelectedView="Admin"
provides:
  - CDPController HC-gated actions (HCApprovals, HCReviewDeliverable, CreateFinalAssessment GET+POST) accept Admin+SelectedView=="HC"
  - CDPController Deliverable GET isHC/canHCReview flags are Admin-aware
  - CDPController ApproveDeliverable/RejectDeliverable accept Admin in Atasan/HC view and skip section check for Admin
  - CDPController ProtonMain and Coaching coachee list queries fall back to all coachees when Admin has null Section
  - CDPController CreateSession POST blocks Admin in Coachee view via isCoacheeView flag
  - Complete end-to-end Admin role-switcher verified by human: all 6 steps passed
affects: [future-phase-cdp-extensions, any-plan-modifying-cdpcontroller-authorization]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "isHCAccess bool: userRole == HC || (Admin && SelectedView == 'HC') — reusable pattern for any HC-gated action"
    - "isAtasanAccess bool: userRole == SrSupervisor || SectionHead || (Admin && SelectedView in ['Atasan','HC'])"
    - "null-safe coachee query: string.IsNullOrEmpty(user.Section) || u.Section == user.Section — Admin with null Section gets all coachees"
    - "isCoacheeView flag for POST gates: Admin+SelectedView=='Coachee' treated same as Coachee role"

key-files:
  created: []
  modified:
    - Controllers/CDPController.cs

key-decisions:
  - "isHCAccess pattern used instead of inline ternary — variable name makes the condition self-documenting and reusable per-action"
  - "Admin section check skipped in ApproveDeliverable/RejectDeliverable — Admin has null Section by design; cross-section approval is intended"
  - "CreateSession uses fresh GetRolesAsync call with local variable userRoleForCreate — userRole not in scope at POST action entry point"
  - "isCoacheeView flag replaces RoleLevel > 5 — original check was not Admin-aware; new check uses role name + SelectedView"

patterns-established:
  - "HC gate pattern: bool isHCAccess = role==HC || (role==Admin && view=='HC'); if (!isHCAccess) Forbid()"
  - "Atasan gate pattern: bool isAtasanAccess = role==SrSupervisor || SectionHead || (role==Admin && view in Atasan/HC)"
  - "Null-section coachee query: string.IsNullOrEmpty(user.Section) || u.Section == user.Section"
  - "Admin section bypass: if (userRole != Admin && (coacheeUser==null || section mismatch)) Forbid()"

# Metrics
duration: ~35min (including human verification)
completed: 2026-02-18
---

# Phase 8 Plan 02: Fix CDPController Admin Role-Switcher Gates Summary

**10 targeted fixes to CDPController.cs making every HC, Atasan, Coach, and Coachee gate Admin-aware, completing the end-to-end Admin role-switcher feature**

## Performance

- **Duration:** ~35 min (Tasks 1+2 automated, Task 3 human verification)
- **Started:** 2026-02-18T07:49:31Z
- **Completed:** 2026-02-18T10:50:36Z (human approval received)
- **Tasks:** 3 (2 auto + 1 human-verify checkpoint)
- **Files modified:** 1 (Controllers/CDPController.cs)

## Accomplishments

- All HC-gated actions (HCApprovals, HCReviewDeliverable, CreateFinalAssessment GET+POST) now accept Admin when SelectedView is "HC"
- Deliverable page isHC and canHCReview flags are Admin-aware; Approve/Reject gates accept Admin in Atasan and HC views with section check bypassed
- Null-section coachee queries in ProtonMain and Coaching fall back to all coachees for Admin; CreateSession POST blocks Admin in Coachee view with isCoacheeView flag
- End-to-end human verification completed: all 6 role-switcher scenarios approved (Admin View, HC View, Atasan View, Coach View, Admin View checkmark, Coachee View gate)

## Task Commits

Each task was committed atomically:

1. **Task 1: Fix HC-gated actions and Deliverable isHC/canHCReview** - `fc3b68c` (feat)
2. **Task 2: Fix Approve/Reject gates, null-Section coachee lists, and CreateSession Coachee gate** - `ad49f5f` (feat)
3. **Task 3: Human verification checkpoint** - approved by user (no code commit)

## Files Created/Modified

- `Controllers/CDPController.cs` - 10 fixes applied: isHCAccess in 4 actions, isHC/canHCReview in Deliverable GET, isAtasanAccess + section bypass in ApproveDeliverable/RejectDeliverable, null-safe coachee queries in ProtonMain and Coaching, isCoacheeView in CreateSession POST

## Decisions Made

- **isHCAccess pattern over inline ternary:** A named bool variable makes each gate self-documenting — future readers see the intent immediately without parsing the condition inline.
- **Admin section bypass in Approve/Reject:** Admin.Section is null in the seed and DB; requiring section match would permanently forbid Admin from approving any deliverable. Bypass is intentional.
- **Fresh GetRolesAsync in CreateSession POST:** The original `userRole` variable is declared in the GET action, not shared with the POST. Declared `userRoleForCreate` as a local variable rather than refactoring shared scope — minimal change, no behavior risk.
- **isCoacheeView replaces RoleLevel > 5:** The original check `RoleLevel > 5` forbids anyone with RoleLevel 6 (Coachee) but does not account for Admin (any RoleLevel) simulating Coachee view. New flag checks role name and SelectedView explicitly.

## Deviations from Plan

None — plan executed exactly as written. All 10 fix patterns were applied as specified in the plan file with matching code snippets.

## Issues Encountered

None — `dotnet build` passed with 0 errors after each task. All plan line-number references were confirmed accurate before applying edits.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- Phase 8 is complete. The Admin role-switcher feature is fully functional end-to-end.
- All CDPController authorization gates are Admin-aware. No known remaining role-gate gaps.
- The HC, Atasan, Coach, and Coachee simulated views all work correctly for Admin.
- No blockers or concerns for future phases.

---
*Phase: 08-fix-admin-role-switcher-and-add-admin-to-supported-roles*
*Completed: 2026-02-18*
