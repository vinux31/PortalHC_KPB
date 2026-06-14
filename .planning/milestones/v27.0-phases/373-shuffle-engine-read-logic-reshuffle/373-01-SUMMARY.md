---
phase: 373-shuffle-engine-read-logic-reshuffle
plan: 01
subsystem: engine
tags: [shuffle, pure-core, static-helper, fisher-yates, xunit, no-db]
requires:
  - phase: 372
    provides: ShuffleQuestions/ShuffleOptions flags (read by callers, not by core)
provides:
  - "Helpers/ShuffleEngine.cs — pure static core (no EF/DB/async): BuildQuestionAssignment + BuildOptionShuffle + Shuffle<T>"
  - "Canonical ON-path (CMPController per-ElemenTeknis Phase 2) moved verbatim into core"
  - "OFF-path: 1 paket q.Order; ≥2 paket round-robin index-stabil paket UTUH, filter paket kosong before modulo"
  - "ShuffleEngineTests.cs — 14 pure unit tests (SHUF-04/05/06/07/08), no DB"
affects: [373-02, 373-03]
tech-stack:
  added: []
  patterns: ["pure static helper (Helpers/ namespace, no EF imports) — unit-testable without DB", "filter-then-modulo for index-stable round-robin"]
key-files:
  created:
    - Helpers/ShuffleEngine.cs
    - HcPortal.Tests/ShuffleEngineTests.cs
  modified: []
key-decisions:
  - "Moved the CANONICAL BuildCrossPackageAssignment from CMPController (per-ET Phase 2, basePerET) — NOT the divergent AssessmentAdminController copy (per-package baseCount). Preserves SC#1 ON behavior verbatim."
  - "OFF≥2 returns the FULL chosen package (D-05), not K-min truncation; rng ignored on OFF paths (deterministic)."
  - "Purity guard: no EntityFrameworkCore/ApplicationDbContext/async — comment reworded to 'fully synchronous' so the acceptance grep stays 0."
patterns-established:
  - "Helpers/ShuffleEngine.cs as single source of truth — controllers will delegate (Plan 02/03) and delete their local copies."
requirements-completed: [SHUF-04, SHUF-05, SHUF-06, SHUF-07, SHUF-08]
duration: ~20min
completed: 2026-06-13
---

# Phase 373 Plan 01: Pure ShuffleEngine Core Summary

**Extracted `Helpers/ShuffleEngine.cs` — a pure (no EF/DB/async) static core hosting the canonical ON-path (moved verbatim from CMPController's per-ElemenTeknis `BuildCrossPackageAssignment`), the new OFF-path (1 paket `q.Order`; ≥2 paket round-robin index-stabil full package, empty-package guarded before modulo), and per-question option shuffle — proven by 14 DB-free unit tests.**

## Performance
- **Duration:** ~20 min
- **Tasks:** 2/2 (interactive inline)
- **Files created:** 2

## Accomplishments
- `ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions, workerIndex, rng)`: ON → `BuildCrossPackageAssignment` (canonical, verbatim); OFF 1 paket → `q.Order`; OFF ≥2 → `packagesWithQuestions[workerIndex % count]` full package in `q.Order`, empty packages excluded **before** modulo (no DivideByZero).
- `BuildOptionShuffle(questions, shuffleOptions, rng)`: ON → dict per question; OFF → empty dict (caller serializes `"{}"`). Independent of question flag.
- `Shuffle<T>` Fisher-Yates hosted in the core (public).
- 14 pure unit tests (no DB/fixture/Trait): OFF order, workerIndex→package `[Theory]`, empty-package-excluded, all-empty no-throw, append-no-shift, ON seed-stable single + multi K-min, options ON/OFF, independence, determinism-twice.

## Task Commits
1. **Task 1: pure ShuffleEngine core** — `c7c975ef` (feat)
2. **Task 2: 14 unit tests** — `dda188f5` (test)

## Files Created
- `Helpers/ShuffleEngine.cs` — 4 public/private static members, pure
- `HcPortal.Tests/ShuffleEngineTests.cs` — 14 green ([Theory]+[Fact])

## Decisions Made
- Canonical = CMPController version (per-ET divisor). The divergent AssessmentAdminController copy is ignored here and deleted in Plan 03.
- Reworded a docstring ("no async" → "fully synchronous") so the purity acceptance grep returns 0 without weakening the comment's intent.

## Deviations from Plan
None — plan executed as written. (Removed one meaningless `Assert.DoesNotContain(idx1, id => false)` sanity line during test cleanup; replaced with `Assert.NotEmpty`.)

## Issues Encountered
- Purity acceptance grep matched the literal word "async" in a docstring; reworded. Code was already pure.

## Verification
- `dotnet build` → Build succeeded, 0 errors.
- `dotnet test --filter "FullyQualifiedName~ShuffleEngine"` → Passed! 14/14, 0 failures (~50ms, no DB).
- `rg "EntityFrameworkCore|ApplicationDbContext|async" Helpers/ShuffleEngine.cs` → 0.
- `rg "basePerET" Helpers/ShuffleEngine.cs` → 3 (canonical per-ET Phase 2 present).

## Next Phase Readiness
- Core ready. Plan 02 (StartExam wiring) + Plan 03 (reshuffle wiring) both delegate to `ShuffleEngine` and delete their local `BuildCrossPackageAssignment` + `Shuffle<T>` copies. Temporary duplication exists until then (build green).

---
*Phase: 373-shuffle-engine-read-logic-reshuffle*
*Completed: 2026-06-13*
