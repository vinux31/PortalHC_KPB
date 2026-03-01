---
phase: 74-hybrid-auth-role-restructuring
plan: "02"
subsystem: auth
tags: [aspnet-identity, role-based-access, ef-migration, razor-views]

# Dependency graph
requires:
  - phase: 74-01
    provides: UserRoles.Supervisor constant, SectionHead level=3 in code, HybridAuthService

provides:
  - _Layout.cshtml reads SelectedView field directly (no async GetRolesAsync)
  - CDPController upload gated on Coach role name (not RoleLevel comparison)
  - AdminController EligibleCoaches uses GetUsersInRoleAsync("Coach")
  - EF migration applied: SectionHead users have RoleLevel=3 in database

affects: [CDPController, AdminController, _Layout, CoachCoacheeMappings, EligibleCoaches, UploadEvidence]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Role checks via UserManager.GetUsersInRoleAsync() instead of RoleLevel comparisons for business logic"
    - "SelectedView field used in _Layout for fast role display without async DB round-trip"
    - "EF data migration via migrationBuilder.Sql for RoleLevel updates without schema changes"

key-files:
  created:
    - Migrations/20260228142913_UpdateSectionHeadRoleLevelAndAddSupervisorRole.cs
  modified:
    - Views/Shared/_Layout.cshtml
    - Controllers/CDPController.cs
    - Controllers/AdminController.cs
    - Migrations/ApplicationDbContextModelSnapshot.cs

key-decisions:
  - "canUpload in Deliverable GET uses userRole == UserRoles.Coach (role name), not RoleLevel comparison — Supervisor (level 5) must not upload evidence"
  - "UploadEvidence POST fetches roles via GetRolesAsync then checks string equality to UserRoles.Coach — clean gate regardless of RoleLevel value"
  - "EligibleCoaches uses GetUsersInRoleAsync(UserRoles.Coach) — explicit role membership query, not level filter"
  - "_Layout uses currentUser?.SelectedView to avoid async GetRolesAsync in view layer — consistent with Phase 73 SelectedView pattern"
  - "Data migration SQL uses JOIN to AspNetUserRoles+AspNetRoles to identify SectionHead users — idempotent (sets RoleLevel=3 regardless of current value)"

patterns-established:
  - "Role-based gates: prefer UserManager.GetUsersInRoleAsync / GetRolesAsync string checks over RoleLevel integer comparisons for access control"
  - "View role display: use SelectedView field (already stored, no async needed) rather than GetRolesAsync in shared layout"

requirements-completed: [AUTH-HYBRID]

# Metrics
duration: 3min
completed: 2026-02-28
---

# Phase 74 Plan 02: Hybrid Auth Role Restructuring — Role Fix Application Summary

**_Layout SelectedView optimization, CDPController Coach-only upload enforcement, AdminController GetUsersInRoleAsync for EligibleCoaches, and EF data migration setting SectionHead RoleLevel=3 in database**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-02-28T14:27:57Z
- **Completed:** 2026-02-28T14:30:28Z
- **Tasks:** 2
- **Files modified:** 4 (+ 1 migration created)

## Accomplishments
- _Layout.cshtml now reads `currentUser?.SelectedView` directly — eliminates async GetRolesAsync call from shared view, faster and deterministic
- CDPController evidence upload now gated on Coach role name (not `RoleLevel <= 5`) — SrSupervisor cannot upload evidence
- AdminController CoachCoacheeMappings dropdown now populated from `GetUsersInRoleAsync("Coach")` — Supervisor users excluded from coach selection
- EF migration `UpdateSectionHeadRoleLevelAndAddSupervisorRole` applied: existing SectionHead users in DB updated to RoleLevel=3

## Task Commits

Each task was committed atomically:

1. **Task 1: Fix _Layout.cshtml role display and CDPController Coach-only checks** - `2053942` (feat)
2. **Task 2: Fix AdminController EligibleCoaches and run EF migration** - `d64a116` (feat)

**Plan metadata:** (docs commit — see final commit)

## Files Created/Modified
- `Views/Shared/_Layout.cshtml` - Line 7: `currentUser?.SelectedView` replaces async GetRolesAsync call
- `Controllers/CDPController.cs` - Deliverable GET `canUpload` uses `userRole == UserRoles.Coach`; UploadEvidence POST uses role string check
- `Controllers/AdminController.cs` - CoachCoacheeMappings GET: EligibleCoaches from `GetUsersInRoleAsync(UserRoles.Coach)`
- `Migrations/20260228142913_UpdateSectionHeadRoleLevelAndAddSupervisorRole.cs` - Data migration: SectionHead RoleLevel 4→3
- `Migrations/ApplicationDbContextModelSnapshot.cs` - Auto-updated by EF tooling

## Decisions Made
- `canUpload` and UploadEvidence POST both check role name "Coach" — this deliberately excludes Supervisor (also level 5) who should not upload evidence under the coaching model
- `EligibleCoaches` via `GetUsersInRoleAsync` is cleaner than filtering `allUsers` by level — explicitly represents business rule "only Coach-role users can be assigned as coaches"
- `_Layout` uses SelectedView (stored field) rather than GetRolesAsync — consistent with Phase 73 pattern where GetDefaultView populates SelectedView at user creation/edit

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required. Migration applied automatically via `dotnet ef database update`.

## Next Phase Readiness
- Phase 74 fully complete (2/2 plans done)
- All role-based fixes applied: display, upload gate, coach selection, and DB alignment
- SectionHead users in DB now have RoleLevel=3, matching the code constant
- No blockers for subsequent phases

## Self-Check

**Files exist:**
- [x] Views/Shared/_Layout.cshtml — FOUND
- [x] Controllers/CDPController.cs — FOUND
- [x] Controllers/AdminController.cs — FOUND
- [x] Migrations/20260228142913_UpdateSectionHeadRoleLevelAndAddSupervisorRole.cs — FOUND

**Commits exist:**
- [x] 2053942 — FOUND
- [x] d64a116 — FOUND

## Self-Check: PASSED

---
*Phase: 74-hybrid-auth-role-restructuring*
*Completed: 2026-02-28*
