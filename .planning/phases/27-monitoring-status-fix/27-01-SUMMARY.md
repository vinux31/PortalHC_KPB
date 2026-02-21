---
phase: 27-monitoring-status-fix
plan: 01
subsystem: api
tags: [csharp, aspnet, linq, assessment, monitoring]

# Dependency graph
requires:
  - phase: 22-exam-lifecycle-actions
    provides: Abandoned status on AssessmentSession, StartedAt column preserved on Abandon

provides:
  - GetMonitorData endpoint with 4-state UserStatus projection (Completed / Abandoned / In Progress / Not started)
  - Abandoned sessions included in GetMonitorData WHERE clause (previously excluded)
  - StartedAt projected in GetMonitorData SELECT for InProgress detection
affects:
  - 28-reshuffle (monitoring card state used by reshuffle guard)
  - Views that render monitoring card JSON (groupStatus/completedCount unaffected)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "4-state UserStatus ordering: Completed > Abandoned > In Progress > Not started (Abandoned before InProgress because Abandoned sessions have StartedAt set)"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Models/AssessmentMonitoringViewModel.cs

key-decisions:
  - "Abandoned branch placed before InProgress in UserStatus projection — Abandoned sessions have StartedAt set and would otherwise be misclassified as InProgress (mirrors existing Phase 22-04 decision in AssessmentMonitoringDetail)"
  - "groupStatus and completedCount/passedCount aggregations untouched — groupStatus uses a.Status (DB field), completedCount uses s.UserStatus == Completed which remains correct"

patterns-established:
  - "4-state UserStatus pattern (Completed > Abandoned > In Progress > Not started) now consistent between GetMonitorData and AssessmentMonitoringDetail"

# Metrics
duration: 3min
completed: 2026-02-21
---

# Phase 27 Plan 01: Monitoring Status Fix Summary

**GetMonitorData expanded from 2-state to 4-state UserStatus — Abandoned sessions now visible in monitoring card with correct In Progress and Abandoned labels instead of Not started**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-02-21T05:38:00Z
- **Completed:** 2026-02-21T05:41:18Z
- **Tasks:** 2 of 2 implementation tasks complete (Task 3 is human-verify checkpoint)
- **Files modified:** 2

## Accomplishments
- Added Abandoned to WHERE clause — Abandoned sessions now appear in the monitoring card (previously excluded)
- Added StartedAt to SELECT projection so InProgress state can be detected in C# grouping code
- Replaced 2-state isCompleted logic with 4-state branch: Completed > Abandoned > In Progress > Not started
- Updated MonitoringSessionViewModel UserStatus comment to document all 4 valid states
- Build passes with zero errors

## Task Commits

Each task was committed atomically:

1. **Task 1: Fix GetMonitorData — WHERE, SELECT, and UserStatus projection** - `faad7d2` (feat)
2. **Task 2: Update MonitoringSessionViewModel UserStatus comment** - `b9d4d23` (chore)

**Plan metadata:** (docs commit after checkpoint verification)

## Files Created/Modified
- `Controllers/CMPController.cs` - WHERE clause + SELECT + 4-state UserStatus projection in GetMonitorData
- `Models/AssessmentMonitoringViewModel.cs` - UserStatus comment updated to list all 4 states

## Decisions Made
- Abandoned branch placed before InProgress in the 4-state projection — this mirrors the existing Phase 22-04 decision in AssessmentMonitoringDetail and is critical because Abandoned sessions have StartedAt set; without this ordering they would show as In Progress
- groupStatus, completedCount, passedCount aggregations left untouched — they operate on a.Status (DB field) and s.UserStatus == "Completed" respectively, both of which remain correct with the new projection

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered
None — StartedAt was already defined on MonitoringSessionViewModel (added in an earlier phase), so no model change was needed. Only CMPController needed the SELECT and projection updates.

## User Setup Required
None — no external service configuration required.

## Next Phase Readiness
- GetMonitorData now returns accurate 4-state statuses; monitoring card view will display correct labels once frontend templates consume UserStatus
- Ready for Phase 28 (re-assign/reshuffle) after human verification of Task 3 confirms correct JSON output

---
*Phase: 27-monitoring-status-fix*
*Completed: 2026-02-21*
