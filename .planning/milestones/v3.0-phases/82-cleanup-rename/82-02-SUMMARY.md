---
phase: 82-cleanup-rename
plan: 02
subsystem: ui
tags: [cleanup, cmp, admin, orphaned-code, dead-links]

# Dependency graph
requires: []
provides:
  - "CMP controller free of orphaned CpdpProgress, CreateTrainingRecord, and ManageQuestions actions"
  - "Admin/Index hub card pointing only to live endpoints"
  - "Admin/CreateAssessment Manage Questions buttons routing to Admin/ManageQuestions"
affects: [84-assessment-flow-qa, 87-dashboard-navigation-qa]

# Tech tracking
tech-stack:
  added: []
  patterns: []

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/Admin/Index.cshtml
    - Views/Admin/CreateAssessment.cshtml
  deleted:
    - Views/CMP/CpdpProgress.cshtml
    - Views/CMP/CreateTrainingRecord.cshtml
    - Views/CMP/ManageQuestions.cshtml
    - Models/Competency/CpdpProgressViewModel.cs

key-decisions:
  - "Removed entire CMP #region Question Management block (ManageQuestions, AddQuestion, DeleteQuestion) since all three redirect to CMP/ManageQuestions which no longer exists — canonical versions live in AdminController"
  - "Kept Models/CreateTrainingRecordViewModel.cs — shared with AdminController/AddTraining"

patterns-established: []

requirements-completed: [CLN-02, CLN-03, CLN-04]

# Metrics
duration: 15min
completed: 2026-03-02
---

# Phase 82 Plan 02: Remove Orphaned CMP Endpoints and Fix Dead Links Summary

**Three dead CMP endpoints (CpdpProgress, CreateTrainingRecord, ManageQuestions) removed with 4 orphaned files deleted and 3 broken references fixed in Admin hub and CreateAssessment view.**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-03-02T06:13:00Z
- **Completed:** 2026-03-02T06:28:20Z
- **Tasks:** 2
- **Files modified:** 3 modified, 4 deleted

## Accomplishments
- Removed CpdpProgress action (GET) and its view/model from CMP controller — /CMP/CpdpProgress now returns 404
- Removed CreateTrainingRecord GET and POST from CMP controller and deleted its view — /CMP/CreateTrainingRecord now returns 404
- Removed ManageQuestions, AddQuestion, DeleteQuestion from CMP controller and deleted ManageQuestions view — /CMP/ManageQuestions now returns 404
- Removed dead "CPDP Progress Tracking" card from Admin/Index hub
- Fixed 2 hardcoded /CMP/ManageQuestions JavaScript href values in Admin/CreateAssessment.cshtml to /Admin/ManageQuestions

## Task Commits

Each task was committed atomically:

1. **Task 1: Remove orphaned CMP controller actions and view/model files** - `1c19239` (fix)
2. **Task 2: Fix dead hub card and broken ManageQuestions links** - `bdf60b5` (fix)

## Files Created/Modified
- `Controllers/CMPController.cs` - Removed CpdpProgress action, CreateTrainingRecord GET+POST, and ManageQuestions/AddQuestion/DeleteQuestion #region
- `Views/Admin/Index.cshtml` - Removed CPDP Progress Tracking card (linked to deleted CMP action)
- `Views/Admin/CreateAssessment.cshtml` - Updated 2 JS href assignments from /CMP/ManageQuestions to /Admin/ManageQuestions
- `Views/CMP/CpdpProgress.cshtml` - DELETED
- `Views/CMP/CreateTrainingRecord.cshtml` - DELETED
- `Views/CMP/ManageQuestions.cshtml` - DELETED
- `Models/Competency/CpdpProgressViewModel.cs` - DELETED

## Decisions Made
- Removed entire CMP `#region Question Management` block (ManageQuestions + AddQuestion + DeleteQuestion), not just ManageQuestions alone, because AddQuestion and DeleteQuestion redirect back to CMP/ManageQuestions which no longer exists — the canonical versions of all three actions live in AdminController
- Kept `Models/CreateTrainingRecordViewModel.cs` untouched — it is shared with AdminController/AddTraining (lines 3859, 3866)

## Deviations from Plan

None - plan executed exactly as written, with one clarification: the plan said to remove the ManageQuestions method, and the related AddQuestion/DeleteQuestion methods (which redirect to ManageQuestions) were also removed as part of the same `#region` block since their view (Views/CMP/ManageQuestions.cshtml) was being deleted. The canonical implementations remain intact in AdminController.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- All dead CMP endpoints eliminated; /CMP/CpdpProgress, /CMP/CreateTrainingRecord, /CMP/ManageQuestions all return 404
- Admin hub (Admin/Index) shows no dead cards
- Admin/CreateAssessment Manage Questions buttons correctly route to /Admin/ManageQuestions
- Phase 82 Plan 03 (rename Proton Progress to Coaching Proton) can proceed independently

---
*Phase: 82-cleanup-rename*
*Completed: 2026-03-02*

## Self-Check: PASSED

- Controllers/CMPController.cs: FOUND
- Views/Admin/Index.cshtml: FOUND
- Views/Admin/CreateAssessment.cshtml: FOUND
- Views/CMP/CpdpProgress.cshtml: CONFIRMED DELETED
- Views/CMP/CreateTrainingRecord.cshtml: CONFIRMED DELETED
- Views/CMP/ManageQuestions.cshtml: CONFIRMED DELETED
- Models/Competency/CpdpProgressViewModel.cs: CONFIRMED DELETED
- Models/CreateTrainingRecordViewModel.cs: CONFIRMED KEPT
- Commit 1c19239: FOUND
- Commit bdf60b5: FOUND
