---
phase: 345-assessment-pending-grade-display-fix
plan: 02
subsystem: api
tags: [admin, assessment, history, stats, viewmodel, nullable, razor, display-correctness]

requires:
  - phase: 345
    provides: PendingGrading constant convention (Plan 01)
provides:
  - "AssessmentReportItem.IsPassed bool->bool? (CDPDashboardViewModel.cs:111)"
  - "UserAssessmentHistoryViewModel GradedCount + PendingCount props"
  - "AssessmentAdminController.ComputeHistoryStats public static helper (passRate exclude-pending math)"
  - "UserAssessmentHistory.cshtml 3-way badge + passRate guard + pending indicator"
affects: [345-04, 348]

tech-stack:
  added: []
  patterns: ["Static pure-math helper (ComputeHistoryStats) extracted from controller for unit testability without 10-dep ctor"]

key-files:
  created: []
  modified:
    - Models/CDPDashboardViewModel.cs
    - Models/ReportsDashboardViewModel.cs
    - Controllers/AssessmentAdminController.cs
    - Views/Admin/UserAssessmentHistory.cshtml

key-decisions:
  - "C-1: VM target = CDPDashboardViewModel.cs:111 (verified), bukan ReportsDashboardViewModel.cs"
  - "D-07: averageScore exclude pending (gradedItems.Average) — pending Score=0 skew"
  - "D-06: indikator 'Menunggu Penilaian: N' sebagai badge sub-line di kartu Pass Rate"
  - "Test strategy Opsi A: ekstrak ComputeHistoryStats static (10-dep ctor bikin Opsi B rapuh)"
  - "C-3: group PassedCount/projection (L2712/L2821) tak disentuh — sudah exclude pending"

patterns-established:
  - "passRate denominator = graded-only (IsPassed != null); all-pending -> 'Belum ada penilaian' guard"

requirements-completed: [CMP06R-02]

duration: 18min
completed: 2026-06-04
---

# Phase 345 Plan 02: UserAssessmentHistory pending stats Summary

**Surface /Admin/UserAssessmentHistory kini render sesi pending sebagai badge amber "Menunggu Penilaian", passRate exclude pending dari denominator (graded-only), all-pending tampil "Belum ada penilaian", + indikator "Menunggu Penilaian: N" — via VM nullable ripple + ComputeHistoryStats static helper.**

## Performance
- **Duration:** ~18 min
- **Completed:** 2026-06-04
- **Tasks:** 3 (committed as 1 atomic ripple unit)
- **Files modified:** 4

## Accomplishments
- `AssessmentReportItem.IsPassed` `bool`→`bool?` (CDPDashboardViewModel.cs:111, C-1 correct file) — pending preserved as null, no collapse.
- `UserAssessmentHistoryViewModel` + `GradedCount` + `PendingCount`.
- `ComputeHistoryStats` public static helper: `passRate = passed*100/graded` (graded-only denom, D-04), `averageScore` over graded only (D-07), returns (total, graded, pending, passed, passRate, averageScore). Drop `?? false` in action projection.
- View: 3-way badge (null→amber), passRate "Belum ada penilaian" guard at mini-stat + card (D-05), "Menunggu Penilaian: N" badge sub-line (D-06).
- C-3 verified: group projection L2712 + PassedCount L2821 unchanged (0 code change).

## Task Commits
Tasks 1-3 committed together as one atomic nullable-ripple unit (build green only after all three land):
1. **Task 1+2+3: VM bool? ripple + ComputeHistoryStats + view 3-way/guards/indicator** - `2e919f48` (feat)

**Plan metadata:** (this summary commit)

## Files Created/Modified
- `Models/CDPDashboardViewModel.cs` - AssessmentReportItem.IsPassed bool→bool?
- `Models/ReportsDashboardViewModel.cs` - +GradedCount +PendingCount
- `Controllers/AssessmentAdminController.cs` - ComputeHistoryStats helper + drop ?? false + wire VM
- `Views/Admin/UserAssessmentHistory.cshtml` - 3-way badge + passRate guard + pending indicator

## Decisions Made
- D-07 averageScore exclude pending (locked); D-06 indicator placement = sub-line in Pass Rate card; Opsi A static helper for testability.

## Deviations from Plan

### Combined task commit (atomic ripple)
**1. [Build integrity] Tasks 1-3 committed together**
- **Found during:** Task 1 (VM bool→bool?)
- **Issue:** Type change at CDPDashboardViewModel.cs:111 breaks compilation (action projection L4744 + Razor view L172) until Task 2 (controller) AND Task 3 (view) land — a single nullable ripple. Per-task commits would leave 2 non-building intermediate commits.
- **Fix:** Made all 3 task edits, verified green build (0 errors), committed as one atomic unit.
- **Files modified:** all 4 plan files
- **Verification:** dotnet build -o .verifybin → 0 errors (lock-safe build, app dev running)
- **Committed in:** 2e919f48

---
**Total deviations:** 1 (commit granularity, no scope change)
**Impact on plan:** All planned edits delivered verbatim. Only commit boundary differs (atomic ripple). No scope creep.

## Issues Encountered
- Same `dotnet build` bin lock (MSB3027/3021) from running dev app (HcPortal PID 6864) — verified via `-o .verifybin` separate output (0 errors). Environmental, not code.

## User Setup Required
None.

## Next Phase Readiness
- `ComputeHistoryStats` static helper ready for xUnit (Plan 345-04 Task 1).
- 345-03 (PDF, same controller, region L4620-4622) clear to run — disjoint from this plan's L4712-4764 edits.

---
*Phase: 345-assessment-pending-grade-display-fix*
*Completed: 2026-06-04*
