---
phase: 367-delete-records-cascade-overhaul
plan: 07
subsystem: controllers
tags: [duplicate-guard, data-integrity, exact-match, manual-assessment, tab2]
requires: []
provides:
  - "Guard duplikat EXACT 3 pintu (AddManual reject / Import+BulkBackfill skip-with-report)"
  - "Shared ManualDuplicatePredicate (single-source 3 pintu + test)"
affects:
  - Controllers/AdminBaseController.cs
  - Controllers/TrainingAdminController.cs
  - Models/ImportTrainingResult.cs
tech-stack:
  added: []
  patterns: ["static Expression predicate single-source (pola 04)", "pre-load HashSet + intra-batch dedup (seenInBatch)"]
key-files:
  created:
    - HcPortal.Tests/DuplicateGuardTests.cs
  modified:
    - Controllers/AdminBaseController.cs
    - Controllers/TrainingAdminController.cs
    - Models/ImportTrainingResult.cs
key-decisions:
  - "Shared ManualDuplicatePredicate (static Expression di AdminBaseController) — EXACT UserId+Title+CompletedAt+IsManualEntry; single-source 3 pintu + DuplicateGuardTests (zero drift, pola 04 StandardGroupSiblingPredicate)"
  - "AddManual multi-worker: REJECT seluruh submit pada dup pertama (pre-loop sebelum simpan file → no partial-save), pesan jelas sebut worker+tanggal"
  - "BulkBackfill (in-tx, 1 SaveChanges) pakai seenInBatch HashSet utk intra-batch dedup (AnyAsync tak lihat row uncommitted); pre-load existingUserIds (title+date konstan → key=UserId) 1 query"
  - "ImportTraining (per-row SaveChanges) cukup AnyAsync per-row (intra-batch ter-cover via row sebelumnya yg sudah Save)"
  - "EXACT (CompletedAt ==) bukan ±1 hari (#15 mirror) — re-entry tanggal beda LOLOS (Pitfall 7 false-positive)"
  - "Training-branch ImportTraining TIDAK di-guard (#12 fokus assessment manual = 3 pintu spec); out of scope"
requirements-completed: ["#12", "#14", "D-02"]
duration: "~45 min"
completed: 2026-06-13
---

# Phase 367 Plan 07: Guard Duplikat EXACT 3 Pintu Input Summary

Pasang guard duplikat di 3 pintu input session manual (#12/#14) — cegah akumulasi data kotor di sumber dengan kombinasi `UserId + Title + CompletedAt` PERSIS sama (D-02 EXACT), tanpa false-positive (re-entry tanggal beda = sah).

**Tasks:** 2/2 | **Files:** 1 created + 3 modified | **Tests:** 9 [Fact] (5 predikat real-SQL + 4 dedup logic)

## What was built

- **Shared `ManualDuplicatePredicate(userId, title, completedAt)`** (static `Expression` di `AdminBaseController`) — `s.UserId==X && s.Title==Y && s.CompletedAt==D && s.IsManualEntry`. Dipakai SAMA oleh 3 pintu + test (zero drift).
- **AddManualAssessment (#12, REJECT):** pre-loop per worker SEBELUM simpan file/Add — bila `AnyAsync(predicate)` → `ModelState.AddModelError` (sebut worker+tanggal) + `return View(model)`. No partial-save.
- **ImportTraining assessment-branch (#12, SKIP):** sebelum Add → `AnyAsync(predicate)` true → `result.Status="Skip"; Message="duplikat — dilewati"; continue`. (per-row SaveChanges → intra-batch ter-cover otomatis.)
- **BulkBackfillAssessment (#14, SKIP + intra-batch):** pre-load `existingUserIds` HashSet (1 query, title+date konstan) + `seenInBatch` HashSet → `existingUserIds.Contains(id) || !seenInBatch.Add(id)` → skip + `skippedNips` report, `success` TAK naik. Pesan sukses sertakan jumlah+NIP dilewati.
- **ImportTrainingResult.Status** dukung `"Skip"` (komentar).

## Verification

- `dotnet build` — 0 error.
- **209 quick + 81 integration = 290 pass** (no regression; +9 DuplicateGuardTests).
- DuplicateGuardTests 9 [Fact]: dup-detected, re-entry-tanggal-beda-lolos (Pitfall 7), beda-judul/user-lolos, online-key-sama-tak-dianggap-dup, ImportSkip status+no-Add, intra-batch second-skip, DB-existing-skip-no-success-increment, beda-user-all-added.
- Acceptance greps: ManualDuplicatePredicate ×3 pintu; AddDays 0 (no ±1 hari); Status="Skip"; seenInBatch ×3; Duplikat AddModelError; ImportTrainingResult "Skip".

## Deviations from Plan

**[Rule 2 - Anti-drift] Predikat diekstrak ke static Expression (bukan inline `s.CompletedAt == model.CompletedAt`)** — Acceptance grep harap literal inline. Diganti shared `ManualDuplicatePredicate` (pola 04, single-source 3 pintu + test → zero drift). Greps `model.CompletedAt` (arg) + no-AddDays tetap terpenuhi; EXACT semantik teruji via predikat shared di real-SQL. BulkBackfill pakai Where inline (batch shape beda — Contains multi-user) tapi semantik EXACT sama (`CompletedAt == completedAt`).

**[Decision - Scope] AddManual multi-worker = reject-whole-submit** — Plan beri opsi (reject submit ATAU reject worker dup). Pilih reject submit pada dup pertama (konsisten "single=reject", pesan jelas sebut worker).

**[Decision - Scope] Training-branch ImportTraining tak di-guard** — #12/#14 = 3 pintu assessment manual; training-record dedup out of scope.

**Total deviations:** 3 (1 anti-drift ekstraksi, 2 scope decision terdokumentasi). **Impact:** Positif — predikat single-source unit-tested, no drift 3 pintu.

## Issues Encountered

None.

## Self-Check: PASSED

- 3 pintu guard EXACT (reject/skip) ✓; shared predikat ✓; EXACT bukan ±1 hari (re-entry lolos) ✓.
- ImportTrainingResult Skip ✓; seenInBatch intra-batch ✓; success tak inflate ✓.
- build 0 err; 290/290 ✓; 9 [Fact] ✓; Migration=FALSE ✓.
- Commit code `08e3e8ed` (DuplicateGuardTests git add eksplisit) ✓.

Ready for 367-08 (UI _TrainingRecordsTab di atas badge 371 + Playwright e2e, Wave 5, autonomous=false CHECKPOINT).
