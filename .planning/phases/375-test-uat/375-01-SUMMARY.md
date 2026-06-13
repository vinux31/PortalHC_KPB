---
phase: 375-test-uat
plan: 01
subsystem: testing
tags: [xunit, shuffle, theory, consolidation-test, shuf-15, shuf-16]

requires:
  - phase: 373-shuffle-engine-read-logic-reshuffle
    provides: ShuffleEngine (BuildQuestionAssignment / BuildOptionShuffle / Shuffle)
provides:
  - "ShuffleModeMatrixTests.cs — single-source-of-truth [Theory] mode-matrix sweep (4 InlineData + 1 guard Fact)"
  - "Full xUnit suite konfirmasi hijau 352/352 (baseline 347 + 5 sweep) — SC#1 evidence"
  - "SHUF-15 closed — CMPController.cs verified bebas komentar stale shuffle"
affects: [375-03]

tech-stack:
  added: []
  patterns: ["Consolidation sweep [Theory] high-level per-mode invariant ON TOP of detail tests (no duplication, D-01a)"]

key-files:
  created: [HcPortal.Tests/ShuffleModeMatrixTests.cs]
  modified: []

key-decisions:
  - "File baru terpisah (BUKAN edit ShuffleEngineTests.cs) untuk single-source-of-truth yang jelas"
  - "SHUF-15 = verify-only — CMPController.cs ZERO match komentar stale, tak butuh edit (sudah bersih sejak Phase 373 engine rewrite)"

patterns-established:
  - "Mode-matrix sweep: 1 [Theory] 4 InlineData (ON/OFF × 1/≥2 paket × opsi) + 1 [Fact] guard DivideByZero, determinisme seed Random(42) di semua mode"

requirements-completed: [SHUF-16, SHUF-15]

duration: ~8min
completed: 2026-06-14
---

# Phase 375 Plan 01: Consolidation xUnit Sweep + SHUF-15 Summary

**ShuffleModeMatrixTests.cs consolidation sweep (4 InlineData mode-matrix + DivideByZero guard) + full suite hijau 352/352 + SHUF-15 closed (CMPController clean)**

## Performance

- **Duration:** ~8 min
- **Tasks:** 2 (1 kode, 1 verify-only)
- **Files modified:** 1 created

## Accomplishments
- `ShuffleModeMatrixTests.cs` baru: 1 `[Theory] ModeMatrix_Invariant` dengan 4 `[InlineData]` (ON 1pkg, OFF 1pkg, OFF ≥2pkg round-robin, ON ≥2pkg) + 1 `[Fact] AllPackagesEmpty_NoDivideByZero`. High-level per-mode invariant + determinisme seed `Random(42)` di semua mode — TANPA menduplikasi detail `ShuffleEngineTests` (D-01a).
- Sweep filtered: **5 passed / 0 failed**.
- Full suite: **352 passed / 0 failed** (baseline 347 + 5 sweep). **SC#1 terpenuhi.**
- SHUF-15: re-grep `Controllers/CMPController.cs` (`option shuffle removed` / `shuffle removed`, case-insensitive) → **ZERO match**. Sudah bersih (line 989 = `// Option shuffle gated on ShuffleOptions`). Verify-only, no edit. **SHUF-15 closed.**

## Task Commits

1. **Task 1: Tulis consolidation mode-matrix sweep** - `fcc0d020` (test)
2. **Task 2: SHUF-15 verify + full suite** - no commit (CMPController clean = no edit; full suite = verification, no file change)

## Files Created/Modified
- `HcPortal.Tests/ShuffleModeMatrixTests.cs` - Consolidation sweep [Theory] mode-matrix + guard Fact

## Decisions Made
- File terpisah untuk single-source-of-truth (bukan edit ShuffleEngineTests.cs)
- SHUF-15 = verify-only (CMPController sudah bersih)

## Deviations from Plan
None - plan executed exactly as written.

## Issues Encountered
None. Build warnings yang muncul saat `dotnet test` semua pre-existing (nullability/async di view & controller lain), bukan dari file baru.

## Next Phase Readiness
- SC#1 evidence siap (suite 352/352 hijau termasuk sweep).
- Plan 02 (Playwright e2e) + Plan 03 (manual exam-diff) bisa lanjut.

---
*Phase: 375-test-uat*
*Completed: 2026-06-14*
