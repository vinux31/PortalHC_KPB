---
phase: 12-dashboard-consolidation
plan: 01
subsystem: ui
tags: [asp.net-core, viewmodel, dashboard, role-branching, excel, closedxml]

requires:
  - phase: 07-development-dashboard
    provides: DevDashboard scoping logic (HC/Admin=all, SrSpv/SectionHead=section, Coach=unit) and CoacheeProgressRow shape
  - phase: 02-hc-reports-dashboard
    provides: Assessment analytics query pattern and ReportsDashboardViewModel field set

provides:
  - CDPDashboardViewModel wrapper with three nullable sub-models
  - CoacheeDashboardSubModel, ProtonProgressSubModel, AssessmentAnalyticsSubModel classes
  - CoacheeProgressRow, AssessmentReportItem, ReportFilters, CategoryStatistic supporting classes (canonical home)
  - CDPController.Dashboard() consolidated action with role-branched sub-model population
  - BuildCoacheeSubModelAsync, BuildProtonProgressSubModelAsync, BuildAnalyticsSubModelAsync private helpers
  - CDPController.ExportAnalyticsResults() with ClosedXML Excel export
  - CDPController.SearchUsers() for Analytics autocomplete

affects:
  - 12-02-view (Dashboard.cshtml tab layout uses CDPDashboardViewModel sub-model nullability)
  - 12-03-cleanup (DevDashboardViewModel and ReportsDashboardViewModel deletion; CMPController actions retirement)

tech-stack:
  added:
    - ClosedXML (already in project — now also imported in CDPController)
    - HcPortal.Helpers (OrganizationStructure.GetAllSections() for Analytics section dropdown)
  patterns:
    - "Wrapper ViewModel with nullable sub-models: controller populates only what role needs; view renders from non-null sub-models"
    - "isLiteralCoachee vs isHCAccess role gate: Coachee early-return, HC/Admin analytics gate ignores SelectedView"
    - "_lastScopeLabel instance field pattern for passing scope context from helper to Dashboard() action"

key-files:
  created:
    - Models/CDPDashboardViewModel.cs
  modified:
    - Controllers/CDPController.cs
    - Models/DevDashboardViewModel.cs
    - Models/ReportsDashboardViewModel.cs

key-decisions:
  - "isHCAccess for Analytics tab: userRole == HC || Admin — SelectedView NOT checked (Admin simulating Coachee still sees Analytics per Phase 12 Context.md locked decision)"
  - "isLiteralCoachee: userRole == Coachee only — Admin simulating Coachee goes through ProtonProgress path (not Coachee early-return)"
  - "Duplicate supporting classes removed from DevDashboardViewModel.cs and ReportsDashboardViewModel.cs — CDPDashboardViewModel.cs is the canonical home going forward"
  - "ScopeLabel propagated from BuildProtonProgressSubModelAsync via _lastScopeLabel instance field (helper cannot set wrapper model directly)"

duration: 4min
completed: 2026-02-19
---

# Phase 12 Plan 01: Dashboard Consolidation — ViewModel and Controller Summary

**CDPDashboardViewModel with three nullable role-branched sub-models and rewritten Dashboard() action with ProtonProgress scoping, Analytics query, ExportAnalyticsResults, and SearchUsers on CDPController**

## Performance

- **Duration:** ~4 min
- **Started:** 2026-02-19T00:36:51Z
- **Completed:** 2026-02-19T00:40:53Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments

- Created CDPDashboardViewModel.cs as a self-contained file with 8 classes: CDPDashboardViewModel, CoacheeDashboardSubModel, ProtonProgressSubModel, AssessmentAnalyticsSubModel, CoacheeProgressRow, AssessmentReportItem, ReportFilters, CategoryStatistic
- Rewrote CDPController.Dashboard() to serve all roles: Coachee gets CoacheeData only (early return), non-Coachee gets ProtonProgressData with role-scoped coachee list, HC/Admin additionally get AssessmentAnalyticsData regardless of SelectedView
- Moved ExportAnalyticsResults (from CMPController.ExportResults) and SearchUsers to CDPController with proper authorization
- Build passes with 0 errors; DevDashboard() action untouched (lives until 12-03 cleanup)

## Task Commits

1. **Task 1: Create CDPDashboardViewModel with three sub-models** - `14ba2a5` (feat)
2. **Task 2: Rewrite CDPController.Dashboard() and add ExportAnalyticsResults + SearchUsers** - `8ca8482` (feat)

## Files Created/Modified

- `Models/CDPDashboardViewModel.cs` - New wrapper ViewModel with all 8 classes; canonical home for CoacheeProgressRow, AssessmentReportItem, ReportFilters, CategoryStatistic
- `Controllers/CDPController.cs` - Dashboard() rewritten; BuildCoacheeSubModelAsync, BuildProtonProgressSubModelAsync, BuildAnalyticsSubModelAsync helpers added; ExportAnalyticsResults and SearchUsers added
- `Models/DevDashboardViewModel.cs` - Duplicate CoacheeProgressRow class removed (now in CDPDashboardViewModel.cs)
- `Models/ReportsDashboardViewModel.cs` - Duplicate AssessmentReportItem, ReportFilters, CategoryStatistic classes removed (now in CDPDashboardViewModel.cs)

## Decisions Made

- isHCAccess gate for Analytics tab uses `userRole == HC || userRole == Admin` — explicitly does NOT check SelectedView per Phase 12 locked decision (Admin simulating Coachee still sees Analytics tab)
- isLiteralCoachee check is `userRole == Coachee` only — Admin simulating Coachee goes to ProtonProgress path, not Coachee early-return
- Supporting classes (CoacheeProgressRow, AssessmentReportItem, ReportFilters, CategoryStatistic) are canonical in CDPDashboardViewModel.cs; old files retain only their main ViewModel classes (DevDashboardViewModel, ReportsDashboardViewModel) to avoid breaking CMPController and CDPController.DevDashboard() until 12-03 cleanup

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Removed duplicate class definitions from old ViewModel files**
- **Found during:** Task 1 (CDPDashboardViewModel creation)
- **Issue:** CoacheeProgressRow was defined in DevDashboardViewModel.cs and AssessmentReportItem, ReportFilters, CategoryStatistic were defined in ReportsDashboardViewModel.cs. Adding them to CDPDashboardViewModel.cs caused CS0101 "namespace already contains definition" build errors.
- **Fix:** Removed the duplicate class definitions from DevDashboardViewModel.cs and ReportsDashboardViewModel.cs, keeping only the main ViewModel classes in those files. The canonical definitions are now exclusively in CDPDashboardViewModel.cs.
- **Files modified:** Models/DevDashboardViewModel.cs, Models/ReportsDashboardViewModel.cs
- **Verification:** dotnet build passes with 0 errors after fix
- **Committed in:** 14ba2a5 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (Rule 3 — blocking build error)
**Impact on plan:** Essential fix; plan instruction said "copy classes here" but old files had them defined, creating name conflicts. Fix is aligned with plan intent (12-03 will delete old files entirely). No scope creep.

## Issues Encountered

None — build error from duplicate classes was immediately identified and resolved via deviation Rule 3.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- CDPDashboardViewModel.cs is the complete data backbone for Plan 12-02 (view layer)
- Dashboard() action correctly serves all roles; Plan 12-02 can use sub-model nullability for tab rendering
- ExportAnalyticsResults and SearchUsers are ready on CDPController for the Analytics partial in 12-02
- DevDashboard() action remains intact on CDPController — Plan 12-03 deletes it
- CMPController.ReportsIndex() remains intact — Plan 12-03 deletes it
- No blockers for 12-02

---
*Phase: 12-dashboard-consolidation*
*Completed: 2026-02-19*
