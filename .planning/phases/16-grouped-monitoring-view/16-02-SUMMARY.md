---
phase: 16-grouped-monitoring-view
plan: "02"
subsystem: ui
tags: [csharp, asp-net-core, razor, monitoring, bootstrap, progress-bar]

# Dependency graph
requires:
  - phase: 16-grouped-monitoring-view
    plan: "01"
    provides: "MonitoringGroupViewModel shape in ViewBag.MonitorData"
provides:
  - "Grouped monitoring tab in Assessment.cshtml — one row per assessment group with progress bar, pass rate, and View Details link"
affects:
  - 16-03-plan (detail view — already committed as 631b1d6)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Bootstrap progress bar with dynamic width from (CompletedCount/TotalCount)*100 calculation"
    - "Pass rate color threshold: text-success if >=70%, text-danger otherwise"
    - "Em-dash fallback for pass rate when CompletedCount == 0"

key-files:
  created: []
  modified:
    - Views/CMP/Assessment.cshtml

key-decisions:
  - "70% pass rate threshold for green/red display — matches default PassPercentage config; display heuristic only, not enforced per group"
  - "Category cast to (string) for switch expression — group.Category is a string property but Razor requires explicit cast in switch context"

patterns-established:
  - "MonitoringGroupViewModel consumed directly from ViewBag.MonitorData — no re-shaping in view"

# Metrics
duration: 3min
completed: 2026-02-19
---

# Phase 16 Plan 02: Grouped Monitoring Tab View Summary

**Monitoring tab in Assessment.cshtml replaced: flat per-user table replaced with grouped summary table using Bootstrap progress bars, pass rate indicators, and View Details links per assessment group**

## Performance

- **Duration:** 3 min
- **Started:** 2026-02-19T13:05:11Z
- **Completed:** 2026-02-19T13:07:57Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments

- Changed `ViewBag.MonitorData` cast from `List<AssessmentSession>` to `List<MonitoringGroupViewModel>` (variable renamed `monitorData` → `monitorGroups`)
- Updated Monitoring tab badge count to use `monitorGroups.Count`
- Replaced flat per-user table (Title, Category, Status, Schedule, Assigned To, Duration columns) with grouped summary table (Assessment, Schedule, Status, Completion, Pass Rate, View Details columns)
- Each group row renders a Bootstrap progress bar (8px height, green at 100% else blue) labeled `X/Y`
- Pass rate column: green `text-success` if ≥70%, red `text-danger` otherwise; em-dash `—` when `CompletedCount == 0`
- View Details button links to `AssessmentMonitoringDetail` with `title`, `category`, `scheduleDate` route values
- Empty state updated: "No active, upcoming, or recently closed assessments" with "Closed assessments older than 30 days are not shown."

## Task Commits

Each task was committed atomically:

1. **Task 1: Replace flat monitor table with grouped summary table** - `93596ee` (feat)

**Plan metadata:** (docs commit — see below)

## Files Created/Modified

- `Views/CMP/Assessment.cshtml` — Three targeted changes: (1) cast updated to `MonitoringGroupViewModel`, (2) badge count updated to `monitorGroups.Count`, (3) entire monitor tab pane body replaced with grouped table

## Decisions Made

- 70% pass rate threshold for green/red color — matches default `PassPercentage` config value; this is a display heuristic only (not enforced against each group's actual config) as noted in the plan
- Used `(string)group.Category` cast in the switch expression — Razor requires explicit cast for dynamic-typed properties in switch expressions even when property is declared as `string` on the ViewModel

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

The app server was running (HcPortal.exe file lock) during `dotnet build`, causing MSB3026/MSB3027 retry warnings and an MSB3021 file-copy error for the output binary. Compilation itself completed with 0 CS/RZ errors — confirmed by checking for `error CS` or `error RZ` patterns in build output (none found). This is the same file-lock behavior noted in the 16-01 SUMMARY and is a non-compilation artifact of the dev server running.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Monitoring tab now shows grouped assessment rows — ready for browser verification
- `AssessmentMonitoringDetail.cshtml` (Plan 03) was already committed at `631b1d6` before this plan's docs commit
- Phase 16 is complete pending Plan 03 SUMMARY creation (if not yet done)

## Self-Check

- [x] `Views/CMP/Assessment.cshtml` modified — confirmed (72 insertions, 29 deletions in commit `93596ee`)
- [x] Commit `93596ee` exists — confirmed via `git log --oneline -3`
- [x] `monitorGroups` variable name used consistently throughout monitoring tab pane
- [x] `dotnet build` — 0 C#/Razor compilation errors

## Self-Check: PASSED

---
*Phase: 16-grouped-monitoring-view*
*Completed: 2026-02-19*
