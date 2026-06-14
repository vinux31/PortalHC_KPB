---
phase: 368-delete-records-hygiene-lanjutan
plan: 03
subsystem: controllers
tags: [reset-assessment, et-scores-cleanup, stale-analytics, retake]
requires: []
provides:
  - "ResetAssessment RemoveRange SessionElemenTeknisScores — retake regen ET fresh (#22)"
affects:
  - Controllers/AssessmentAdminController.cs
tech-stack:
  added: []
  patterns: ["RemoveRange ditambahkan ke batch cleanup existing (no new transaction)"]
key-files:
  created:
    - HcPortal.Tests/ResetEtScoreTests.cs
  modified:
    - Controllers/AssessmentAdminController.cs
key-decisions:
  - "RemoveRange ET disisip SETELAH Remove UPA, SEBELUM SaveChangesAsync existing (L3974-area) → ter-flush batch sama; TANPA BeginTransactionAsync (ResetAssessment sengaja pakai SaveChanges + ExecuteUpdateAsync — koreksi RESEARCH load-bearing)"
  - "Hanya SessionElemenTeknisScores; cleanup lain (PackageUserResponses/UPA/archive AttemptHistory) + ExecuteUpdateAsync + ResetGuard 367 tak disentuh (anti-scope-creep)"
  - "Test data-level (pola A4): cleanup+regen pada DbContext real-SQL; [Fact] WithoutCleanup_DuplicateThrows membuktikan unique-index = akar #22"
requirements-completed: ["#22"]
duration: "~10 min"
completed: 2026-06-13
---

# Phase 368 Plan 03: ResetAssessment RemoveRange ET Scores (#22) Summary

Reset assessment kini ikut menghapus `SessionElemenTeknisScores` sesi → retake meng-generate ET scores BARU, bukan stale (sebelumnya `GradingService` gagal Add ET karena unique index → exception ditelan → analitik ET salah).

**Tasks:** 2/2 | **Files:** 1 created + 1 modified | **Tests:** 3 [Fact] integration real-SQL

## What was built

- **#22 ResetAssessment:** sisip `RemoveRange(SessionElemenTeknisScores.Where(e => e.AssessmentSessionId == id))` ke batch cleanup existing, **SEBELUM** `SaveChangesAsync` (di antara Remove UPA dan ExecuteUpdateAsync). Retake regen ET fresh tanpa unique-index violation.
- **Koreksi load-bearing diterapkan:** ResetAssessment TIDAK punya explicit transaction — RemoveRange ditambah ke `SaveChanges` existing, **TANPA** `BeginTransactionAsync` baru (mencegah scope creep). Cleanup lain + ExecuteUpdateAsync + ResetGuard 367 tidak disentuh.

## Verification

- `dotnet build` — 0 error.
- `dotnet test --filter "ResetEtCleanup"` — **3/3** (real-SQL): cleanup hapus ET; retake re-insert ElemenTeknis sama tanpa violation (CorrectCount fresh); bukti tanpa-cleanup → `DbUpdateException` (unique index akar #22).
- Quick suite `--filter "Category!=Integration"` — **212/212** (no regression).
- Acceptance greps: `SessionElemenTeknisScores ... Where(e => e.AssessmentSessionId == id)` + `RemoveRange` di ResetAssessment (L3975-3979, sebelum SaveChanges) ✓; no `BeginTransactionAsync` ditambah ✓.
- Migration = FALSE (no `Migrations/*368*`) ✓.

## Deviations from Plan

None — plan executed exactly as written (line refs L3958-3988 cocok dengan source aktual).

## Issues Encountered

None.

## Self-Check: PASSED

- RemoveRange ET di ResetAssessment, sebelum SaveChanges, no new transaction ✓.
- ResetEtScoreTests 3 [Fact] real-SQL hijau (incl bukti unique-index akar) ✓.
- build 0 err; 212/212 quick; Migration=FALSE ✓.

Ready for 368-04 (CertificationManagement dedup helper #25 + UAT checkpoint #23/#27/#25). Plan 04 = Wave 2, Task 3 autonomous:false (UAT browser).
