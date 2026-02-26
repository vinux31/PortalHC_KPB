---
phase: 46-attempt-history
plan: "02"
subsystem: ui
tags: [aspnet, cshtml, bootstrap, csharp, history, assessment, attempt-tracking]

# Dependency graph
requires:
  - phase: 46-attempt-history/46-01
    provides: AssessmentAttemptHistory model + table + archival in ResetAssessment
provides:
  - AllWorkersHistoryRow extended with AttemptNumber property
  - RecordsWorkerListViewModel split into AssessmentHistory + TrainingHistory + AssessmentTitles
  - GetAllWorkersHistory() returns (assessment, training) tuple with batch Attempt # computation
  - History tab at /CMP/Records replaced with two Bootstrap sub-tabs (Riwayat Assessment + Riwayat Training)
  - Client-side worker/NIP search + assessment title dropdown filter on Riwayat Assessment table
affects: [CMP Records view, HC/Admin history tab, future attempt-history phases]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Tuple return from private async helper to split unified list into two typed lists"
    - "Batch count archived rows (GroupBy) then ToDictionary lookup to compute Attempt # for current session — avoids N+1"
    - "Bootstrap nested nav-tabs inside outer tab-pane for sub-tab UX"
    - "Client-side data-* attributes on tr elements for JS filter without round-trip"

key-files:
  created: []
  modified:
    - Models/AllWorkersHistoryRow.cs
    - Models/RecordsWorkerListViewModel.cs
    - Controllers/CMPController.cs
    - Views/CMP/RecordsWorkerList.cshtml

key-decisions:
  - "GetAllWorkersHistory() returns tuple (assessment, training) — caller destructures inline for clean ViewModel init"
  - "Riwayat Assessment is the default active sub-tab (show active) since it's the new HIST-02/HIST-03 feature"
  - "Assessment rows sorted by Title then Date descending — grouped-by-title view matches plan spec"
  - "Current completed session Attempt # = archived count for (UserId, Title) + 1 — consistent with Plan 01 archival logic"
  - "Training sub-tab retains same column set as Phase 40 (no Score/IsPassed shown for training rows)"

patterns-established:
  - "Nested Bootstrap sub-tabs: outer tab-pane contains ul.nav.nav-tabs + div.tab-content — used for History sub-tabs"
  - "Client-side filter: data-worker + data-title on tr, filterAssessmentRows() reads both inputs and sets row.style.display"

requirements-completed: [HIST-02, HIST-03]

# Metrics
duration: 15min
completed: 2026-02-26
---

# Phase 46 Plan 02: Attempt History Summary

**History tab at /CMP/Records split into Riwayat Assessment + Riwayat Training sub-tabs, with unified assessment query (archived + current completed) and per-worker per-title Attempt # sequencing**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-02-26T02:00:00Z
- **Completed:** 2026-02-26T02:15:00Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- Unified assessment history view surfaces both archived AttemptHistory rows (with stored Attempt #) and current Completed sessions (Attempt # = archived count + 1), all in one table
- Client-side combinable filters — worker name/NIP text search + assessment title dropdown — filter without page reload
- Riwayat Training sub-tab preserves all Phase 40 training data unchanged; outer History tab badge reflects combined count
- Build passes with 0 errors after both tasks

## Task Commits

Each task was committed atomically:

1. **Task 1: Extend models + rewrite GetAllWorkersHistory()** - `fac49e3` (feat)
2. **Task 2: Rewrite History tab with two sub-tabs** - `61a9141` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified
- `Models/AllWorkersHistoryRow.cs` - Added `AttemptNumber` nullable int property
- `Models/RecordsWorkerListViewModel.cs` - Replaced `History` list with `AssessmentHistory`, `TrainingHistory`, `AssessmentTitles`
- `Controllers/CMPController.cs` - GetAllWorkersHistory() now returns tuple; Records action destructures and populates new ViewModel; batch Attempt # computation via GroupBy + ToDictionary
- `Views/CMP/RecordsWorkerList.cshtml` - History tab badge updated; unified history pane replaced with nested sub-tabs; filterAssessmentRows() JS function added

## Decisions Made
- GetAllWorkersHistory() returns `(List<AllWorkersHistoryRow> assessment, List<AllWorkersHistoryRow> training)` tuple — the two lists have completely different sort orders (title+date vs date-only) and columns, so a single list with a flag would be messier
- Current session Attempt # = archived count for that (UserId, Title) pair + 1, which mirrors the Plan 01 archival logic where the archived row stores `count + 1` at the time of reset
- Batch GroupBy/ToDictionary for archived counts computed once and reused per session row — avoids N+1 against AssessmentAttemptHistory
- Riwayat Assessment is default active sub-tab (show active classes) since it is the new HIST-02/HIST-03 feature and more relevant to HC/Admin workflow

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None — build succeeded cleanly after Task 2. Task 1 left 3 expected view errors (Model.History references) which were resolved by Task 2.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- HIST-01 (archival): live from Plan 01
- HIST-02 (unified query with Attempt #): complete — this plan
- HIST-03 (columns, filters, sub-tab structure): complete — this plan
- All three Phase 46 requirements satisfied; v2.2 Attempt History milestone is complete

---
*Phase: 46-attempt-history*
*Completed: 2026-02-26*
