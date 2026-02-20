---
phase: 22-exam-lifecycle-actions
plan: 04
subsystem: ui, api
tags: [csharp, aspnet, ef-core, assessment, monitoring]

# Dependency graph
requires:
  - phase: 22-exam-lifecycle-actions (plan 01)
    provides: ExamWindowCloseDate column on AssessmentSession
  - phase: 22-exam-lifecycle-actions (plan 02)
    provides: AbandonExam POST action + Abandoned status string
  - phase: 21-exam-state-foundation
    provides: StartedAt column, InProgress status, AssessmentMonitoringDetail GET view
provides:
  - ResetAssessment POST action in CMPController (clears session + deletes answers)
  - ForceCloseAssessment POST action in CMPController (force-completes with score 0)
  - Abandoned branch in UserStatus projection (four-state: Completed/Abandoned/InProgress/Not started)
  - Action buttons (Reset + Force Close) in AssessmentMonitoringDetail table per row
  - TempData Success/Error alert banners on AssessmentMonitoringDetail view
affects: [assessment-monitoring, exam-retake, hc-management]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Inline POST forms per table row with CSRF token and browser confirm() dialog"
    - "Four-state UserStatus projection: check Abandoned before InProgress (Abandoned sessions have StartedAt set)"
    - "Reset deletes UserResponses + UserPackageAssignment; ForceClose does not (audit preservation)"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/AssessmentMonitoringDetail.cshtml

key-decisions:
  - "Abandoned branch added before InProgress check in UserStatus projection — Abandoned sessions always have StartedAt set, so without this they would incorrectly show InProgress"
  - "ResetAssessment deletes both UserResponses and UserPackageAssignment so next StartExam assigns a fresh random package"
  - "ForceCloseAssessment preserves UserResponse/UserPackageAssignment records for audit — session is formally completed, not reset"
  - "Success message for ResetAssessment omits User.FullName (navigation property not loaded) — generic message avoids null reference"

patterns-established:
  - "Status guard in POST actions: check status before mutating, redirect with TempData['Error'] on invalid state"
  - "Both actions redirect to AssessmentMonitoringDetail with title/category/scheduleDate params — HC stays on same view"

# Metrics
duration: 4min
completed: 2026-02-20
---

# Phase 22 Plan 04: HC Reset and Force Close Assessment Actions Summary

**ResetAssessment POST (clears session + deletes answers for retake) and ForceCloseAssessment POST (force-completes with Score=0) added to CMPController, with conditional action buttons per row in AssessmentMonitoringDetail view**

## Performance

- **Duration:** 4 min
- **Started:** 2026-02-20T13:51:20Z
- **Completed:** 2026-02-20T13:55:28Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- Added Abandoned as a distinct fourth UserStatus state in the monitoring detail projection (previously Abandoned sessions showed as InProgress)
- ResetAssessment POST: resets session to Open, clears Score/IsPassed/CompletedAt/StartedAt/Progress, deletes UserResponses and UserPackageAssignment — ready for fresh retake
- ForceCloseAssessment POST: marks session Completed with Score=0, IsPassed=false — does not delete answers (audit preservation)
- AssessmentMonitoringDetail view now shows Reset button on Completed/Abandoned rows and Force Close button on InProgress/Not started rows, each with browser confirm() dialog and CSRF token
- TempData Success/Error alert banners added at top of AssessmentMonitoringDetail view

## Task Commits

Each task was committed atomically:

1. **Task 1: Add ResetAssessment and ForceCloseAssessment POST actions to CMPController** - `2838c07` (feat) — note: these were already committed in a prior session as part of plan 22-02 execution
2. **Task 2: Add Reset and Force Close action buttons to AssessmentMonitoringDetail view** - `d34546f` (feat)

**Plan metadata:** committed as part of final docs commit

## Files Created/Modified
- `Controllers/CMPController.cs` - Added Abandoned branch to UserStatus projection; added ResetAssessment and ForceCloseAssessment POST actions
- `Views/CMP/AssessmentMonitoringDetail.cshtml` - TempData alerts, Actions column header, conditional Reset/Force Close buttons per row, colspan updated to 7

## Decisions Made
- Abandoned branch placed before InProgress in UserStatus projection — required because Abandoned sessions have StartedAt set and would otherwise be misclassified as InProgress
- ResetAssessment deletes UserPackageAssignment so the next StartExam call assigns a fresh random question package
- ForceCloseAssessment does not delete answers — session is completed, answers preserved for audit
- Success message for ResetAssessment uses generic text (no User.FullName) since navigation property is not loaded in that action

## Deviations from Plan

None - plan executed exactly as written. Task 1 controller changes were found already committed from a prior session; Task 2 view changes were implemented fresh.

## Issues Encountered
- Task 1 controller changes (ResetAssessment, ForceCloseAssessment, Abandoned projection branch) were already present in the repository from commit `2838c07` (plan 22-02 execution). No re-implementation needed — the code was verified to be correct and complete.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Phase 22 complete: all four plans (22-01 through 22-04) implemented — ExamWindowCloseDate, AbandonExam, timer enforcement, and HC reset/force-close actions
- Ready for Phase 23: PackageUserResponse table and question package assignment logic
- AssessmentMonitoringDetail now has full HC management capability for individual worker sessions

---
## Self-Check: PASSED

- Controllers/CMPController.cs — FOUND
- Views/CMP/AssessmentMonitoringDetail.cshtml — FOUND
- .planning/phases/22-exam-lifecycle-actions/22-04-SUMMARY.md — FOUND
- Commit d34546f — FOUND

*Phase: 22-exam-lifecycle-actions*
*Completed: 2026-02-20*
