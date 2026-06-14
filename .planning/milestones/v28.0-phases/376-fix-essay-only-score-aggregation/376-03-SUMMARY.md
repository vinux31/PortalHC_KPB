---
phase: 376-fix-essay-only-score-aggregation
plan: 03
subsystem: assessment-grading
tags: [endpoint, recompute, idempotent, integration-test, it-handoff]
requires: ["376-02 (helper)"]
provides:
  - "POST /Admin/RecomputeEssayScores (idempotent prod-repair, D-03)"
  - "EssayFinalizeRecomputeTests.cs (integration real-SQL)"
  - "docs/IT_NOTIFY.md Phase 376 entry"
affects: []
tech-stack:
  added: []
  patterns: ["admin idempotent batch endpoint (BackfillProtonPenanda analog)", "real-SQL fixture"]
key-files:
  created:
    - HcPortal.Tests/EssayFinalizeRecomputeTests.cs
  modified:
    - Controllers/AssessmentAdminController.cs
    - docs/IT_NOTIFY.md
key-decisions:
  - "Recompute endpoint Admin-only (mass-repair, lebih ketat dari Admin,HC)"
  - "Score+IsPassed ONLY (D-03) via ExecuteUpdateAsync WHERE Score 0/null (idempotent)"
  - "Candidate predicate: Completed + HasManualGrading + (Score null|0) + essay fully graded"
  - "Integration test di disposable real-SQL DB (HcPortalDB_Dev untouched)"
requirements-completed: [GRADE-01, GRADE-02]
duration: "~30 min"
completed: 2026-06-14
---

# Phase 376 Plan 03: Recompute Endpoint + Integration + IT Handoff Summary

Endpoint admin idempotent `RecomputeEssayScores` untuk repair baris essay-only historis Score=0 (prod pasca-deploy bundle). Reuse helper (D-02). Integration real-SQL 3/3. IT handoff note.

## Tasks
- **Task 1:** `RecomputeEssayScores` action — `[HttpPost][Authorize(Roles="Admin")][ValidateAntiForgeryToken]`, predicate kandidat, reuse `AssessmentScoreAggregator.Compute` (Compute call-sites kini **2** — forward+recompute, D-02), idempotent `ExecuteUpdateAsync` Score+IsPassed only, skip ungraded/maxScore=0, warn-only audit, no-info-leak BI. Commit `87329dc8`. **D-03 grep: cert/Proton/notif/TR/Status-set = 0** ✓.
- **Task 2:** `EssayFinalizeRecomputeTests.cs` (disposable real-SQL fixture, 3 [Fact]). Commit `8979cf31`. **3/3 GREEN.**
- **Task 3:** `docs/IT_NOTIFY.md` Phase 376 entry — migration FALSE, endpoint recompute, eksekusi=IT, D-03, idempotent, langkah pra-eksekusi. Commit `88ffd3ac`.

## D-03 No-Side-Effect (grep evidence — dalam action RecomputeEssayScores)
| Forbidden | Count |
|-----------|-------|
| CertNumberHelper | 0 |
| _protonCompletionService | 0 |
| _protonBypassService | 0 |
| NotifyIfGroupCompleted | 0 |
| TrainingRecords.Add | 0 |
| SetProperty(r => r.Status | 0 |

## Verification
- `dotnet build` exit 0; full unit `dotnet test --filter "Category!=Integration"` 256/256.
- Integration `Category=Integration&~EssayFinalizeRecompute` → **3/3 GREEN** (Forward_ScoreNotZero, Recompute_Idempotent_OnlyTouchesScoreZero, Recompute_NoSideEffects).
- Compute call-sites = 2 (kill-drift D-02 verified).
- IT_NOTIFY ACs: endpoint + migration-false + exec=IT + admin-only + idempotent + D-03 all present.

## Deviations from Plan
None — plan executed as written (continuation of Option 1 direction set in 376-01). Recompute scoped for prod historical repair (prod still pre-bundle).

## Next
Phase gate: full suite + e2e FLOW L6. Then phase verification.

## Self-Check: PASSED
- RecomputeEssayScores Admin+antiforgery, Compute×2, D-03 grep all 0 ✓
- integration 3/3 GREEN, unit 256/256 ✓
- IT_NOTIFY complete (migration false, exec=IT, D-03) ✓
