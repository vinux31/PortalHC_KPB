---
phase: 31-hc-reporting-actions
plan: 01
subsystem: ui, api
tags: [closedxml, xlsx, export, cmp, assessment, monitoring]

# Dependency graph
requires:
  - phase: 27-monitoring-status-fix
    provides: 4-state UserStatus logic (Completed/Abandoned/In Progress/Not Started) used in export row building
  - phase: 28-package-reassign-and-reshuffle
    provides: packageNameMap pattern (UserPackageAssignments join AssessmentPackages) reused for export
provides:
  - ExportAssessmentResults GET action in CMPController returning .xlsx of all assigned workers
  - Export Results button in AssessmentMonitoringDetail card header (GET form, no antiforgery)
affects:
  - 31-02 (ForceCloseAll — uses same monitoring detail page layout)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - ClosedXML export pattern (identical to CDPController.ExportAnalyticsResults): XLWorkbook, XLColor.LightBlue header, AdjustToContents, MemoryStream.ToArray(), File() return
    - Conditional column pattern: isPackageMode bool drives both header col++ and data row c++ for Package column
    - Filename sanitization: Regex.Replace(title, @"[^\w]", "_") for safe .xlsx filename

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/AssessmentMonitoringDetail.cshtml

key-decisions:
  - "ExportAssessmentResults uses GET (not POST) — file downloads do not require antiforgery; matches CDPController pattern"
  - "All workers exported regardless of status — no Status filter on query; HC needs full roster for reporting"
  - "Score column: write int as string via Score?.ToString() ?? '—' to avoid XLCellValue ambiguous overload; consistent with '—' null display pattern"
  - "isPackageMode package col: conditional on both col++ increment (header) and c++ increment (data rows) ensures correct column alignment"
  - "scheduleDate format yyyy-MM-dd from view matches ASP.NET Core DateTime model binding"

patterns-established:
  - "GET download button: <form method=get> with hidden inputs; no antiforgery; btn-outline-success to distinguish from action buttons"
  - "Export button placed inside d-flex gap-2 wrapper alongside existing action buttons — scales to future additions"

# Metrics
duration: 1min
completed: 2026-02-23
---

# Phase 31 Plan 01: HC Assessment Export Summary

**ClosedXML .xlsx export of all assigned workers (including not-started) via ExportAssessmentResults GET action with conditional Package column**

## Performance

- **Duration:** ~4 min
- **Started:** 2026-02-23T07:52:48Z
- **Completed:** 2026-02-23T07:57:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- New `ExportAssessmentResults` GET action queries all AssessmentSessions for a group (Title+Category+Schedule.Date) with no status filter, ensuring not-started workers appear in the export
- Row data uses 4-state status logic (Completed/Abandoned/In Progress/Not Started), matching the monitoring detail view
- Package detection via AssessmentPackages count; packageNameMap built via UserPackageAssignments join — same pattern as AssessmentMonitoringDetail GET
- Export Results button added to AssessmentMonitoringDetail card header inside `d-flex gap-2` wrapper alongside the existing Reshuffle All button

## Task Commits

Each task was committed atomically:

1. **Task 1: Add ExportAssessmentResults action to CMPController.cs** - `6cfe8f7` (feat)
2. **Task 2: Add Export Results button to AssessmentMonitoringDetail.cshtml** - `248e4e5` (feat)

**Plan metadata:** (see final commit below)

## Files Created/Modified
- `Controllers/CMPController.cs` - Added ExportAssessmentResults GET action (~120 lines) between ForceCloseAssessment and ReshufflePackage sections
- `Views/CMP/AssessmentMonitoringDetail.cshtml` - Replaced card-header button area with d-flex gap-2 wrapper containing new Export Results GET form and existing Reshuffle All button

## Decisions Made
- ExportAssessmentResults uses GET (not POST) — file downloads do not require antiforgery; matches CDPController.ExportAnalyticsResults pattern
- All workers exported regardless of status — no Status filter on query; HC needs full roster (including not-started) for reporting
- Score cell written as `r.Score?.ToString() ?? "—"` to avoid XLCellValue ambiguous overload with mixed int/string types
- Package column conditionally included: `isPackageMode` drives both header `col++` and data `c++` increments; `totalCols = isPackageMode ? 7 : 6` for header range styling
- scheduleDate passed from view as `yyyy-MM-dd` string — matches ASP.NET Core DateTime model binding without culture issues
- btn-outline-success for Export button to visually distinguish from blue Reshuffle All (btn-outline-primary)

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None. Build passed on first attempt with 0 errors (35 pre-existing warnings from CDPController and unrelated CMPController sections, unchanged from before this plan).

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness
- Phase 31-02 (ForceCloseAll) is ready to execute; same monitoring detail page is now updated with the Export button
- No blockers or concerns

---
*Phase: 31-hc-reporting-actions*
*Completed: 2026-02-23*

## Self-Check: PASSED

- FOUND: Controllers/CMPController.cs (contains ExportAssessmentResults)
- FOUND: Views/CMP/AssessmentMonitoringDetail.cshtml (contains ExportAssessmentResults form)
- FOUND: .planning/phases/31-hc-reporting-actions/31-01-SUMMARY.md
- Commit 6cfe8f7 verified in git log
- Commit 248e4e5 verified in git log
