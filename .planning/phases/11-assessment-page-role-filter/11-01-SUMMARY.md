---
phase: 11-assessment-page-role-filter
plan: 01
subsystem: ui
tags: [csharp, aspnetcore, razor, rbac, assessment, viewbag]

# Dependency graph
requires:
  - phase: 10-unified-training-records
    provides: Training Records page as destination for Completed assessments, making it safe to exclude Completed from worker Assessment view
provides:
  - Assessment() controller action with role-gated data: workers get Open+Upcoming only, HC/Admin get dual ViewBag datasets
  - isHCAccess named bool pattern applied to Assessment() action
  - ViewBag.ManagementData (all assessments, paginated, searchable) for HC/Admin manage view
  - ViewBag.MonitorData (Open+Upcoming, flat, schedule-ascending) for HC/Admin monitoring tab
affects:
  - 11-02 (Assessment view — will render ManagementData and MonitorData as two tabs)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - isHCAccess named bool for all HC/Admin gate checks (consistent with Phase 08 pattern)
    - Early return structural pattern for HC/Admin branch in Assessment() — cleaner than nested else
    - Dual ViewBag datasets: Model holds primary data (ManagementData), ViewBag.MonitorData holds secondary

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs

key-decisions:
  - "Admin always gets HC branch regardless of SelectedView — no SelectedView branching in Assessment()"
  - "Worker status filter applied at DB query level (.Where on EF IQueryable) — not post-query filtering"
  - "HC/Admin branch returns early — worker branch follows without else nesting"
  - "MonitorData has no pagination and no search filter — flat list sorted by Schedule ascending (soonest first)"
  - "Model passed to View() is managementData for HC/Admin branch — MonitorData is secondary, lives in ViewBag only"

patterns-established:
  - "isHCAccess named bool: bool isHCAccess = userRole == UserRoles.Admin || userRole == UserRoles.HC"
  - "Dual ViewBag pattern: ViewBag.ManagementData + ViewBag.MonitorData for HC/Admin two-tab pages"

# Metrics
duration: 6min
completed: 2026-02-18
---

# Phase 11 Plan 01: Assessment Role Filter (Controller) Summary

**CMPController.Assessment() rewritten with isHCAccess gate: workers receive Open+Upcoming only at DB level, HC/Admin receive dual ViewBag datasets (ManagementData paginated + MonitorData flat) for two-tab rendering in Plan 02**

## Performance

- **Duration:** ~6 min
- **Started:** 2026-02-18T12:42:43Z
- **Completed:** 2026-02-18T12:48:00Z
- **Tasks:** 2 (implemented as one atomic controller edit)
- **Files modified:** 1

## Accomplishments
- Added `bool isHCAccess = userRole == UserRoles.Admin || userRole == UserRoles.HC` — consistent with Phase 08 pattern
- Worker branch now filters to Open+Upcoming at the EF query level — Completed assessments excluded from DB result, not post-query
- HC/Admin branch restructured to early-return with two distinct data sets: ViewBag.ManagementData (all assessments, paginated, searchable) and ViewBag.MonitorData (Open+Upcoming, sorted by Schedule ascending, no pagination)
- Existing EditAssessment, DeleteAssessment, RegenerateToken redirects to `view = "manage"` confirmed intact

## Task Commits

Each task was committed atomically:

1. **Task 1+2: isHCAccess gate, worker status filter, dual ViewBag** - `f951ab3` (feat)

_Note: Task 1 and Task 2 were implemented as a single atomic edit to Assessment() — splitting into two separate commits would have left the action in an intermediate state between the two tasks._

**Plan metadata:** (docs commit follows)

## Files Created/Modified
- `Controllers/CMPController.cs` - Assessment() action rewritten with isHCAccess gate, worker status filter, HC/Admin early-return branch with ViewBag.ManagementData + ViewBag.MonitorData

## Decisions Made
- Admin always gets HC/Admin branch regardless of SelectedView — no `SelectedView == "HC"` check needed. This is consistent with the Phase 10 decision (Admin always elevated).
- Worker filter applied inside the personal/worker branch only — HC/Admin branch is completely separate code path.
- MonitorData has no search filter and no pagination — it serves the monitoring tab which needs a complete flat list of open assessments sorted soonest-first.
- Tasks 1 and 2 combined into a single commit since the change is one atomic rewrite of Assessment(). Splitting would leave a broken intermediate state.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Controller is ready: Assessment() returns ViewBag.ManagementData + ViewBag.MonitorData for HC/Admin
- Plan 02 (Assessment Razor view) can now render two tabs using these ViewBag datasets
- Worker view is clean — no Completed rows, no UI changes needed for the worker experience

---
*Phase: 11-assessment-page-role-filter*
*Completed: 2026-02-18*
