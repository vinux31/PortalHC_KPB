---
phase: 16-grouped-monitoring-view
plan: "01"
subsystem: ui
tags: [csharp, asp-net-core, razor, viewmodel, entity-framework, monitoring]

# Dependency graph
requires:
  - phase: 14-bulk-assign
    provides: "grouped manage view pattern (Title+Category+Schedule.Date) and AssessmentSession model"
provides:
  - "MonitoringGroupViewModel and MonitoringSessionViewModel classes in HcPortal.Models"
  - "Updated Assessment() monitor query — grouped by assessment identity, includes recently-closed sessions"
  - "AssessmentMonitoringDetail GET action for per-group detail page"
affects:
  - 16-02-plan (monitoring tab view depends on MonitoringGroupViewModel shape in ViewBag.MonitorData)
  - 16-03-plan (detail page view depends on AssessmentMonitoringDetail action and MonitoringGroupViewModel model)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "GroupBy (Title, Category, Schedule.Date) in-memory after EF ToListAsync — consistent with manage view grouping pattern"
    - "GroupStatus priority: Open > Upcoming > Closed — derived from any() checks on constituent sessions"
    - "Dual sort: OrderBy(Closed=1 else 0) ThenBy(non-Closed scheduleAsc) ThenByDescending(Closed scheduleDesc)"

key-files:
  created:
    - Models/AssessmentMonitoringViewModel.cs
  modified:
    - Controllers/CMPController.cs

key-decisions:
  - "In-memory grouping (ToListAsync then GroupBy) — consistent with existing manage view pattern; avoids complex EF GroupBy translation"
  - "30-day cutoff uses DateTime.UtcNow.AddDays(-30) to match CompletedAt which is stored in UTC"
  - "UserStatus derived as 'Completed' if Score != null || CompletedAt != null (matches derivation rules in plan)"
  - "AssessmentMonitoringDetail placed immediately before EditAssessment to keep assessment-related actions contiguous"

patterns-established:
  - "MonitoringGroupViewModel is the canonical shape for all monitoring data — Plans 02 and 03 depend on this exact property set"
  - "AssessmentMonitoringDetail route: /CMP/AssessmentMonitoringDetail?title=X&category=Y&scheduleDate=Z"

# Metrics
duration: 2min
completed: 2026-02-19
---

# Phase 16 Plan 01: Server-Side Monitoring Foundation Summary

**MonitoringGroupViewModel + grouped query (Open/Upcoming/Closed-30d) in Assessment() and AssessmentMonitoringDetail GET action, enabling Plans 02 and 03 to build the monitoring UI**

## Performance

- **Duration:** 2 min
- **Started:** 2026-02-19T13:01:03Z
- **Completed:** 2026-02-19T13:03:03Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Created `Models/AssessmentMonitoringViewModel.cs` with `MonitoringGroupViewModel` (group-level data: TotalCount, CompletedCount, PassedCount, Sessions list) and `MonitoringSessionViewModel` (per-user data: Id, UserFullName, UserNIP, UserStatus, Score, IsPassed, CompletedAt)
- Replaced flat `List<AssessmentSession>` monitor query with grouped `List<MonitoringGroupViewModel>` that includes Completed sessions within last 30 days, sorted Open/Upcoming soonest-first then Closed most-recent-first
- Added `AssessmentMonitoringDetail` GET action with `[Authorize(Roles = "Admin, HC")]`, querying by `title+category+scheduleDate.Date`, returning `MonitoringGroupViewModel` with full session list and `ViewBag.BackUrl`

## Task Commits

Each task was committed atomically:

1. **Task 1: Create MonitoringGroupViewModel and MonitoringSessionViewModel** - `f874c52` (feat)
2. **Task 2: Update Assessment() monitor query and add AssessmentMonitoringDetail GET action** - `543ae8a` (feat)

**Plan metadata:** (docs commit — see below)

## Files Created/Modified

- `Models/AssessmentMonitoringViewModel.cs` — Two ViewModel classes: `MonitoringGroupViewModel` (group summary) and `MonitoringSessionViewModel` (per-user row)
- `Controllers/CMPController.cs` — Replaced 6-line flat monitor query with 55-line grouped projection; added 55-line `AssessmentMonitoringDetail` action

## Decisions Made

- In-memory grouping after `ToListAsync()` — consistent with existing manage view pattern; avoids complex EF GroupBy translation issues
- `DateTime.UtcNow.AddDays(-30)` cutoff for recently-closed sessions — UTC matches `CompletedAt` storage convention
- `UserStatus` derived as `"Completed"` when `Score != null || CompletedAt != null` — matches plan derivation spec
- `AssessmentMonitoringDetail` placed immediately before `EditAssessment` to keep assessment-related actions grouped

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None. The app server was running (HcPortal.exe file lock during build) causing MSB3026 warnings, but compilation completed successfully with 0 errors. All pre-existing CS8602 warnings in CDPController are unchanged.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- `ViewBag.MonitorData` is now `List<MonitoringGroupViewModel>` — Plan 02 can build the monitoring tab Razor view against this shape
- `AssessmentMonitoringDetail` action exists and is authorized — Plan 03 can create `Views/CMP/AssessmentMonitoringDetail.cshtml` targeting `MonitoringGroupViewModel` as the typed model
- Route for detail page: `/CMP/AssessmentMonitoringDetail?title=X&category=Y&scheduleDate=Z`

---
*Phase: 16-grouped-monitoring-view*
*Completed: 2026-02-19*
