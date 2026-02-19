---
phase: 10-unified-training-records
plan: 01
subsystem: ui
tags: [asp.net-core, razor, ef-core, viewmodel, linq]

# Dependency graph
requires:
  - phase: 09-gap-analysis-removal
    provides: clean baseline with no CompetencyGap dead code
provides:
  - UnifiedTrainingRecord flat ViewModel bridging AssessmentSession and TrainingRecord
  - GetUnifiedRecords() helper returning merged, date-sorted list for a given userId
  - Records() role branch: Coach/Coachee personal view; Admin/HC always get worker list
  - WorkerDetail() action using GetUnifiedRecords()
  - WorkerTrainingStatus extended with CompletedAssessments and CompletionDisplayText
  - GetWorkersInSection() batch GroupBy query for passed assessments
affects:
  - 10-02 (Razor views for Records and WorkerDetail — model type now List<UnifiedTrainingRecord>)
  - 11-assessment-filter (RecordsWorkerList row data now includes assessment counts)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Two-query in-memory merge: separate EF Core queries for AssessmentSessions and TrainingRecords, merged in memory with OrderByDescending + ThenBy
    - Batch GroupBy query: single query with IsPassed==true GroupBy UserId to avoid N+1 in GetWorkersInSection
    - isCoacheeView named bool: follows established isHCAccess pattern from CDPController

key-files:
  created:
    - Models/UnifiedTrainingRecord.cs
  modified:
    - Models/WorkerTrainingStatus.cs
    - Controllers/CMPController.cs

key-decisions:
  - "Admin always gets HC worker list regardless of SelectedView — SelectedView personal-records branch removed"
  - "Assessment Status column shows Passed/Failed derived from IsPassed, not literal Completed string"
  - "completedTrainings count uses Passed|Valid only — Permanent status removed per phase decision"
  - "GetPersonalTrainingRecords() kept as dead code (not called from Records or WorkerDetail); GetUnifiedRecords() replaces its role"

patterns-established:
  - "UnifiedTrainingRecord: flat ViewModel with SortPriority field for tie-break ordering across heterogeneous sources"
  - "isCoacheeView bool: userRole == UserRoles.Coach || userRole == UserRoles.Coachee — Admin explicitly excluded"

# Metrics
duration: 12min
completed: 2026-02-18
---

# Phase 10 Plan 01: Unified Training Records — Data Layer Summary

**UnifiedTrainingRecord ViewModel + GetUnifiedRecords() controller helper merging AssessmentSessions and TrainingRecords into a single date-sorted list, with Admin role branch fix and batch assessment count query for the HC worker list**

## Performance

- **Duration:** ~12 min
- **Started:** 2026-02-18
- **Completed:** 2026-02-18
- **Tasks:** 2
- **Files modified:** 3 (1 created, 2 modified)

## Accomplishments

- Created `UnifiedTrainingRecord` flat ViewModel with 10 fields + `IsExpired` computed property covering both AssessmentSession and TrainingRecord projection
- Added `GetUnifiedRecords()` private helper to CMPController that performs two EF Core queries then merges in-memory with `.OrderByDescending(r => r.Date).ThenBy(r => r.SortPriority)`
- Fixed Admin role branch in `Records()`: removed Admin SelectedView personal-records path; Admin now always routes to HC worker list regardless of SelectedView
- Extended `GetWorkersInSection()` with a single batch GroupBy query for passed assessments, eliminating the N+1 pattern that would have arisen when counting assessments per user
- Extended `WorkerTrainingStatus` with `CompletedAssessments` and `CompletionDisplayText` computed property

## Task Commits

Each task was committed atomically:

1. **Task 1: Create UnifiedTrainingRecord ViewModel** - `0c42d2f` (feat)
2. **Task 2: Extend WorkerTrainingStatus and rewrite CMPController data layer** - `3a9b584` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified

- `Models/UnifiedTrainingRecord.cs` — New flat ViewModel; fields: Date, RecordType, Title, Score, IsPassed, Penyelenggara, CertificateType, ValidUntil, Status, SortPriority; IsExpired computed property
- `Models/WorkerTrainingStatus.cs` — Added CompletedAssessments int field and CompletionDisplayText computed property; existing CompletedTrainings field reused
- `Controllers/CMPController.cs` — Added GetUnifiedRecords() helper; rewrote Records() role branch; updated WorkerDetail(); extended GetWorkersInSection() with batch assessment query; removed Permanent from training completion count

## Decisions Made

- Admin always gets the HC worker list regardless of SelectedView — phase decision from CONTEXT.md that the existing code had inverted
- Assessment Status column value is "Passed"/"Failed" derived from IsPassed, not the literal "Completed" string from AssessmentSession.Status
- `completedTrainings` in GetWorkersInSection now counts only `Status == "Passed" || Status == "Valid"` — "Permanent" was incorrectly included in the old code per research anti-patterns
- `GetPersonalTrainingRecords()` is retained as dead code rather than deleted — avoids scope risk if any other call site exists; Plan 02 is the safe cleanup point

## Deviations from Plan

**1. [Rule 1 - Bug] Removed duplicate CompletedTrainings property declaration**
- **Found during:** Task 2 (Step A — Extend WorkerTrainingStatus)
- **Issue:** The plan's Step A specified adding CompletedTrainings as a new field, but WorkerTrainingStatus already had a CompletedTrainings property at line 18. Adding a second declaration would cause a compile error.
- **Fix:** Added only CompletedAssessments (the genuinely new field) and CompletionDisplayText. The existing CompletedTrainings property at line 18 serves the same purpose and is populated by GetWorkersInSection() with the corrected count.
- **Files modified:** Models/WorkerTrainingStatus.cs
- **Verification:** Build passes with 0 errors
- **Committed in:** 3a9b584 (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 — duplicate property bug)
**Impact on plan:** Fix was essential for compilation. No functional change — outcome matches plan intent exactly.

## Issues Encountered

None — build passed cleanly after correcting the duplicate property.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plan 02 (Razor views) can now proceed: `List<UnifiedTrainingRecord>` is the model type for Records.cshtml and WorkerDetail.cshtml
- Records.cshtml `@model` must be updated from `List<TrainingRecord>` to `List<UnifiedTrainingRecord>` as first edit in Plan 02
- WorkerDetail.cshtml `@model` requires the same update
- RecordsWorkerList.cshtml can now render `worker.CompletionDisplayText` for the new combined count column

## Self-Check: PASSED

- FOUND: Models/UnifiedTrainingRecord.cs
- FOUND: commit 0c42d2f (feat(10-01): create UnifiedTrainingRecord ViewModel)
- FOUND: commit 3a9b584 (feat(10-01): extend WorkerTrainingStatus and rewrite CMPController data layer)
- FOUND: commit 2039490 (docs(10-01): complete unified-training-records data layer plan)
- Build: 0 errors, 0 warnings (all pre-existing)

---
*Phase: 10-unified-training-records*
*Completed: 2026-02-18*
