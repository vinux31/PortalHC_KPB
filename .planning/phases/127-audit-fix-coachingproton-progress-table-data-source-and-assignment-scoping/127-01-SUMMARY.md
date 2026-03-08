---
phase: 127-audit-fix-coachingproton-progress-table-data-source-and-assignment-scoping
plan: 01
subsystem: database
tags: [ef-core, migration, fk, auto-create, proton-progress]

requires: []
provides:
  - ProtonTrackAssignmentId FK on ProtonDeliverableProgress
  - AutoCreateProgressForAssignment helper method
  - CleanupProgressForAssignment helper method
  - Migration that wipes and recreates progress data from active assignments
affects: [127-02, 127-03, coaching-proton, dashboard-scoping]

tech-stack:
  added: []
  patterns: [auto-create progress on track assignment, cleanup cascade on reassignment]

key-files:
  created:
    - Migrations/20260308091500_LinkProgressToAssignment.cs
  modified:
    - Models/ProtonModels.cs
    - Data/ApplicationDbContext.cs
    - Controllers/AdminController.cs
    - Migrations/ApplicationDbContextModelSnapshot.cs

key-decisions:
  - "ProtonTrackAssignmentId is required (not nullable) with CASCADE delete"
  - "Migration wipes all existing progress, sessions, and history then repopulates from active assignments"
  - "Unique constraint changed from (CoacheeId, DeliverableId) to (AssignmentId, DeliverableId)"

patterns-established:
  - "Auto-create progress: after track assignment creation, create Pending progress for all deliverables in track"
  - "Cleanup on reassign: delete history, sessions, then progress before creating new assignment"

requirements-completed: []

duration: 3min
completed: 2026-03-08
---

# Phase 127 Plan 01: Link Progress to Assignment Summary

**ProtonTrackAssignmentId FK added to ProtonDeliverableProgress with auto-create on assign and cleanup on reassign**

## Performance

- **Duration:** 3 min
- **Started:** 2026-03-08T09:13:09Z
- **Completed:** 2026-03-08T09:15:42Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments
- Added ProtonTrackAssignmentId FK to ProtonDeliverableProgress with CASCADE delete
- Created migration that wipes test data and repopulates from active assignments via INSERT...SELECT
- Added AutoCreateProgressForAssignment and CleanupProgressForAssignment helper methods
- Wired auto-create into CoachCoacheeMappingAssign (after track assignment creation)
- Wired cleanup + auto-create into CoachCoacheeMappingEdit (on track change)

## Task Commits

Each task was committed atomically:

1. **Task 1: Model, DbContext, and Migration** - `0823da4` (feat)
2. **Task 2: Auto-create helper and wire into Assign/Edit** - `81ae15d` (feat)

## Files Created/Modified
- `Models/ProtonModels.cs` - Added ProtonTrackAssignmentId and nav property to ProtonDeliverableProgress
- `Data/ApplicationDbContext.cs` - Updated FK config, new unique index, CASCADE delete
- `Migrations/20260308091500_LinkProgressToAssignment.cs` - Wipe + repopulate migration
- `Migrations/ApplicationDbContextModelSnapshot.cs` - Reflect new column, indexes, FK
- `Controllers/AdminController.cs` - AutoCreate/Cleanup helpers, wired into assign and edit

## Decisions Made
- ProtonTrackAssignmentId is required (not nullable) -- every progress must belong to an assignment
- CASCADE delete on assignment removes all associated progress automatically
- Unique constraint changed from (CoacheeId, DeliverableId) to (AssignmentId, DeliverableId)
- Migration repopulates by joining ProtonTrackAssignments with deliverable hierarchy

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed DbSet name for ProtonDeliverables**
- **Found during:** Task 2 (AutoCreateProgressForAssignment helper)
- **Issue:** Plan used `_context.ProtonDeliverables` but actual DbSet name is `ProtonDeliverableList`
- **Fix:** Changed to `_context.ProtonDeliverableList`
- **Files modified:** Controllers/AdminController.cs
- **Committed in:** 81ae15d (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 bug)
**Impact on plan:** Minor naming correction. No scope creep.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- ProtonDeliverableProgress now linked to assignments via FK
- Auto-create/cleanup wired in -- ready for dashboard and page scoping changes in subsequent plans
- Migration must be applied to database before testing

---
*Phase: 127-audit-fix-coachingproton-progress-table-data-source-and-assignment-scoping*
*Completed: 2026-03-08*
