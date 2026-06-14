---
phase: 373-shuffle-engine-read-logic-reshuffle
plan: 02
subsystem: api
tags: [cmpcontroller, startexam, shuffle, flag-gate, worker-index, dedup]
requires:
  - phase: 373-01
    provides: ShuffleEngine static core (BuildQuestionAssignment / BuildOptionShuffle / Shuffle<T>)
  - phase: 372
    provides: assessment.ShuffleQuestions / ShuffleOptions flags (propagated onto each session)
provides:
  - "StartExam builds UserPackageAssignment via ShuffleEngine, gated on both flags"
  - "Stable worker-index (siblingSessionIds.OrderBy(x=>x).IndexOf(id)) for OFF≥2 round-robin"
  - "Stale comment :1054 fixed (SHUF-15); local BuildCrossPackageAssignment + Shuffle<T> deleted (dedup)"
affects: [373-03, 374, 375]
tech-stack:
  added: []
  patterns: ["controller delegates pure distribution to ShuffleEngine; EF query + flag-read stay in controller"]
key-files:
  modified:
    - Controllers/CMPController.cs
key-decisions:
  - "Worker-index computed in-memory after .ToListAsync() via OrderBy(x=>x) — SQL Server has no guaranteed order without ORDER BY (Pitfall 2). Same sibling predicate (Title+Category+Schedule.Date, no status filter) as reshuffle for cross-call determinism (OQ#1)."
  - "Option dict built only over assignedQuestions (shuffledIds.Contains) — extra keys would be harmless but this is cleaner (RESEARCH A4)."
  - "Sentinel/persist/race-guard/stale-count guard preserved verbatim (D-03); VM opts + ViewBag.OptionShuffle untouched (Pitfall 4)."
patterns-established:
  - "ShuffleEngine is the single source — CMPController no longer hosts its own distribution copy."
requirements-completed: [SHUF-04, SHUF-05, SHUF-06, SHUF-07, SHUF-08, SHUF-15]
duration: ~20min
completed: 2026-06-13
---

# Phase 373 Plan 02: StartExam Wiring Summary

**`CMPController.StartExam` now builds `UserPackageAssignment` through the shared `ShuffleEngine`, gated on `assessment.ShuffleQuestions` / `assessment.ShuffleOptions`, with a stable worker-index (`siblingSessionIds.OrderBy(x=>x).IndexOf(id)`) for OFF≥2 round-robin; stale comment :1054 fixed and the local `BuildCrossPackageAssignment` + `Shuffle<T>` duplicates deleted (−147 net lines).**

## Performance
- **Duration:** ~20 min
- **Tasks:** 2/2 (interactive inline)
- **Files modified:** 1 (CMPController.cs)

## Accomplishments
- Build branch (`if (assignment == null)`) delegates: `ShuffleEngine.BuildQuestionAssignment(packages, assessment.ShuffleQuestions, workerIndex, rng)` + `ShuffleEngine.BuildOptionShuffle(assignedQuestions, assessment.ShuffleOptions, rng)`.
- Stable worker-index added right after the sibling query (Pitfall 2 fix).
- ON path unchanged (SC#1 — canonical core). OFF 1 paket urut / OFF ≥2 1 paket utuh per worker. OFF options → `"{}"` → view DB-order fallback.
- SHUF-15: stale comment replaced. Local `BuildCrossPackageAssignment` + `Shuffle<T>` removed (now in core).
- Preserved verbatim: sentinel, `SavedQuestionCount = shuffledIds.Count`, `catch (DbUpdateException)` race-guard, stale-count guard (D-03), VM opts + ViewBag.OptionShuffle (Pitfall 4), ownership/auth check.

## Task Commits
1. **Task 1 + Task 2 (wire + cleanup, one file)** — `b5a150a3` (feat) — 19 insertions, 166 deletions.

## Files Modified
- `Controllers/CMPController.cs` — StartExam build branch + worker-index + comment + 2 method deletions.

## Decisions Made
- Single commit for both tasks (same file, interdependent: Task 2 deletes methods Task 1 made unused).

## Deviations from Plan
None — plan executed as written.

## Issues Encountered
None.

## Verification
- All acceptance greps pass: BuildQuestionAssignment=1, BuildOptionShuffle=1, ShuffleQuestions/Options=1 each, OrderBy(x=>x)=1, SavedQuestionCount preserved, catch(DbUpdateException) preserved, "option shuffle removed"=0, local BuildCrossPackageAssignment=0, local Shuffle<T>=0, new comment=1.
- `dotnet build` → Build succeeded, 0 errors.
- `dotnet test --filter ~ShuffleEngine` → 14/14 green (no regression after wiring).
- Manual smoke (`dotnet run` @5277) = Phase 375 scope; per CLAUDE.md Develop Workflow before push.

## Next Phase Readiness
- StartExam read-path wired. Plan 03 wires the reshuffle endpoints (AssessmentAdminController) to the same core + fixes the `"{}"` bug + deletes the divergent duplicate.

---
*Phase: 373-shuffle-engine-read-logic-reshuffle*
*Completed: 2026-06-13*
