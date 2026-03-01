---
phase: 79-assessment-monitoring-page-group-list
plan: 01
subsystem: ui
tags: [assessment, monitoring, asp-net-core, razor, bootstrap]

# Dependency graph
requires:
  - phase: 78-training-records-hub
    provides: Admin/Index hub structure with Section C cards pattern
  - phase: 77-assessment-monitoring-detail
    provides: AssessmentMonitoringDetail action and MonitoringGroupViewModel used as base
provides:
  - Assessment Monitoring group list page at /Admin/AssessmentMonitoring
  - Extended MonitoringGroupViewModel with IsTokenRequired and AccessToken
  - Hub card in Admin/Index Section C linking to AssessmentMonitoring
  - Filter bar with status/category/search, group table with progress bars and badge system
affects:
  - Phase 80 (per-participant monitoring detail and HC actions — will link from this page)
  - Phase 81 (cleanup — will remove old monitoring dropdown from ManageAssessment)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Filter bar using GET form with @if/@else selected option pattern (not ternary in tag helper attrs)"
    - "GroupStatus computed after DB fetch by inspecting session statuses (Open/InProgress -> Open, Upcoming -> Upcoming, else Closed)"
    - "Default filter excludes Closed groups; ViewBag.SelectedStatus set to 'active' as default sentinel"

key-files:
  created:
    - Views/Admin/AssessmentMonitoring.cshtml
  modified:
    - Models/AssessmentMonitoringViewModel.cs
    - Controllers/AdminController.cs
    - Views/Admin/Index.cshtml

key-decisions:
  - "Status filter applied AFTER grouping because GroupStatus is computed from session statuses in memory, not available in DB query"
  - "Default display shows Open + Upcoming groups only (status='active' sentinel); user must explicitly select 'Semua Status' or 'Closed' to see closed groups"
  - "Razor option selected uses @if/@else blocks (project pattern from ManageWorkers) — ternary in tag helper attr attribute area throws RZ1031 error"

patterns-established:
  - "AssessmentMonitoring action placed before AssessmentMonitoringDetail to keep monitoring actions grouped in AdminController"
  - "Hub card uses bi-binoculars icon with text-success for monitoring/oversight distinction from management (bi-sliders text-primary)"

requirements-completed:
  - MON-01
  - MON-02
  - MON-05

# Metrics
duration: 20min
completed: 2026-03-01
---

# Phase 79 Plan 01: Assessment Monitoring Group List Summary

**Dedicated Assessment Monitoring page (/Admin/AssessmentMonitoring) with group list, status/category/search filters, progress bars, and Regenerate Token support — giving HC/Admin a first-class monitoring home beyond the ManageAssessment dropdown**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-03-01T09:02:00Z
- **Completed:** 2026-03-01T09:22:00Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- Extended MonitoringGroupViewModel with IsTokenRequired and AccessToken properties needed for token regeneration on monitoring page
- Added AssessmentMonitoring GET action to AdminController with 7-day window, text search, status/category filters, and GroupStatus computed post-grouping
- Added Assessment Monitoring hub card to Admin/Index Section C (visible to Admin and HC roles)
- Created AssessmentMonitoring.cshtml with summary stat cards, filter bar, group table (category/status badges, progress bars, participant counts), Regenerate Token button for token-required groups, and JS fetch for token regeneration

## Task Commits

Each task was committed atomically:

1. **Task 1: Extend ViewModel, add AssessmentMonitoring action, hub card** - `eea9122` (feat)
2. **Task 2: Create AssessmentMonitoring.cshtml group list view** - `b2bf2bb` (feat)

## Files Created/Modified
- `Models/AssessmentMonitoringViewModel.cs` - Added IsTokenRequired (bool) and AccessToken (string) properties to MonitoringGroupViewModel
- `Controllers/AdminController.cs` - Added AssessmentMonitoring GET action with filtering and GroupStatus computation
- `Views/Admin/Index.cshtml` - Added Assessment Monitoring hub card in Section C (bi-binoculars icon, Admin/HC gated)
- `Views/Admin/AssessmentMonitoring.cshtml` - New group list view: stat cards, filter bar, group table with badges/progress bars/dropdown actions, JS token regeneration

## Decisions Made
- Status filter applied after in-memory grouping because GroupStatus is derived from session statuses (not a DB column), so it cannot be applied in the EF query
- Default page load uses `status="active"` sentinel value (shows Open + Upcoming only), so users immediately see actionable groups without needing to change filters
- Razor `<option selected>` pattern uses `@if/@else` blocks per ManageWorkers project convention — ternary expressions in tag helper attribute areas trigger RZ1031 compiler error

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed Razor RZ1031 error: option selected attribute with C# ternary**
- **Found during:** Task 2 (AssessmentMonitoring.cshtml creation)
- **Issue:** Initial view used `@(condition ? "selected" : "")` in `<option>` tag attributes; Razor tag helpers throw RZ1031 (no C# in element attribute declaration area) for this pattern
- **Fix:** Replaced all option selected logic with `@if/@else` blocks matching the established ManageWorkers project pattern
- **Files modified:** Views/Admin/AssessmentMonitoring.cshtml
- **Verification:** Build passed with 0 errors after fix
- **Committed in:** b2bf2bb (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 Rule 1 bug fix)
**Impact on plan:** Required fix for correctness; no scope creep.

## Issues Encountered
- Razor RZ1031 compile error on option selected pattern — resolved by switching to @if/@else blocks per project convention (see Deviations)

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Phase 79 Plan 01 complete: group list page live at /Admin/AssessmentMonitoring
- Ready for Phase 80: per-participant monitoring detail and HC actions (override score, reshuffle, etc.)
- View Detail links on each group row correctly point to AssessmentMonitoringDetail with title/category/scheduleDate params
- Regenerate Token button wired to existing /Admin/RegenerateToken endpoint

## Self-Check: PASSED

All required files found and commits verified.

---
*Phase: 79-assessment-monitoring-page-group-list*
*Completed: 2026-03-01*
