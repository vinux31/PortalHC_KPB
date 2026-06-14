---
phase: 373-shuffle-engine-read-logic-reshuffle
plan: 03
subsystem: api
tags: [assessmentadmincontroller, reshuffle, shuffle, bugfix, dedup, xunit]
requires:
  - phase: 373-01
    provides: ShuffleEngine core (BuildQuestionAssignment / BuildOptionShuffle)
provides:
  - "ReshufflePackage + ReshuffleAll delegate to ShuffleEngine, respecting both flags"
  - "Hard-coded ShuffledOptionIdsPerQuestion = \"{}\" bug fixed at both sites (SHUF-09)"
  - "Divergent local BuildCrossPackageAssignment (per-package) + local Shuffle<T> deleted"
  - "ShuffleReshuffleTests.cs — regression: optDict != \"{}\" when ShuffleOptions ON"
affects: [374, 375]
tech-stack:
  added: []
  patterns: ["reshuffle delegates pure distribution + option-shuffle to ShuffleEngine; auth/guard/audit unchanged"]
key-files:
  modified:
    - Controllers/AssessmentAdminController.cs
  created:
    - HcPortal.Tests/ShuffleReshuffleTests.cs
key-decisions:
  - "Deleting the divergent (per-package Phase 2) copy means reshuffle ON≥2 now follows the CANONICAL CMPController per-ElemenTeknis algorithm — intended dedup (reshuffle↔StartExam consistency), NOT a no-op for ON≥2 ordering (Pitfall 1, by design)."
  - "OQ#1 parity CONFIRMED: ReshufflePackage siblingSessionIds keys on Title+Category+Schedule.Date (same as StartExam); ReshuffleAll derives siblingSessionIds from `sessions` queried by the same predicate. Worker index matches StartExam → no package shift on reshuffle."
  - "Wave-0 regression at engine boundary (pure, fast) proves the '{}' bug cannot recur; full reshuffle mode-matrix + Playwright = Phase 375."
patterns-established:
  - "AssessmentAdminController no longer hosts a shuffle algorithm copy — ShuffleEngine is the sole source."
requirements-completed: [SHUF-09]
duration: ~20min
completed: 2026-06-13
---

# Phase 373 Plan 03: Reshuffle Wiring + Bug Fix Summary

**`ReshufflePackage` + `ReshuffleAll` now delegate to the shared `ShuffleEngine`, honoring both `ShuffleQuestions` and `ShuffleOptions`; the long-standing hard-coded `ShuffledOptionIdsPerQuestion = "{}"` bug is fixed at both sites (reshuffled participants finally get shuffled options when ON); the divergent per-package `BuildCrossPackageAssignment` + local `Shuffle<T>` are deleted (−140 net lines), with a pure regression test closing the bug.**

## Performance
- **Duration:** ~20 min
- **Tasks:** 3/3 (interactive inline)
- **Files:** 1 modified + 1 created

## Accomplishments
- Both reshuffle endpoints: `ShuffleEngine.BuildQuestionAssignment(packages, flag, workerIndex, rng)` + `ShuffleEngine.BuildOptionShuffle(assignedQuestions, flag, rng)` → `JsonSerializer.Serialize(optDict)` replaces the hard-coded `"{}"`.
- Stable worker-index from `siblingSessionIds.OrderBy(x=>x).IndexOf(...)`; ReshuffleAll sorts once before the loop.
- **OQ#1 parity confirmed:** both reshuffle sibling sets key on `Title+Category+Schedule.Date` — identical to StartExam → worker keeps the same package across reshuffle/resume.
- Deleted the divergent `BuildCrossPackageAssignment` (per-package `baseCount = remaining / N` + redistribution) and local `Shuffle<T>`; the `#region Helper Methods` is now a one-line pointer comment to the core.
- Preserved verbatim: `[Authorize(Roles="Admin, HC")]`, `[ValidateAntiForgeryToken]`, `userStatus != "Not started"/"Abandoned"` guards, audit-log, sentinel.
- `ShuffleReshuffleTests.cs`: ON → serialize ≠ `"{}"`; OFF → exactly `"{}"`.

## Task Commits
1. **Task 1 + Task 2 (wire both endpoints + fix "{}" + delete divergent dup)** — `cdc1cc8a` (feat) — 21 insertions, 161 deletions.
2. **Task 3 (regression test)** — `<test commit>` (test) — ShuffleReshuffleTests.cs.

## Files
- `Controllers/AssessmentAdminController.cs` — 2 endpoints wired, "{}" fixed ×2, 2 methods deleted.
- `HcPortal.Tests/ShuffleReshuffleTests.cs` — 2 [Fact] regression.

## Decisions Made
- Combined Task 1+2 into one feat commit (same file, interdependent). Task 3 separate test commit.
- Reworded the dedup pointer comment to avoid the literal `ShuffleEngine.BuildQuestionAssignment` token so the call-site grep count stays exactly 2.

## Deviations from Plan
None — plan executed as written.

## Issues Encountered
- Dedup pointer comment initially tripped the "2 call-sites" acceptance grep (counted a comment occurrence); reworded.

## Verification
- Acceptance greps: `"{}"`=0, BuildQuestionAssignment=2, BuildOptionShuffle=2, serialize optDict=2, Authorize present, "Not started" guard=2, divergent method=0, local Shuffle<T>=0, regression asserts present.
- `dotnet build` → Build succeeded, 0 errors.
- `dotnet test --filter ~Shuffle` → **23/23 green** (14 ShuffleEngine + 2 ShuffleReshuffle + 7 Phase 372 real-SQL — no regression).
- Manual smoke (HC reshuffle ON assessment → non-empty dict in DB) = Phase 375 scope per CLAUDE.md.

## Next Phase Readiness
- Phase 373 engine complete: pure core + StartExam wiring + reshuffle wiring + bug fix + cleanup. Phase 374 (ManagePackages UI/lock/warning/reminder) builds on this. Full mode-matrix + Playwright UAT = Phase 375.

---
*Phase: 373-shuffle-engine-read-logic-reshuffle*
*Completed: 2026-06-13*
