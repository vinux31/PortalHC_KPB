---
status: passed
phase: 376-fix-essay-only-score-aggregation
verified: 2026-06-14
requirements: [GRADE-01, GRADE-02]
note: "Rule-4 pivot — bug no-repro (fixed incidental v27.0); reframed to regression-lock + hardening + prod-repair (Option 1, user-confirmed). All SC met."
---

# Phase 376 — Verification

**Goal (ROADMAP):** Essay-only assessment finalize mengagregasi skor manual ke `AssessmentSessions.Score` (fix Score=0 walau dinilai); konsisten dgn jalur mixed. Diagnose root cause dulu.

**Outcome:** ✅ PASSED. Diagnose-first menemukan bug **tidak reproduce** di code current (sudah ke-fix incidental oleh v27.0 Phase 373). Fase di-reframe (Rule-4, user-confirmed Option 1) → regression-lock + hardening forward path + endpoint recompute untuk repair baris prod historis. Semua Success Criteria terpenuhi.

## Success Criteria

| SC | Kriteria | Status | Bukti |
|----|----------|--------|-------|
| SC1 | Repro lokal essay-only finalize + root cause teridentifikasi | ✅ | `376-DIAGNOSE.md` — repro 2× (sessionId 9019, Score=**80** bukan 0; shuffled=[50055], EssayScore=80). Root cause locked = H1 historis (ShuffledQuestionIds malformed pra-v27), fixed v27.0 Phase 373. |
| SC2 | HC nilai essay-only + finalize → Score = agregasi (bukan 0) | ✅ | e2e FLOW L6 GREEN (Score=80) ×3 run; integration `Forward_EssayOnly_ScoreNotZero` (Percentage=80, MaxScore=100). Forward path via helper. |
| SC3 | Jalur mixed Score tetap benar — no regression | ✅ | unit `Mixed_McMaEssay_MatchesInlineFormula_90Percent` (90%); full suite 361/361; diff scoped 1 hunk. |
| SC4 | Regression test hijau kedua jalur | ✅ | xUnit 6 [Fact] (essay pass/fail, maxScore=0, mixed no-drift, MA partial, empty) + integration 3 [Fact] + e2e L6 un-fixme. |

## Requirements

| REQ | Status | Evidence |
|-----|--------|----------|
| GRADE-01 | ✅ satisfied | Forward path aggregates EssayScore → Score (helper); endpoint recompute repairs historical rows. Diagnose confirmed correct. |
| GRADE-02 | ✅ satisfied | Konsistensi essay-only vs mixed (single helper D-02, no-drift unit); regression test both paths green. |

## must_haves (per plan)

- **376-01:** root cause locked + documented (DIAGNOSE.md) ✓; test scaffold RED→GREEN ✓; controller untouched in diagnose ✓.
- **376-02:** pure helper `AssessmentScoreAggregator` ✓ (impurity grep 0); FinalizeEssayGrading wired + robust derivation (D-06) ✓; invariants 310/324/358 preserved (grep: TrainingRecords.Add=0, WHERE-guard×3, Proton hooks×2) ✓; SC2/SC3 ✓.
- **376-03:** endpoint `RecomputeEssayScores` Admin+antiforgery+idempotent ✓; D-03 (Score+IsPassed only, grep cert/Proton/notif/TR/Status-set all 0) ✓; reuse helper (Compute×2, D-02) ✓; integration real-SQL 3/3 ✓; IT_NOTIFY ✓.

## Test Evidence

- `dotnet test HcPortal.Tests` → **361/361 GREEN** (unit + integration).
- Integration `EssayFinalizeRecomputeTests` → **3/3** (Forward_ScoreNotZero, Recompute_Idempotent_OnlyTouchesScoreZero, Recompute_NoSideEffects).
- e2e `exam-types.spec.ts FLOW L` (`--workers=1`) → **7/7** (L6 Score=80), DB restore clean.
- `dotnet build` → 0 errors.

## Migration

**FALSE** — no schema change. Recompute = DML repair via endpoint (IT executes on Dev/Prod).

## Deviations

**[Rule 4]** Phase premise (reproducible Score=0 bug) refuted by diagnose-first. User confirmed Option 1 (lock + harden + recompute). No wasted artifacts — helper, recompute endpoint, regression tests all deliver value (forward hardening + prod-repair tool for historical broken rows, since v24-v27 bundle not yet deployed to prod).

## Cleanup

- DB lokal `HcPortalDB_Dev` verified clean post-runs (58 sessions, 0 test leftover). e2e harness auto snapshot→restore. App stopped. SEED_JOURNAL entries marked cleaned.
- DB Dev/Prod TIDAK disentuh (CLAUDE.md). Recompute execution = IT handoff.

**Verdict:** Phase 376 goal achieved. Ready for completion.
