---
phase: 38-auto-hide-filter
plan: 01
subsystem: api
tags: [csharp, aspnet, linq, ef-core, assessment, filtering]

# Dependency graph
requires: []
provides:
  - 7-day auto-hide filter on AssessmentSessions in Management branch (GetManageData)
  - 7-day auto-hide filter on AssessmentSessions in GetMonitorData
  - ExamWindowCloseDate ?? Schedule fallback for cutoff calculation
affects: [39-close-early, 40-history-tab]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Null-coalescing fallback in LINQ WHERE: (ExamWindowCloseDate ?? Schedule) >= sevenDaysAgo — translates to SQL COALESCE"
    - "sevenDaysAgo = DateTime.UtcNow.AddDays(-7) declared at top of branch, used in EF query before ToListAsync"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs

key-decisions:
  - "Filter uses ExamWindowCloseDate ?? Schedule — ExamWindowCloseDate is nullable, Schedule is non-nullable, so fallback is always safe"
  - "DateTime.UtcNow used (not DateTime.Now) — consistent with existing code and DB stores UTC"
  - "7-day condition in GetMonitorData is top-level AND wrapping the entire status OR block — preserves existing status filter logic exactly"
  - "No frontend changes, no migration, no model changes — pure WHERE-clause addition"

patterns-established:
  - "Auto-hide cutoff: declare var sevenDaysAgo = DateTime.UtcNow.AddDays(-7) before query, apply in .Where() before .ToListAsync()"

# Metrics
duration: 3min
completed: 2026-02-24
---

# Phase 38 Plan 01: Auto-Hide Filter Summary

**SQL-level 7-day cutoff added to both Management tab and Monitoring tab using `(ExamWindowCloseDate ?? Schedule) >= sevenDaysAgo` — assessment groups whose exam window closed more than 7 days ago no longer appear in either view.**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-02-24T00:00:00Z
- **Completed:** 2026-02-24
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments
- Management branch: `sevenDaysAgo` declared inside `if (view == "manage" && isHCAccess)` block; `managementQuery` initialized with `.Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)` before search filter and before `.ToListAsync()`
- GetMonitorData: `sevenDaysAgo` declared after existing `cutoff`; WHERE clause extended with 7-day condition as top-level AND, wrapping the entire status OR block
- Build: 0 errors, 36 warnings all pre-existing (no new warnings introduced)

## Task Commits

Each task was committed atomically:

1. **Task 1 + Task 2: Add 7-day cutoff to Management branch and GetMonitorData** - `d53369e` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified
- `Controllers/CMPController.cs` - Added `sevenDaysAgo` + WHERE filter in Management branch (lines 114, 118); added `sevenDaysAgo` + WHERE filter top-level AND in GetMonitorData (lines 289, 291-296)

## Decisions Made
- Both tasks modify the same file sequentially, committed together in one atomic commit
- Filter uses `DateTime.UtcNow` (not `DateTime.Now`) — consistent with existing `cutoff` declaration pattern
- `ExamWindowCloseDate ?? Schedule` null-coalescing is safe: `ExamWindowCloseDate` is nullable, `Schedule` is non-nullable on `AssessmentSession`

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Phase 38 complete. Both Management and Monitoring tabs now auto-hide stale assessment groups.
- Phase 39 (close-early) and Phase 40 (history tab) can be planned and executed independently.
- No blockers or concerns.

## Self-Check: PASSED

- [x] `Controllers/CMPController.cs` - FOUND
- [x] `.planning/phases/38-auto-hide-filter/38-01-SUMMARY.md` - FOUND
- [x] Commit `d53369e` - FOUND in git log

---
*Phase: 38-auto-hide-filter*
*Completed: 2026-02-24*
