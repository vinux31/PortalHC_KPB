---
phase: 07-development-dashboard
plan: 02
subsystem: dashboard
tags: [asp.net-core, chart.js, bootstrap, razor, role-based-nav]

# Dependency graph
requires:
  - phase: 07-01
    provides: DevDashboardViewModel, CoacheeProgressRow, CDPController.DevDashboard GET action with role-scoped data

provides:
  - DevDashboard.cshtml view with Bootstrap summary cards, per-coachee progress table, Chart.js line chart, doughnut chart
  - _Layout.cshtml nav link visible to Coach/SrSupervisor/SectionHead/HC/Admin roles

affects:
  - End users (Coach, HC, Admin) — now have a visual development dashboard in the top nav

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "@Json.Serialize() for chart data arrays — matches existing Dashboard.cshtml pattern in this project"
    - "@section Scripts {} integrates with _Layout.cshtml @await RenderSectionAsync('Scripts', required: false)"
    - "Empty-state alert-info guards around Chart.js canvas elements prevent JS errors on no-data scenarios"

key-files:
  created:
    - Views/CDP/DevDashboard.cshtml
  modified:
    - Views/Shared/_Layout.cshtml

key-decisions:
  - "@Json.Serialize() used instead of Html.Raw(JsonSerializer.Serialize()) — matches existing Dashboard.cshtml chart pattern"
  - "Nav link condition uses userRole variable already computed at top of _Layout.cshtml — no additional DI required"
  - "Empty-state guards on both charts: TrendLabels.Any() for line chart, StatusData.Any(d => d > 0) for doughnut"

patterns-established:
  - "Pattern: Chart.js canvas wrapped in @if(Model.Data.Any()) block with alert-info else branch — prevents runtime JS errors when scope has no data"

# Metrics
duration: 2min
completed: 2026-02-18
---

# Phase 7 Plan 2: Development Dashboard View Summary

**DevDashboard.cshtml with Bootstrap summary cards, per-coachee progress table, Chart.js line/doughnut charts, and role-gated _Layout.cshtml nav link completing the Phase 7 development dashboard**

## Performance

- **Duration:** ~2 min
- **Started:** 2026-02-18T03:04:42Z
- **Completed:** 2026-02-18T03:06:26Z
- **Tasks:** 2 (+ 1 checkpoint awaiting human verification)
- **Files modified:** 2

## Accomplishments

- Created `Views/CDP/DevDashboard.cshtml` with 5 summary cards (TotalCoachees, Approved/Total Deliverables, PendingSpvApprovals, PendingHCReviews, CompletedCoachees), per-coachee progress table with progress bars and status badges, line chart (trendChart) with empty-state guard, doughnut chart (statusChart) with empty-state guard, and ScopeLabel subtitle
- Added "Dev Dashboard" nav link to `_Layout.cshtml` between CDP and BP nav items, conditional on Coach/SrSupervisor/SectionHead/HC/Admin roles — Coachee role does not see the link
- Both Chart.js charts use `@Json.Serialize()` pattern matching existing `Dashboard.cshtml` and integrate via `@section Scripts {}` / `@await RenderSectionAsync("Scripts", required: false)` in the layout

## Task Commits

Each task was committed atomically:

1. **Task 1: Create DevDashboard.cshtml view** - `d87a857` (feat)
2. **Task 2: Add DevDashboard nav link to _Layout.cshtml** - `c30ef65` (feat)

**Task 3:** Checkpoint — awaiting human verification

**Plan metadata:** (docs commit to follow after verification)

## Files Created/Modified

- `Views/CDP/DevDashboard.cshtml` - Complete development dashboard view: summary cards, per-coachee table, trendChart (line), statusChart (doughnut), empty-state guards, @section Scripts block
- `Views/Shared/_Layout.cshtml` - "Dev Dashboard" nav link added between CDP and BP, visible to Coach/SrSupervisor/SectionHead/HC/Admin only

## Decisions Made

- Used `@Json.Serialize()` for chart data serialization — matches `Dashboard.cshtml` pattern; avoids introducing `System.Text.Json.JsonSerializer` divergence
- Nav link condition reuses the `userRole` variable already computed at lines 6-7 of `_Layout.cshtml` — no additional service injection needed
- Empty-state guards: `Model.TrendLabels.Any()` for line chart, `Model.StatusData.Any(d => d > 0)` for doughnut — prevents Chart.js JS errors when canvas element is absent

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 7 complete pending human verification (Task 3 checkpoint)
- Dashboard is fully functional: backend (07-01) + frontend (07-02) both committed and build exits 0 errors
- Verification steps: log in as Coach/HC/Admin, navigate to Dev Dashboard, confirm summary cards, table, charts, and nav link visibility by role

---
*Phase: 07-development-dashboard*
*Completed: 2026-02-18*
