---
phase: 127-audit-fix-coachingproton-progress-table-data-source-and-assignment-scoping
plan: 02
subsystem: controller
tags: [scoping, assignment-based, coaching-proton, dashboard]

requires:
  - ProtonTrackAssignmentId FK on ProtonDeliverableProgress (from 127-01)
provides:
  - Assignment-based coachee scoping in BuildProtonProgressSubModelAsync
  - Assignment-based scoping in CoachingProton, HistoriProton
  - Belt-and-suspenders track validation on progress queries
affects: [dashboard, coaching-proton-page, histori-proton]

tech-stack:
  added: []
  patterns: [assignment-based scoping replaces RoleLevel==6 user queries]

key-files:
  created: []
  modified:
    - Controllers/CDPController.cs

key-decisions:
  - "All coachee visibility now flows through active ProtonTrackAssignment, not RoleLevel==6 user queries"
  - "Belt-and-suspenders: progress query validates deliverable track matches assignment track"
  - "HistoriProton shows coachees with any assignment (active or inactive) for history completeness"
  - "ExportCoachingProton action does not exist — no changes needed"

patterns-established:
  - "Assignment-based scoping pattern: query ProtonTrackAssignments then extract coacheeIds"

requirements-completed: []

duration: 2min
completed: 2026-03-08
---

# Phase 127 Plan 02: Rewrite Dashboard and CoachingProton Scoping Summary

**All CDP coachee visibility rewritten from RoleLevel==6 user queries to active ProtonTrackAssignment-based scoping with belt-and-suspenders track validation**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-08T09:17:43Z
- **Completed:** 2026-03-08T09:19:36Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments
- Rewrote BuildProtonProgressSubModelAsync to find coachees via active ProtonTrackAssignment per role
- Rewrote CoachingProton STEP 1 from user-based to assignment-based scoping for all 4 role levels
- Changed progress query from CoacheeId-based to ProtonTrackAssignmentId-based with Include chain
- Added belt-and-suspenders WHERE clause validating deliverable track matches assignment track
- Updated HistoriProton to scope coachees via assignments (all assignments for history, not just active)
- Level 6 coachee now only sees data if they have an active assignment

## Task Commits

Each task was committed atomically:

1. **Task 1: Rewrite BuildProtonProgressSubModelAsync** - `0bfae2d` (feat)
2. **Task 2: Rewrite CoachingProton, HistoriProton scoping** - `3aaa023` (feat)

## Files Created/Modified
- `Controllers/CDPController.cs` - Three methods rewritten: BuildProtonProgressSubModelAsync, CoachingProton STEP 1, HistoriProton scoping

## Decisions Made
- All coachee visibility flows through ProtonTrackAssignment, not raw user queries
- Belt-and-suspenders track validation ensures progress records match their assignment's track
- HistoriProton intentionally queries all assignments (not just active) since it shows history
- ExportCoachingProton does not exist in the codebase -- no changes needed

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] ExportCoachingProton action does not exist**
- **Found during:** Task 2
- **Issue:** Plan referenced ExportCoachingProton action but it does not exist in CDPController
- **Fix:** Skipped -- no action needed
- **Files modified:** None

**2. [Rule 2 - Missing] Level 6 coachee assignment check**
- **Found during:** Task 2
- **Issue:** Plan said "unchanged" for Level 6 but assignment-based scoping means coachee should only see data if they have active assignment
- **Fix:** Added AnyAsync check for active assignment before including user.Id
- **Files modified:** Controllers/CDPController.cs

---

**Total deviations:** 2 (1 skip, 1 enhancement)
**Impact on plan:** Minor. No scope creep.

## Issues Encountered
None

## User Setup Required
None

## Next Phase Readiness
- All CDP pages now scope through ProtonTrackAssignment
- Ready for Plan 03 (auto-create/sync triggers)

---
*Phase: 127-audit-fix-coachingproton-progress-table-data-source-and-assignment-scoping*
*Completed: 2026-03-08*
