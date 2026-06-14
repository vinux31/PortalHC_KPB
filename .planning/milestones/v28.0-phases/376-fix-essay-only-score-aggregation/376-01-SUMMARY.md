---
phase: 376-fix-essay-only-score-aggregation
plan: 01
subsystem: assessment-grading
tags: [diagnose, regression-test, essay, grading]
requires: []
provides:
  - "376-DIAGNOSE.md (root cause locked, no-repro finding)"
  - "AssessmentScoreAggregatorTests.cs (RED — contract for Plan 02 helper)"
  - "exam-types.spec.ts FLOW L6 un-fixme (regression guard)"
affects:
  - Helpers/AssessmentScoreAggregator.cs (Plan 02 must satisfy this test contract)
tech-stack:
  added: []
  patterns: ["pure xUnit (ShuffleEngineTests analog)", "diagnose-first gate"]
key-files:
  created:
    - .planning/phases/376-fix-essay-only-score-aggregation/376-DIAGNOSE.md
    - HcPortal.Tests/AssessmentScoreAggregatorTests.cs
  modified:
    - tests/e2e/exam-types.spec.ts
key-decisions:
  - "Bug essay-only Score=0 TIDAK reproduce di code current — fixed incidental v27.0 Phase 373 (ShuffledQuestionIds malformed pra-v27 = H1 historis)"
  - "Fallback derivasi question-set = PackageUserResponses session (guard defensif D-06, bukan fix)"
  - "Predicate recompute = Status=Completed AND HasManualGrading=1 AND (Score IS NULL OR Score=0)"
  - "Phase pivot (Rule-4): forward-fix → hardening + regression-lock + prod-repair (Option 1, user-confirmed)"
requirements-completed: [GRADE-01, GRADE-02]
duration: "~50 min"
completed: 2026-06-14
---

# Phase 376 Plan 01: Diagnose-First + Test Scaffold Summary

Diagnose-first repro (e2e FLOW L, 2× deterministik) menemukan bug essay-only Score=0 **TIDAK reproduce** di code current — finalize benar `Score=80`. Root cause historis (H1: ShuffledQuestionIds malformed/empty pra-v27) sudah ke-fix incidental oleh v27.0 Phase 373 ShuffleEngine rewrite. Test scaffold (xUnit + e2e L6 un-fixme) ditulis RED sebagai kontrak + regression guard.

## Tasks
- **Task 1:** `376-DIAGNOSE.md` (89 baris) — repro 2× (sessionId 9019, Score=80, shuffled=[50055], EssayScore=80), tabel H1..H5 (H1 historis, sisanya ditolak), root cause story v27, keputusan fallback derivasi + recompute predicate. Commit `46998257`. AC: controller untouched ✓.
- **Task 2:** `AssessmentScoreAggregatorTests.cs` (6 [Fact]: essay-only pass/fail, maxScore=0 D-05, mixed no-drift 90% locking D-04, MA partial, empty) + un-fixme e2e FLOW L6 (kini PASS Score=80). Commit `b2a526a4`. RED state confirmed: `error CS0103: AssessmentScoreAggregator does not exist` (helper Plan 02).

## RED State Evidence (gate untuk Plan 02 GREEN)
```
HcPortal.Tests\AssessmentScoreAggregatorTests.cs(39,22): error CS0103:
  The name 'AssessmentScoreAggregator' does not exist in the current context
```
e2e FLOW L6 (run #2, fixme dihapus): assertion asli `expect(score).toBe(80)` + Status='Completed' → **PASS** (7 passed, teardown RESTORE OK).

## Deviations from Plan

**[Rule 4 — Architectural/Scope] Premis fase gugur: bug tidak reproduce.**
- Found during: Task 1 (diagnose-first repro SC1).
- Issue: Plan SC1 mengasumsikan repro `Score=0`. Repro lokal (2×) justru `Score=80` (benar). Bug sudah resolved oleh v27.0.
- Resolution: STOP + present ke user (interactive checkpoint). User konfirmasi **Option 1** (Lock + harden + recompute): forward-fix Plan 02 jadi hardening (bukan perbaikan code rusak), recompute Plan 03 tetap (repair baris prod historis — bundle v24-v27 belum push, prod masih buggy), regression-lock via test.
- Impact: Tujuan fase bergeser dari "fix bug" → "regression-lock + hardening + prod-repair". Value tetap tinggi. SC1 terpenuhi sebagai "root cause teridentifikasi" (bukan "Score=0 dikonfirmasi" — direfute dengan data).

**Total deviations:** 1 (Rule 4, user-confirmed direction). **Impact:** scope reframe, no wasted work — semua artefak (helper, recompute, test) tetap relevan.

## Cleanup
- 2× e2e run: harness auto snapshot→restore. DB lokal verified bersih (58 sessions, 0 leftover). App lokal di-stop pasca-diagnose.
- `exam-types.spec.ts` diagnostic edits di-revert; un-fixme resmi = commit ini.
- TIDAK menyentuh DB Dev/Prod (CLAUDE.md).

## Next
Ready for **376-02** (GREEN): buat `Helpers/AssessmentScoreAggregator.cs` (D-04 verbatim) → unit test hijau + wire FinalizeEssayGrading + guard defensif.

## Self-Check: PASSED
- 376-DIAGNOSE.md ada (89 baris ≥30), root cause + H1 ✓
- AssessmentScoreAggregatorTests.cs: 6 [Fact], Compute() call, no Integration trait ✓
- exam-types.spec.ts: 0 test.fixme, L6 title preserved ✓
- RED confirmed (CS0103 helper absent) ✓
- git diff Controllers/ kosong (Task 1) ✓
