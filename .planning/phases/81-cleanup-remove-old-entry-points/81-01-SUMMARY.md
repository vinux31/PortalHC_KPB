---
phase: 81-cleanup-remove-old-entry-points
plan: 01
subsystem: ui
tags: [razor, cleanup, views, assessment-monitoring]

# Dependency graph
requires:
  - phase: 79-assessment-monitoring-group-list
    provides: Assessment Monitoring page that replaces the old dropdown entry point
  - phase: 80-per-participant-monitoring-detail
    provides: Detail page confirming the new monitoring flow is complete
provides:
  - ManageAssessment dropdown without Monitoring item (CLN-01)
  - Admin/Index Section C with only 2 hub cards (Manage Assessment & Training + Assessment Monitoring) (CLN-02)
  - AssessmentMonitoring table with min-height viewport fill
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns: []

key-files:
  created: []
  modified:
    - Views/Admin/ManageAssessment.cshtml
    - Views/Admin/Index.cshtml
    - Views/Admin/AssessmentMonitoring.cshtml

key-decisions:
  - "CLN-01: Monitoring dropdown link removed entirely from ManageAssessment — monitoring now accessed exclusively via dedicated Assessment Monitoring page"
  - "CLN-02: Training Records hub card removed from Kelola Data hub — accessible via ManageAssessment Training Records tab"
  - "Table min-height matches ManageAssessment pattern (calc(100vh - 420px)) for consistent UX"

patterns-established: []

requirements-completed: [CLN-01, CLN-02]

# Metrics
duration: 2min
completed: 2026-03-01
---

# Phase 81 Plan 01: Cleanup — Remove Old Entry Points Summary

**Three surgical view edits: Monitoring dropdown removed from ManageAssessment, Training Records hub card removed from Kelola Data, AssessmentMonitoring table gets viewport-fill min-height**

## Performance

- **Duration:** ~2 min
- **Started:** 2026-03-01T10:42:38Z
- **Completed:** 2026-03-01T10:44:12Z
- **Tasks:** 2 auto + 1 checkpoint (pending human verify)
- **Files modified:** 3

## Accomplishments
- Removed the `bi-binoculars` Monitoring `<li>` from the assessment group dropdown in ManageAssessment — satisfies CLN-01
- Removed Training Records card block from Admin/Index.cshtml Section C — Section C now has exactly 2 cards: Manage Assessment & Training and Assessment Monitoring — satisfies CLN-02
- Added `min-height: calc(100vh - 420px); overflow-y: auto;` to the `table-responsive` div in AssessmentMonitoring.cshtml — matches ManageAssessment visual pattern

## Task Commits

Each task was committed atomically:

1. **Task 1: Remove Monitoring dropdown item from ManageAssessment (CLN-01)** - `c7ebae4` (feat)
2. **Task 2: Remove Training Records hub card + add table min-height (CLN-02)** - `ed0fabb` (feat)

## Files Created/Modified
- `Views/Admin/ManageAssessment.cshtml` - Removed 5-line Monitoring `<li>` block from assessment group dropdown
- `Views/Admin/Index.cshtml` - Removed 16-line Training Records card block from Section C
- `Views/Admin/AssessmentMonitoring.cshtml` - Added min-height style to table-responsive div

## Decisions Made
- None - followed plan as specified. All three edits were surgical single-block removals/additions with no ambiguity.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- `dotnet build` reported MSB3021 file-lock error (app already running in another process) — not a compilation error. No CS or RZ errors found. Razor view edits are pure HTML/template changes.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Awaiting human verification at checkpoint (Task 3)
- Once approved: Phase 81 / Plan 01 complete, v2.7 Assessment Monitoring milestone complete
- No blockers

---
*Phase: 81-cleanup-remove-old-entry-points*
*Completed: 2026-03-01*
