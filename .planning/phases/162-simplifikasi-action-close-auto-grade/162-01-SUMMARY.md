---
phase: 162-simplifikasi-action-close-auto-grade
plan: "01"
subsystem: api
tags: [assessment, grading, auto-grade, cancelled-status]

requires:
  - phase: none
    provides: existing ForceClose/CloseEarly actions to replace
provides:
  - AkhiriUjian action (individual auto-grade from saved answers)
  - AkhiriSemuaUjian action (bulk auto-grade + cancel not-started)
  - GradeFromSavedAnswers shared method
  - GetAkhiriSemuaCounts endpoint for confirmation modal
  - Cancelled status support in monitoring, export, CheckExamStatus
affects: [162-02 (UI buttons/modals), assessment-monitoring, exam-polling]

tech-stack:
  added: []
  patterns: [shared-grading-method, cancelled-status-handling]

key-files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Controllers/CMPController.cs
    - Models/AssessmentMonitoringViewModel.cs
    - Models/AuditLog.cs

key-decisions:
  - "GradeFromSavedAnswers duplicated NotifyIfGroupCompleted in AdminController (CMPController's is private) with Cancelled-aware group completion check"
  - "Cancelled sessions redirect to Assessment page (not Results) since they have no score"

patterns-established:
  - "GradeFromSavedAnswers: reusable per-session grading with TrainingRecord + notification"

requirements-completed: [CLOSE-01, CLOSE-02, CLOSE-03, CLOSE-04]

duration: 8min
completed: 2026-03-13
---

# Phase 162 Plan 01: Backend Action Close + Auto-Grade Summary

**Replaced 3 inconsistent close actions with AkhiriUjian/AkhiriSemuaUjian auto-grading via shared GradeFromSavedAnswers method, plus Cancelled status for not-started workers**

## Performance

- **Duration:** 8 min
- **Started:** 2026-03-13T01:13:50Z
- **Completed:** 2026-03-13T01:22:00Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- Extracted GradeFromSavedAnswers shared method handling both package and legacy grading paths with TrainingRecord creation and group completion notification
- AkhiriUjian (individual) auto-grades InProgress session from saved answers with real score
- AkhiriSemuaUjian (bulk) auto-grades all InProgress + sets Open/not-started to Cancelled
- Removed ForceCloseAssessment, ForceCloseAll, CloseEarly entirely
- Cancelled status handled in CheckExamStatus, monitoring detail, Excel export, and Reset validation
- GetAkhiriSemuaCounts endpoint provides impact counts for confirmation modal

## Task Commits

Each task was committed atomically:

1. **Task 1: Extract shared grading method and create AkhiriUjian + AkhiriSemuaUjian actions** - `321a6e2` (feat)
2. **Task 2: Update CheckExamStatus for new statuses and add GetAkhiriSemuaCounts endpoint** - `451b7df` (feat)

## Files Created/Modified
- `Controllers/AdminController.cs` - AkhiriUjian, AkhiriSemuaUjian, GradeFromSavedAnswers, GetAkhiriSemuaCounts; removed ForceClose*/CloseEarly; Cancelled in monitoring+export
- `Controllers/CMPController.cs` - CheckExamStatus handles Cancelled status with redirect to Assessment page
- `Models/AssessmentMonitoringViewModel.cs` - Added CancelledCount and InProgressCount properties
- `Models/AuditLog.cs` - Updated action type comment to list AkhiriUjian/AkhiriSemuaUjian

## Decisions Made
- Duplicated NotifyIfGroupCompleted in AdminController rather than making CMPController's version public/shared, keeping controllers self-contained. AdminController version treats Cancelled as "done" for group completion check.
- GetAkhiriSemuaCounts was included in Task 1 commit alongside the main actions for atomicity.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Backend actions ready; Plan 02 (UI buttons/modals in AssessmentMonitoringDetail.cshtml) can proceed
- View still references old ForceClose/CloseEarly form actions — must be updated in Plan 02

---
*Phase: 162-simplifikasi-action-close-auto-grade*
*Completed: 2026-03-13*
