---
phase: 128-unit-filtered-progress-clean-migration
plan: 01
subsystem: database
tags: [ef-core, migration, unit-filter, coaching-proton, progress]

requires:
  - phase: 123-assignment-unit-scoping
    provides: AssignmentUnit field on CoachCoacheeMapping
provides:
  - Unit-filtered AutoCreateProgressForAssignment method
  - Clean migration wiping and recreating all progress data with unit scope
affects: [129-secondary-unit-paths, coaching-proton]

tech-stack:
  added: []
  patterns: [unit-resolution-fallback, sql-data-migration]

key-files:
  created:
    - Migrations/20260308101158_CleanAndRecreateProgress.cs
  modified:
    - Controllers/AdminController.cs

key-decisions:
  - "Used DisplayName (not Name) for ProtonTrack — model uses DisplayName property"
  - "Table names in SQL match EF schema: ProtonDeliverableList, ProtonKompetensiList, ProtonSubKompetensiList (not pluralized)"

patterns-established:
  - "Unit resolution: AssignmentUnit from active mapping, fallback to User.Unit, skip if both null"
  - "SQL data migration pattern: DELETE in FK order, then INSERT with unit filter join"

requirements-completed: [PROG-01, MIG-01, MIG-02]

duration: 4min
completed: 2026-03-08
---

# Phase 128 Plan 01: Unit-Filtered Progress & Clean Migration Summary

**Unit-filtered AutoCreateProgressForAssignment with data migration wiping and recreating all progress scoped by AssignmentUnit**

## Performance

- **Duration:** 4 min
- **Started:** 2026-03-08T10:09:32Z
- **Completed:** 2026-03-08T10:13:15Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- AutoCreateProgressForAssignment now resolves unit (AssignmentUnit -> User.Unit fallback) and filters deliverables by matching ProtonKompetensi.Unit
- Returns warnings list to callers when unit missing or no deliverables found for unit
- Clean migration deletes all DeliverableStatusHistories, linked CoachingSessions, and ProtonDeliverableProgresses then recreates with unit filter
- Both callers (CoachCoacheeMappingAssign and CoachCoacheeMappingEdit) surface warnings via TempData

## Task Commits

Each task was committed atomically:

1. **Task 1: Add unit filter to AutoCreateProgressForAssignment** - `f0a94c8` (feat)
2. **Task 2: Create clean migration to wipe and recreate progress** - `929efda` (feat)

## Files Created/Modified
- `Controllers/AdminController.cs` - Unit-filtered AutoCreateProgressForAssignment with warnings, updated callers
- `Migrations/20260308101158_CleanAndRecreateProgress.cs` - SQL data migration
- `Migrations/20260308101158_CleanAndRecreateProgress.Designer.cs` - EF snapshot

## Decisions Made
- Used DisplayName property for ProtonTrack (model has no Name property)
- Corrected SQL table names to match actual EF schema (ProtonDeliverableList singular, ProtonKompetensiList, ProtonSubKompetensiList)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed ProtonTrack.Name to ProtonTrack.DisplayName**
- **Found during:** Task 1 (AutoCreateProgressForAssignment)
- **Issue:** Plan referenced `t.Name` but ProtonTrack model uses `DisplayName`
- **Fix:** Changed to `t.DisplayName`
- **Files modified:** Controllers/AdminController.cs
- **Verification:** Build succeeds
- **Committed in:** f0a94c8

**2. [Rule 1 - Bug] Fixed SQL table names in migration**
- **Found during:** Task 2 (migration)
- **Issue:** Plan used AspNetUsers (actual: Users), ProtonDeliverableLists (actual: ProtonDeliverableList), ProtonSubKompetensis (actual: ProtonSubKompetensiList), ProtonKompetensis (actual: ProtonKompetensiList)
- **Fix:** Corrected all table names to match EF schema from Designer file
- **Files modified:** Migrations/20260308101158_CleanAndRecreateProgress.cs
- **Verification:** Migration applies successfully
- **Committed in:** 929efda

---

**Total deviations:** 2 auto-fixed (2 bugs — incorrect property/table names)
**Impact on plan:** Both fixes necessary for correctness. No scope creep.

## Issues Encountered
None beyond the table/property name mismatches documented above.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Phase 129 (secondary unit paths) can proceed — AutoCreateProgressForAssignment with unit filter is in place
- All existing progress data has been recreated with correct unit scope

---
*Phase: 128-unit-filtered-progress-clean-migration*
*Completed: 2026-03-08*
