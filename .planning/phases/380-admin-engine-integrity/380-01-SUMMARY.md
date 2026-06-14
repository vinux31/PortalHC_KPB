---
phase: 380-admin-engine-integrity
plan: 01
subsystem: assessment-engine
tags: [assessment, shuffle, engine, exam-entry]
requires: []
provides: [WSE-01-engine-filter, WSE-01-startexam-guard]
affects: [Helpers/ShuffleEngine.cs, Controllers/CMPController.cs]
tech-stack:
  added: []
  patterns: [defensive-empty-filter, block-before-write-guard]
key-files:
  created:
    - tests/e2e/exam-taking.spec.ts (Flow L appended)
  modified:
    - Helpers/ShuffleEngine.cs
    - Controllers/CMPController.cs
    - HcPortal.Tests/ShuffleEngineTests.cs
key-decisions:
  - "D-04: ON-path empty-package filter mirrors OFF-path :53-57 — reassign local `packages` to filtered list at TOP of BuildCrossPackageAssignment so existing Count==1 / K=Min run on filtered set (Pitfall 2)."
  - "D-05 approach A: hoist cheap AnyAsync emptiness check BEFORE the justStarted write in StartExam; ALL-empty blocks with friendly BI redirect, zero state mutation. Zero-package case untouched (existing else ~:1198)."
requirements-completed: [WSE-01]
duration: ~35 min
completed: 2026-06-14
---

# Phase 380 Plan 01: SHF-01 Empty-Package + Shuffle ON Summary

ON-path `ShuffleEngine.BuildCrossPackageAssignment` now filters empty packages before the `K = packages.Min(...)` compute (mirroring the OFF-path guard), so a worker with ≥2 sibling packages where one is empty + shuffle ON receives the filled package's questions instead of an empty set. `CMPController.StartExam` blocks (no StartedAt/Status/assignment/SignalR write) when packages exist but ALL are empty — preventing a false batch-wide 0% Fail.

## Execution

- **Duration:** ~35 min · **Tasks:** 3 · **Files:** 4 (3 modified, 1 e2e appended)
- **Commits:** `a2f26fbb` (Task 1 RED test), `ff99479c` (Task 2 engine GREEN), `77d2e206` (Task 3 guard + e2e #6)

### Task 1 — RED unit facts (TDD)
Added `On_MultiPackage_OneEmpty_ReturnsFilledPackageQuestions` + `On_AllPackagesEmpty_ReturnsEmpty` to `ShuffleEngineTests.cs` (reused existing `Pkg(...)` builder). RED proven explicitly: with the engine fix reverted, `On_MultiPackage_OneEmpty` fails `Assert.NotEmpty() Failure: Collection was empty` (K=Min(3,0)=0). `On_AllPackagesEmpty` green from the start (K=0 already returns []).

### Task 2 — Engine fix (GREEN)
Inserted into `BuildCrossPackageAssignment` (after `packages.Count == 0` guard, before `packages.Count == 1`):
```csharp
packages = packages
    .Where(p => p.Questions != null && p.Questions.Count > 0)
    .OrderBy(p => p.PackageNumber)
    .ToList();
if (packages.Count == 0) return new List<int>();
```
"2 paket, satu kosong" now collapses to single-package shuffle → worker gets the filled package's 3 questions. Single-point fix heals all 3 callers (StartExam, ReshufflePackage, ReshuffleAll). ShuffleEngine filter suite 16/16 green.

### Task 3 — Controller guard (D-05) + e2e #6
`StartExam`: before the `justStarted` write, two cheap `AnyAsync` queries over the sibling package set decide `anyPackages && !anyWithQuestions` → friendly BI redirect (`"Ujian belum siap — belum ada soal pada paket. Silakan hubungi Admin atau HC."`), no writes. e2e `Flow L` (6 tests, selectable `-g "empty"`): 2 packages one empty + shuffle ON → worker gets exactly 3 questions, answers correct-by-text (shuffle-safe), DB-asserts Score > 0 + Completed.

## Verification

- `dotnet build` — 0 errors (24 warnings, pre-existing).
- `dotnet test HcPortal.Tests` — **374 passed, 0 failed** (full suite, no regression).
- `dotnet test --filter ShuffleEngine` — 16/16 green (14 prior + 2 new).
- RED→GREEN proven for `On_MultiPackage_OneEmpty` (reverted-fix run failed, re-applied passed).
- **No migration:** `git diff --name-only` shows no `Migrations/*` / `*ModelSnapshot.cs`.
- e2e #6 `Flow L` — TypeScript parses (`playwright --list` lists 6 tests). **Live run deferred** to the consolidated e2e pass (#5 + #6 together) before `/gsd-verify-work`, per VALIDATION.md sampling — avoids standing up the local app+DB harness twice.

## Deviations from Plan

None — plan executed exactly as written.

## Manual-Only Verification (carry to UAT)

- All-empty friendly message wording (BI) — visual check StartExam all-empty path → pesan ramah arah admin (VALIDATION.md manual-only).

## Notes

- **Parallel session active on ITHandoff:** a concurrent session is committing phase 381/382 docs (`docs(381)`/`docs(382)`) interleaved with these commits. They touch only `.planning/` docs, not code — no conflict. Per established project pattern, STATE.md is NOT advanced here (`state advance-plan` skipped) to avoid racing the concurrent writer.

## Next

Ready for Plan 02 (WSE-02 token + WSE-03 AddExtraTime authz/cap). Wave 2 — depends on 380-01 because both plans touch `CMPController.cs`; execute sequentially (this plan = StartExam guard, Plan 02 = VerifyToken).

## Self-Check: PASSED
- key-files exist on disk: ✓ (ShuffleEngine.cs, CMPController.cs, ShuffleEngineTests.cs, exam-taking.spec.ts)
- `git log --grep="380-01"` returns 3 commits: ✓ (a2f26fbb, ff99479c, 77d2e206)
- All acceptance criteria re-run green: ✓
- Plan-level verification (build 0 err, xUnit 374/374, no migration): ✓
