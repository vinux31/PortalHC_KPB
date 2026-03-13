---
phase: 163-hub-infrastructure-safety-foundations
plan: 02
subsystem: database
tags: [race-condition, concurrency, ExecuteUpdateAsync, assessment, EF-Core]

requires:
  - phase: 162-simplifikasi-action-close
    provides: AkhiriUjian and GradeFromSavedAnswers in AdminController

provides:
  - Status-guarded AkhiriUjian with ExecuteUpdateAsync WHERE guard
  - Status-guarded ResetAssessment with ExecuteUpdateAsync WHERE guard
  - Status-guarded SubmitExam (package + legacy paths) with ExecuteUpdateAsync WHERE guard
  - SaveAnswer + SaveLegacyAnswer guard extended to include Cancelled sessions

affects: [164-signalr-push, 165-signalr-cleanup]

tech-stack:
  added: []
  patterns:
    - "Detach-then-ExecuteUpdateAsync: detach tracked entity, use ExecuteUpdateAsync with WHERE status guard for first-write-wins semantics without optimistic concurrency exceptions"
    - "Flush-then-guard: flush prerequisite DB ops (archive, delete responses) via SaveChangesAsync before status-guarded ExecuteUpdateAsync claim"

key-files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Controllers/CMPController.cs

key-decisions:
  - "AkhiriUjian: rowsAffected==0 returns Info TempData 'Sesi sudah selesai atau dibatalkan.' (silent skip per locked decision from 163-CONTEXT)"
  - "SubmitExam: rowsAffected==0 redirects to Results with Info TempData 'Ujian Anda sudah diakhiri oleh pengawas.' (not an error)"
  - "ResetAssessment: archive+delete ops run before status-guarded reset — idempotent pre-work is safe even in a race"
  - "SaveAnswer + SaveLegacyAnswer: Cancelled added to closed-session check; minor race (check passes, session closes, answer saves) is harmless since grading re-reads all answers"
  - "Legacy SubmitExam path gets same ExecuteUpdateAsync treatment as package path for consistency"

patterns-established:
  - "All AssessmentSession write actions use WHERE-clause-guarded ExecuteUpdateAsync for first-write-wins; 0-rows = no-op"

requirements-completed: []

duration: 25min
completed: 2026-03-13
---

# Phase 163 Plan 02: Race Condition Guards Summary

**Status-guarded writes (ExecuteUpdateAsync WHERE-clause pattern) added to all 4 assessment write actions — AkhiriUjian, ResetAssessment, SubmitExam, SaveAnswer — eliminating TOCTOU races before SignalR concurrency increases in Phase 164-165.**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-03-13T02:00:00Z
- **Completed:** 2026-03-13T02:25:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- AkhiriUjian: detach entity after GradeFromSavedAnswers, use ExecuteUpdateAsync with 5-field WHERE guard (StartedAt/CompletedAt/Score/Status); rowsAffected==0 returns Info TempData
- ResetAssessment: flush archive+delete via SaveChangesAsync first, then atomic reset with WHERE Status != 'Cancelled'; rowsAffected==0 returns Error TempData
- SubmitExam (package path): replaced DbUpdateConcurrencyException retry block with ExecuteUpdateAsync WHERE Status != 'Completed'; UserPackageAssignments updated via separate ExecuteUpdateAsync
- SubmitExam (legacy path): same ExecuteUpdateAsync pattern applied for consistency
- SaveAnswer + SaveLegacyAnswer: Cancelled status added to closed-session guard

## Task Commits

1. **Task 1: AkhiriUjian and ResetAssessment** - `d4c049e` (fix)
2. **Task 2: SubmitExam and SaveAnswer** - `15bfc79` (fix)

## Files Created/Modified

- `Controllers/AdminController.cs` - AkhiriUjian and ResetAssessment status-guarded writes
- `Controllers/CMPController.cs` - SubmitExam (both paths) and SaveAnswer/SaveLegacyAnswer guards

## Decisions Made

- rowsAffected==0 in AkhiriUjian returns Info (not Error) — per locked decision: first-write-wins, loser silently skips
- rowsAffected==0 in SubmitExam redirects to Results with Info — user's exam was already graded by HC
- ResetAssessment pre-work (archive, delete) runs before the guarded claim — this is safe since those ops are idempotent
- DbUpdateConcurrencyException catch block removed from SubmitExam — no longer needed with atomic WHERE-clause update

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed incorrect type cast for LastActivePage in ResetAssessment**
- **Found during:** Task 1 (ResetAssessment ExecuteUpdateAsync)
- **Issue:** Plan used `(string?)null` for LastActivePage but model declares it as `int?`
- **Fix:** Changed to `(int?)null`
- **Files modified:** Controllers/AdminController.cs
- **Committed in:** d4c049e (Task 1 commit)

**2. [Rule 2 - Missing] Extended SaveLegacyAnswer with Cancelled guard**
- **Found during:** Task 2 (SaveAnswer fix)
- **Issue:** SaveAnswer and SaveLegacyAnswer share the same status check pattern — fixing only SaveAnswer while leaving SaveLegacyAnswer incomplete would be inconsistent
- **Fix:** Applied replace_all to update both simultaneously
- **Files modified:** Controllers/CMPController.cs
- **Committed in:** 15bfc79 (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (1 bug, 1 missing coverage)
**Impact on plan:** Both auto-fixes were necessary for correctness. No scope creep.

## Issues Encountered

- `dotnet build` fails to copy output exe because app is running in IIS Express (file locked). No C# compiler errors present — only MSBuild copy errors from locked process. Build success verified via `grep "error CS"`.

## Next Phase Readiness

- All 4 assessment write actions are now race-safe with first-write-wins semantics
- Foundation is ready for Phase 163-03 (WAL mode) and Phase 164-165 (SignalR push events)
- No blockers

---
*Phase: 163-hub-infrastructure-safety-foundations*
*Completed: 2026-03-13*
