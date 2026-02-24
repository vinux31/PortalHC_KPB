---
phase: 40-training-records-history-tab
plan: 01
subsystem: api
tags: [csharp, mvc, viewmodel, training-records, assessment]

# Dependency graph
requires:
  - phase: 39-close-early
    provides: CMPController foundation, AssessmentSession model with Status/Score/IsPassed fields
provides:
  - AllWorkersHistoryRow model class (flat projection for one history event across all workers)
  - RecordsWorkerListViewModel wrapper combining Workers + History sub-lists
  - GetAllWorkersHistory() helper method in CMPController
  - Records() action updated to pass RecordsWorkerListViewModel to RecordsWorkerList view
affects: [40-02-frontend-history-tab, RecordsWorkerList.cshtml]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Wrapper ViewModel pattern (mirrors CDPDashboardViewModel) — Records() returns typed wrapper instead of bare list"
    - "In-memory merge pattern — GetAllWorkersHistory() loads both tables, merges to flat list, sorts by Date descending"

key-files:
  created:
    - Models/AllWorkersHistoryRow.cs
    - Models/RecordsWorkerListViewModel.cs
  modified:
    - Controllers/CMPController.cs

key-decisions:
  - "Single return point in Records() — if/else sets workers variable, single return at bottom wraps in RecordsWorkerListViewModel; plan anticipated two return sites but original code already had one consolidated return"
  - "History always fully loaded regardless of filter state — no filtering on History tab in Phase 40"
  - "TanggalMulai ?? Tanggal coalesce for training rows — TanggalMulai is nullable"
  - "GetAllWorkersHistory uses Include(User) nav property — FullName ?? UserId fallback for display"

patterns-established:
  - "AllWorkersHistoryRow.RecordType: 'Manual' for training records, 'Assessment Online' for assessment sessions"

# Metrics
duration: 8min
completed: 2026-02-24
---

# Phase 40 Plan 01: Backend Data Pipeline for All-Workers History Tab Summary

**AllWorkersHistoryRow + RecordsWorkerListViewModel models and GetAllWorkersHistory() CMPController helper providing merged, date-sorted training history across all workers for the History tab**

## Performance

- **Duration:** ~8 min
- **Started:** 2026-02-24T00:00:00Z
- **Completed:** 2026-02-24T00:08:00Z
- **Tasks:** 2 (Tasks 1 and 2; Task 3 is human-verify checkpoint)
- **Files modified:** 3

## Accomplishments

- Created `AllWorkersHistoryRow.cs` — flat projection model combining TrainingRecord and AssessmentSession fields for cross-worker history display
- Created `RecordsWorkerListViewModel.cs` — wrapper ViewModel with Workers (existing tab) and History (new tab) sub-lists, mirroring CDPDashboardViewModel pattern
- Added `GetAllWorkersHistory()` private async helper in CMPController — queries all completed AssessmentSessions + all TrainingRecords with User nav property, merges into sorted AllWorkersHistoryRow list
- Updated `Records()` action to return `RecordsWorkerListViewModel` instead of bare `List<WorkerTrainingStatus>` — Plan 02 can now consume `Model.Workers` and `Model.History`

## Task Commits

Each task was committed atomically:

1. **Task 1: Create AllWorkersHistoryRow and RecordsWorkerListViewModel** - `1e3ae82` (feat)
2. **Task 2: Add GetAllWorkersHistory() and update Records() return points in CMPController** - `b19172a` (feat)

## Files Created/Modified

- `Models/AllWorkersHistoryRow.cs` - Flat projection for one training event across all workers (WorkerName, NIP, RecordType, Title, Date, Penyelenggara, Score, IsPassed)
- `Models/RecordsWorkerListViewModel.cs` - Wrapper ViewModel with `List<WorkerTrainingStatus> Workers` and `List<AllWorkersHistoryRow> History`
- `Controllers/CMPController.cs` - Added GetAllWorkersHistory() helper after GetUnifiedRecords(); Records() return updated to wrap RecordsWorkerListViewModel

## Decisions Made

- **Single return point in Records():** The plan anticipated two `return View("RecordsWorkerList", ...)` sites based on plan analysis, but the actual original code had a single consolidated return after the if/else block that sets `workers`. Only one replacement was needed — this is correct and cleaner.
- **History always fully loaded:** GetAllWorkersHistory() is called on every Records() request regardless of filter state. History tab has no filters in Phase 40.
- **TanggalMulai coalesce:** `t.TanggalMulai ?? t.Tanggal` used for training row Date — TanggalMulai is nullable in TrainingRecord model.

## Deviations from Plan

None - plan executed exactly as written. The only variance was that Records() had one return site (not two as estimated in the plan), but the resulting code is correct.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Controller is ready. `RecordsWorkerListViewModel` flows from `Records()` to `RecordsWorkerList.cshtml` with both `Model.Workers` and `Model.History` populated.
- Plan 02 can now update `RecordsWorkerList.cshtml` to add the History tab using `Model.History`.
- At this stage, navigating to `/CMP/Records` will cause a Razor compile error because RecordsWorkerList.cshtml still declares `@model List<WorkerTrainingStatus>` — this is expected and will be fixed in Plan 02.

## Self-Check

**Files created:**
- `Models/AllWorkersHistoryRow.cs` — FOUND
- `Models/RecordsWorkerListViewModel.cs` — FOUND

**Commits:**
- `1e3ae82` feat(40-01): add AllWorkersHistoryRow and RecordsWorkerListViewModel — FOUND
- `b19172a` feat(40-01): add GetAllWorkersHistory helper and update Records() return — FOUND

**Build:** 0 errors (36 pre-existing warnings from CDPController and CMPController — unrelated to this plan)

**grep verification:**
- `GetAllWorkersHistory` appears 2 times in CMPController.cs (1 definition + 1 call site)
- No bare `return View("RecordsWorkerList", workers)` remains in Records()

## Self-Check: PASSED

---
*Phase: 40-training-records-history-tab*
*Completed: 2026-02-24*
