---
phase: 368-delete-records-hygiene-lanjutan
plan: 01
subsystem: controllers
tags: [edit-atomic-file, renewal-validation, file-replace, idor-guard]
requires: []
provides:
  - "EditTraining + EditManualAssessment atomic file replace (#21, pola 331)"
  - "EditTraining renewal validation Renews*Id exist+same-user (#26)"
affects:
  - Controllers/TrainingAdminController.cs
tech-stack:
  added: []
  patterns: ["atomic file replace (capture-before → SaveChanges → File.Delete post-commit warn-only)", "static-predicate validation contract test (data-level, no controller mock)"]
key-files:
  created:
    - HcPortal.Tests/EditAtomicFileTests.cs
    - HcPortal.Tests/RenewalValidationTests.cs
  modified:
    - Controllers/TrainingAdminController.cs
key-decisions:
  - "#26 var rename srcAsRenew (bukan srcAs) — hindari shadow dgn srcAs DAG-validation L491 (sibling scope sebenarnya aman, tapi nama beda lebih jelas)"
  - "#26 test data-level (predikat src==null||src.UserId!=target.UserId) bukan controller-invoke — pola A4 RESEARCH fallback (mock HubContext/UserManager/SignalR terlalu berat); acceptance grep Task 2 cover sisi controller"
  - "#21 delete-old STRICTLY conditional: oldUrl di-null-kan saat uploadedUrl==null (upload gagal) ATAU CertificateFile==null (metadata-only) → File.Delete tak terpanggil"
requirements-completed: ["#21", "#26"]
duration: "~12 min"
completed: 2026-06-13
---

# Phase 368 Plan 01: Edit Atomic File Replace (#21) + Renewal Validation (#26) Summary

Hardening 2 method POST di `TrainingAdminController.cs`: file sertifikat kini di-replace atomik (no data-loss saat upload gagal) dan `EditTraining` menolak link renewal lintas-user (IDOR root-cause) tanpa merusak edit data legacy.

**Tasks:** 3/3 | **Files:** 2 created + 1 modified | **Tests:** 6 [Fact] (3 atomic file-on-disk + 3 renewal real-SQL)

## What was built

- **#21 atomic file (EditTraining `SertifikatUrl` + EditManualAssessment `ManualSertifikatUrl`):** ganti pola NON-atomik (File.Delete LAMA **sebelum** SaveFileAsync — bug: upload gagal = sertifikat hilang permanen) → pola Phase 331: capture `oldUrl` → `SaveFileAsync` baru → set url → `SaveChangesAsync` → `FileUploadHelper.DeleteFile(old)` **POST-commit** dalam try/catch warn-only. Delete LAMA hanya bila `uploadedUrl != null` (upload sukses) — metadata-only / upload-gagal → file lama utuh.
- **#26 EditTraining renewal validation:** disisipkan setelah `FindAsync` (butuh `record.UserId` + Renews*Id LAMA), sebelum assign. Validasi `RenewsTrainingId`/`RenewsSessionId` (exist + same-user) **HANYA saat field BERUBAH** (`model.Renews*Id != record.Renews*Id && HasValue`) → toleran legacy. Invalid (tak exist / beda user) → `ModelState.AddModelError` → TempData firstError + redirect (mirror blok L502-510). DAG-validation existing (L483-494) tidak disentuh.

## Verification

- `dotnet build` — 0 error.
- `dotnet test --filter "EditAtomicFile"` — **3/3** (file-on-disk temp-dir, pola 355).
- `dotnet test --filter "RenewalValidation"` — **3/3** (real-SQL @localhost\SQLEXPRESS, reuse RecordCascadeFixture).
- Quick suite `--filter "Category!=Integration"` — **212/212** (209 baseline + 3 EditAtomicFile, no regression).
- Acceptance greps: DeleteFile post-commit di EditTraining (L567) + EditManualAssessment (L1037) ✓; PRE-save `System.IO.File.Delete(oldPath)` = 0 ✓; `UserId != record.UserId` = 2 ✓; `model.RenewsTrainingId != record.RenewsTrainingId` = 1 ✓.
- Migration = FALSE (zero schema change, no `Migrations/*368*`) ✓.

## Deviations from Plan

**[Rule 2 - Naming] #26 var `srcAsRenew` bukan `srcAs`** — Found during: Task 2. Plan action tulis `srcAs`, tapi DAG-validation existing L491 sudah pakai `srcAs` (sibling if-block, sebenarnya tak konflik scope). Pakai nama distinct `srcAsRenew` demi kejelasan. Greps acceptance tetap match (`UserId != record.UserId` ≥2). Verification: build 0 error.

**Total deviations:** 1 (rename kosmetik demi kejelasan). **Impact:** Tidak ada — semantik identik.

## Issues Encountered

None.

## Self-Check: PASSED

- EditAtomicFileTests.cs 3 [Fact] `EditAtomicFile*` hijau; `DeletesOldFileOnDisk` + `Directory.Delete(dir, recursive: true)` present ✓.
- TrainingAdminController: post-commit DeleteFile ×2, PRE-save delete 0, same-user check ×2, only-when-changed ×1 ✓.
- RenewalValidationTests.cs 3 [Fact] real-SQL hijau; `RecordCascadeFixture` reuse + `[Trait("Category","Integration")]` ✓.
- build 0 err; 212/212 quick; Migration=FALSE ✓.

Ready for 368-02 (ImportTraining audit/konstanta #24 + BulkBackfill label #27 + CleanupAttemptHistory endpoint #23). Plan 02 depends 01 (file-overlap TrainingAdminController.cs — kini hardened).
