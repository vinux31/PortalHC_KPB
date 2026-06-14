---
phase: 376-fix-essay-only-score-aggregation
plan: 02
subsystem: assessment-grading
tags: [helper, kill-drift, grading, hardening]
requires: ["376-01 (test contract + diagnose)"]
provides:
  - "Helpers/AssessmentScoreAggregator.cs (pure, single source of truth)"
  - "FinalizeEssayGrading wired to helper + robust derivation (D-06)"
affects:
  - "Controllers/AssessmentAdminController.cs RecomputeEssayScores (Plan 03 reuses helper)"
tech-stack:
  added: []
  patterns: ["pure helper kill-drift (ShuffleEngine analog)", "defensive fallback + log"]
key-files:
  created:
    - Helpers/AssessmentScoreAggregator.cs
  modified:
    - Controllers/AssessmentAdminController.cs
key-decisions:
  - "Math agregasi diekstrak verbatim → helper murni; FinalizeEssayGrading panggil Compute()"
  - "D-06 fallback derivasi dari PackageUserResponses saat shuffledIds kosong + LogWarning (insurance, code current sudah benar)"
  - "D-05 maxScore=0 → Score 0 + LogWarning, no block"
  - "Invariant 310/324/358 preserved verbatim (grep-verified)"
requirements-completed: [GRADE-01, GRADE-02]
duration: "~25 min"
completed: 2026-06-14
---

# Phase 376 Plan 02: Helper Extract + Forward Wire Summary

Ekstrak math agregasi inline `FinalizeEssayGrading` → `Helpers/AssessmentScoreAggregator.cs` (pure, D-04 verbatim, D-02 kill-drift). Wire helper + derivasi question-set robust (D-06 fallback) + log warning (D-05). Karena bug sudah ke-fix v27 (lihat 376-DIAGNOSE.md), ini = hardening + regression-lock, bukan perbaikan code rusak.

## Tasks
- **Task 1:** `Helpers/AssessmentScoreAggregator.cs` — `Compute(questions, responses, passPct) → ScoreAggregateResult`. Pure (impurity grep=0). Commit `c73bb095`. Unit test Plan 01 → **6/6 GREEN**.
- **Task 2:** `FinalizeEssayGrading` patched — derivasi robust (shuffledIds kosong→fallback PackageUserResponses+LogWarning) + panggil `AssessmentScoreAggregator.Compute` (ganti math L3535-3564) + maxScore=0 LogWarning. Commit `203aa6d6`.

## Diff scope (Controllers/AssessmentAdminController.cs)
Satu hunk `@@ -3524,45 +3524,39 @@` (29 ins / 35 del). HANYA derivasi `allQuestions` + blok math. Region invariant (L3466-3526, L3566-3669) tak disentuh.

## Invariant preserved (grep evidence)
| Invariant | Check | Hasil |
|-----------|-------|-------|
| Helper wired | `AssessmentScoreAggregator.Compute` | 1 ✓ |
| Shuffle source (310) | `GetShuffledQuestionIds` | 3 (preserved) ✓ |
| Replay-guard (310) | `Status == ...PendingGrading` | 3 ✓ |
| No auto-TR (324) | `_context.TrainingRecords.Add` | 0 ✓ |
| Proton hook (358) | `EnsureAsync`=2 / `NotifyIfGroupCompleted`=2 | preserved ✓ |

## Verification
- `dotnet build` exit 0.
- `dotnet test HcPortal.Tests --filter "Category!=Integration"` → **256/256 GREEN** (no regression).
- e2e `npx playwright test exam-types --grep "FLOW L" --workers=1` → **7/7 passed**, L6 Score=80 (SC2/SC3 confirmed dgn helper wired). teardown RESTORE OK, DB bersih.

## Deviations from Plan
**[Rule 2 — context]** Forward "fix" diposisikan sbg hardening (premis bug gugur, lihat 376-01 deviation Rule 4). Tetap eksekusi penuh per Option 1 user-confirmed. Fallback D-06 = insurance anti-regresi shuffle masa depan (code current tak butuh fallback — shuffledIds terisi). **Total:** 1 (continuation dari 376-01 pivot). **Impact:** none — helper + wiring + regression guard semua valuable.

## Next
Ready for **376-03**: endpoint `RecomputeEssayScores` (reuse helper, Score+IsPassed only D-03) + integration real-SQL + IT_NOTIFY.

## Self-Check: PASSED
- Helper pure (impurity=0), class + record struct ✓
- Compute wired (1), invariants preserved (grep) ✓
- build 0 err, 256/256 unit, e2e 7/7 L6 GREEN ✓
- diff scoped one hunk ✓
