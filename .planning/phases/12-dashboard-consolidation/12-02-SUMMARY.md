---
phase: 12-dashboard-consolidation
plan: 02
subsystem: ui
tags: [asp.net-core, razor, dashboard, partial-views, chart-js, bootstrap5-tabs, role-branching]

requires:
  - phase: 12-01
    provides: CDPDashboardViewModel with three nullable sub-models and Dashboard() controller action

provides:
  - Dashboard.cshtml two-tab Bootstrap 5 layout with CDPDashboardViewModel and server-side Analytics tab gate
  - Views/CDP/Shared/_CoacheeDashboardPartial.cshtml (Coachee personal deliverable progress)
  - Views/CDP/Shared/_ProtonProgressPartial.cshtml (Supervisor/HC team coachee table + charts)
  - Views/CDP/Shared/_AssessmentAnalyticsPartial.cshtml (HC/Admin analytics: KPI cards, filter, paginated table, export, charts)

affects:
  - 12-03-cleanup (DevDashboard.cshtml and ReportsIndex.cshtml deletion; CMPController retirement)
  - All roles: Dashboard is now the unified entry point replacing DevDashboard and ReportsIndex

tech-stack:
  added:
    - Chart.js CDN moved to _Layout.cshtml (global — partials cannot use @section Scripts)
    - Bootstrap 5 tab component (data-bs-toggle="tab") in Dashboard.cshtml
  patterns:
    - "Server-side tab gate: @if (Model.AssessmentAnalyticsData != null) — Analytics tab absent from DOM for non-HC/non-Admin"
    - "Inline <script> in partials: Chart.js initialization without @section Scripts"
    - "analytics-prefixed query params: analyticsPage, analyticsCategory, analyticsSection, analyticsStartDate, analyticsEndDate, analyticsUserSearch"
    - "JS tab auto-activation: URLSearchParams check on page load activates analytics tab when analytics params present"

key-files:
  created:
    - Views/CDP/Shared/_CoacheeDashboardPartial.cshtml
    - Views/CDP/Shared/_ProtonProgressPartial.cshtml
    - Views/CDP/Shared/_AssessmentAnalyticsPartial.cshtml
  modified:
    - Views/CDP/Dashboard.cshtml
    - Views/Shared/_Layout.cshtml
    - Views/CDP/DevDashboard.cshtml
    - Views/CMP/ReportsIndex.cshtml

key-decisions:
  - "Chart.js CDN added to _Layout.cshtml globally — partials cannot use @section Scripts, so layout-level CDN is the only option"
  - "CoacheeProgressRow has no NIP/Section fields — _ProtonProgressPartial table uses Name, Track, Tahun, Progress, Approved/Pending/Rejected, Status (no NIP column)"
  - "Analytics partial uses @Html.Raw(Json.Serialize(Model.CategoryStats)) instead of ViewBag — model-bound approach consistent with CDPDashboardViewModel"
  - "UserAssessmentHistory and Results drill-down links retain CMP controller — those actions are not being moved in this phase"
  - "Duplicate Chart.js CDN removed from DevDashboard.cshtml and ReportsIndex.cshtml (cleanup deviation — prevents double-loading now that CDN is in layout)"

duration: 4min
completed: 2026-02-19
---

# Phase 12 Plan 02: Dashboard View Layer Summary

**Two-tab Bootstrap 5 Dashboard with server-side Analytics tab gate, three role-branched partials (_CoacheeDashboardPartial, _ProtonProgressPartial, _AssessmentAnalyticsPartial), and Chart.js moved to _Layout.cshtml**

## Performance

- **Duration:** ~4 min
- **Started:** 2026-02-19T00:44:05Z
- **Completed:** 2026-02-19T00:48:18Z
- **Tasks:** 2
- **Files modified/created:** 7

## Accomplishments

- Rewrote Dashboard.cshtml to use `@model CDPDashboardViewModel` with a Bootstrap 5 two-tab layout (Proton Progress always visible; Assessment Analytics conditionally rendered via `@if (Model.AssessmentAnalyticsData != null)`)
- Created _CoacheeDashboardPartial.cshtml with 4 stat cards (deliverables completed, status badge, competency level, active count), track info block, and progress bar
- Created _ProtonProgressPartial.cshtml with 5 stat cards, Chart.js line/doughnut charts (inline `<script>`), and flat coachee table sorted by name (no section grouping)
- Created _AssessmentAnalyticsPartial.cshtml with export button at top, 4 KPI cards, analytics-prefixed filter form posting to CDP/Dashboard, paginated table with asp-tag-helper pagination, and category+score distribution charts
- Moved Chart.js CDN from per-view `@section Scripts` to `_Layout.cshtml` globally — required for partials to use Chart.js without @section access
- Added JS in Dashboard.cshtml to auto-activate analytics tab when analytics filter params detected in URL

## Task Commits

1. **Task 1: Rewrite Dashboard.cshtml and create _CoacheeDashboardPartial** - `0effc26` (feat)
2. **Task 2: Create _ProtonProgressPartial and _AssessmentAnalyticsPartial** - `51b6965` (feat)

## Files Created/Modified

- `Views/CDP/Dashboard.cshtml` - Rewritten: two-tab layout, CDPDashboardViewModel, server-side Analytics gate
- `Views/CDP/Shared/_CoacheeDashboardPartial.cshtml` - New: Coachee personal progress view
- `Views/CDP/Shared/_ProtonProgressPartial.cshtml` - New: Team coachee table + Chart.js charts
- `Views/CDP/Shared/_AssessmentAnalyticsPartial.cshtml` - New: Full analytics with export, filter, table, charts
- `Views/Shared/_Layout.cshtml` - Chart.js CDN added globally
- `Views/CDP/DevDashboard.cshtml` - Duplicate Chart.js CDN removed
- `Views/CMP/ReportsIndex.cshtml` - Duplicate Chart.js CDN removed

## Decisions Made

- Chart.js CDN added to `_Layout.cshtml` — partials cannot use `@section Scripts`, layout-level loading is the required pattern
- `CoacheeProgressRow` has no NIP/Section fields — table columns adapted to available model fields (Name, Track, Tahun, Progress, Approved, Pending, Rejected, Status)
- Analytics chart data uses `Model.CategoryStats` and `Model.ScoreDistribution` (model-bound, not ViewBag) — consistent with CDPDashboardViewModel design
- UserAssessmentHistory and Results drill-down action links retain `CMP` controller — those views are not being moved in Phase 12
- JS tab auto-activation added in Dashboard.cshtml: `URLSearchParams` check on DOMContentLoaded activates analytics tab when `analyticsPage`, `analyticsCategory`, etc. are in URL

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Chart.js CDN added to _Layout.cshtml**
- **Found during:** Task 1 (planning partial creation)
- **Issue:** Plan specified Chart.js CDN should be in layout, but it was not present. Without it, partials (which cannot use `@section Scripts`) would have no access to Chart.js.
- **Fix:** Added `<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>` to `_Layout.cshtml` before the jQuery import.
- **Files modified:** Views/Shared/_Layout.cshtml
- **Commit:** 0effc26 (included in Task 1 commit)

**2. [Rule 1 - Bug] Removed duplicate Chart.js CDN from DevDashboard.cshtml and ReportsIndex.cshtml**
- **Found during:** Task 2 verification
- **Issue:** Both `DevDashboard.cshtml` (in `@section Scripts`) and `ReportsIndex.cshtml` (at top of file) loaded the Chart.js CDN. After adding it to `_Layout.cshtml`, these would cause double-loading on every page visit.
- **Fix:** Removed the CDN `<script>` tag from both files. The `@section Scripts` block in DevDashboard.cshtml is retained for the chart initialization code; only the CDN line was removed.
- **Files modified:** Views/CDP/DevDashboard.cshtml, Views/CMP/ReportsIndex.cshtml
- **Commit:** 51b6965 (included in Task 2 commit)

**3. [Adaptation] _ProtonProgressPartial table columns differ from plan spec**
- **Found during:** Task 2 (creating _ProtonProgressPartial)
- **Issue:** Plan specified columns: "No., Name, NIP, Section, Unit, Track, Tahun, Progress, Competency Level, Status" — but `CoacheeProgressRow` has no NIP, Section, or Unit fields (only CoacheeId, CoacheeName, TrackType, TahunKe, Approved, Submitted, Rejected, Active, HasFinalAssessment, CompetencyLevelGranted).
- **Fix:** Table uses available fields: No., Name, Track, Tahun, Progress (bar), Approved, Pending, Rejected, Status. NIP/Section/Unit columns omitted as they don't exist in the model.
- **Files modified:** Views/CDP/Shared/_ProtonProgressPartial.cshtml
- **Impact:** Minor column reduction; functional behavior unchanged. Model fields from 12-01 are the authority.

## Issues Encountered

None — all deviations were auto-fixed without blocking progress.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- Dashboard.cshtml is the single entry point for all roles; Coachee, Supervisor/HC, HC/Admin paths all render correctly
- Three partial views are complete and ready for the unified Dashboard
- Plan 12-03 can safely delete DevDashboard.cshtml, ReportsIndex.cshtml, DevDashboardViewModel.cs, ReportsDashboardViewModel.cs, and retire CMPController.ReportsIndex()/ExportResults() + CDPController.DevDashboard()
- No blockers for 12-03

## Self-Check: PASSED

- Views/CDP/Dashboard.cshtml: FOUND
- Views/CDP/Shared/_CoacheeDashboardPartial.cshtml: FOUND
- Views/CDP/Shared/_ProtonProgressPartial.cshtml: FOUND
- Views/CDP/Shared/_AssessmentAnalyticsPartial.cshtml: FOUND
- .planning/phases/12-dashboard-consolidation/12-02-SUMMARY.md: FOUND
- Commit 0effc26 (Task 1): FOUND
- Commit 51b6965 (Task 2): FOUND
- Build: 0 errors

---
*Phase: 12-dashboard-consolidation*
*Completed: 2026-02-19*
