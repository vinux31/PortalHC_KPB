---
phase: 26-data-integrity-safeguards
plan: 01
subsystem: ui
tags: [asp.net, razor, cascade-delete, audit-log, UserPackageAssignment]

# Dependency graph
requires:
  - phase: 23-package-answer-integrity
    provides: PackageUserResponse table and UserPackageAssignment FK relationships
  - phase: 24-hc-audit-log
    provides: AuditLogService for LogAsync calls
provides:
  - DeletePackage cascade cleanup (UserPackageAssignment + PackageUserResponse)
  - Assignment-count-aware confirm dialog on ManagePackages delete button
  - Audit log entry for DeletePackage
affects: [future package management work, any phase touching AssessmentPackage deletion]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - GroupBy + ToDictionaryAsync for per-package counts passed via ViewBag
    - @{} block inside @foreach to pre-compute Razor confirm messages with interpolation
    - Cascade delete: responses -> assignments -> options -> questions -> package before SaveChangesAsync
    - Audit log wrapped in try/catch after SaveChangesAsync to avoid rollback on audit failure

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/ManagePackages.cshtml

key-decisions:
  - "Pre-compute confirm message in @{} block inside foreach — avoids @ interpolation collision in onsubmit attribute"
  - "Cascade order: PackageUserResponses (via question IDs) then UserPackageAssignments before options/questions/package to satisfy FK constraints"
  - "Audit log placed AFTER SaveChangesAsync and wrapped in try/catch — audit failure must not roll back a successful delete"
  - "Assignment count check at page load (ManagePackages GET) via GroupBy — no extra round-trips per delete click"

patterns-established:
  - "Assignment-aware confirm: ViewBag.AssignmentCounts Dictionary<int,int> populated in GET, consumed in view per-item"

# Metrics
duration: 3min
completed: 2026-02-21
---

# Phase 26 Plan 01: Data Integrity Safeguards — Delete Package Warning Summary

**DeletePackage enhanced with cascade cleanup of UserPackageAssignment + PackageUserResponse records and a data-loss confirm dialog showing affected assignment count.**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-02-21T04:31:26Z
- **Completed:** 2026-02-21T04:34:13Z
- **Tasks:** 1
- **Files modified:** 2

## Accomplishments

- ManagePackages GET now queries UserPackageAssignment counts per package via GroupBy and passes them as `ViewBag.AssignmentCounts`
- DeletePackage POST cascades to delete PackageUserResponses (via question IDs) and UserPackageAssignments before removing the package itself — no more FK constraint errors
- ManagePackages view shows a strong PERINGATAN warning with assignment count when deleting a package with active assignments; simpler confirm for zero-assignment packages
- DeletePackage is audit-logged with assignment count in the message, wrapped in try/catch to protect the delete result

## Task Commits

1. **Task 1: Enhance DeletePackage with assignment count warning and cascade cleanup** - `5061260` (feat)

   Note: CMPController.cs changes were staged alongside the 26-02 changes in commit `51d4323` (both committed in the same session). ManagePackages.cshtml landed in `5061260`.

**Plan metadata:** (docs commit follows)

## Files Created/Modified

- `Controllers/CMPController.cs` - ManagePackages GET adds assignment count query; DeletePackage POST adds cascade cleanup and audit log
- `Views/CMP/ManagePackages.cshtml` - Adds `assignmentCounts` ViewBag binding; delete form uses assignment-aware confirm message

## Decisions Made

- Pre-compute confirm message in `@{}` block inside `@foreach` — direct `$""` interpolation in `onsubmit="return confirm('...')"` would conflict with Razor's `@` syntax
- Cascade order is: PackageUserResponses (via questionIds) → UserPackageAssignments → Options → Questions → Package to satisfy FK constraints in the correct sequence
- Audit log placed after `SaveChangesAsync` in try/catch so audit failure cannot roll back a successful delete (matching established Phase 24-01 pattern)

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- DATA-01 satisfied: HC sees assignment count warning before destructive package delete
- No FK constraint errors on delete (cascade handles UserPackageAssignment and PackageUserResponse)
- Phase 26-02 (schedule-change warning) is the next plan in this phase

## Self-Check: PASSED

- FOUND: Controllers/CMPController.cs
- FOUND: Views/CMP/ManagePackages.cshtml
- FOUND: .planning/phases/26-data-integrity-safeguards/26-01-SUMMARY.md
- FOUND commit: 5061260 (feat: view changes)
- FOUND commit: 51d4323 (controller changes committed alongside 26-02)
- Build: 0 errors

---
*Phase: 26-data-integrity-safeguards*
*Completed: 2026-02-21*
