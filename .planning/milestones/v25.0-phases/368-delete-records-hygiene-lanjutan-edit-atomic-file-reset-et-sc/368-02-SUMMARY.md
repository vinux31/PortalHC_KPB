---
phase: 368-delete-records-hygiene-lanjutan
plan: 02
subsystem: controllers
tags: [import-audit, assessment-type-constant, bulk-label, attempt-history-cleanup, admin-endpoint]
requires:
  - "TrainingAdminController hardened (368-01)"
provides:
  - "ImportTraining audit ringkasan + AssessmentType konstanta + GenerateCertificate=isPassed (#24)"
  - "BulkBackfill AssessmentType konstanta + label 'Bulk Import Nilai (Excel)' (#27)"
  - "CleanupAttemptHistory GET preview + POST execute+audit (idempotent) (#23)"
affects:
  - Controllers/TrainingAdminController.cs
  - Views/Admin/CleanupAttemptHistory.cshtml
  - Views/Admin/BulkBackfill.cshtml
  - Views/Admin/Index.cshtml
  - Views/Admin/Shared/_AssessmentGroupsTab.cshtml
tech-stack:
  added: []
  patterns: ["idempotent admin maintenance endpoint (preview-count GET → execute+audit POST)", "1-entri audit ringkasan (bukan per-row)", "orphan detection via NOT EXISTS (SessionId dangling, no FK)"]
key-files:
  created:
    - Views/Admin/CleanupAttemptHistory.cshtml
    - HcPortal.Tests/OrphanCleanupTests.cs
    - HcPortal.Tests/ImportTrainingAuditTests.cs
  modified:
    - Controllers/TrainingAdminController.cs
    - Views/Admin/BulkBackfill.cshtml
    - Views/Admin/Index.cshtml
    - Views/Admin/Shared/_AssessmentGroupsTab.cshtml
key-decisions:
  - "#23 endpoint disisip setelah BulkBackfill GET (TrainingAdminController) — punya _context/_auditLog/_userManager; orphan predikat single-source di GET preview + POST execute (identik)"
  - "#23 orphan SEMPIT: !AssessmentSessions.Any(s => s.Id == h.SessionId) — AssessmentAttemptHistory.SessionId plain int NO FK (insert orphan test bebas)"
  - "#24 audit = 1 entri ringkasan (ok/skip/err counts) sebelum return View(results), bukan per-row (Open Question 3 RESEARCH)"
  - "#24/#23 test data-level (pola A4): hindari mock ClosedXML/UserManager/HttpContext; field-contract Theory + AuditLog persist + orphan DbContext"
  - "BulkBackfill.cshtml label lama 'Bulk Backfill Assessment (REST-04)' (beda dari grep target) ikut diganti ke 'Bulk Import Nilai (Excel)' — 3 occurrence di file itu + 1 Index + 1 AssessmentGroupsTab = 5 total"
requirements-completed: ["#24", "#27", "#23"]
duration: "~18 min"
completed: 2026-06-13
---

# Phase 368 Plan 02: ImportTraining audit/konstanta (#24) + BulkBackfill label (#27) + CleanupAttemptHistory (#23) Summary

Tiga fix di `TrainingAdminController.cs` (+ 4 view): import ter-audit + field konsisten, BulkBackfill label jujur, dan endpoint admin idempotent untuk bersihkan orphan AttemptHistory legacy.

**Tasks:** 3/3 | **Files:** 3 created + 4 modified | **Tests:** 5 [Fact]/[Theory] (1 orphan real-SQL + 2 field-contract Theory + 1 audit-persist; + EditAtomicFile 3 dari 01)

## What was built

- **#24 ImportTraining:** `GenerateCertificate = true` → `= isPassed` (hanya lulus dapat sertifikat); `AssessmentType = ""` → `= AssessmentConstants.AssessmentType.Manual`; tambah 1 audit log ringkasan `"ImportTraining"` (X sukses/Y skip/Z error) sebelum `return View(results)`.
- **#27 BulkBackfill:** `AssessmentType = "Standard"` → `= AssessmentConstants.AssessmentType.Manual`; label UI di 3 view → persis **"Bulk Import Nilai (Excel)"** (BulkBackfill.cshtml title+breadcrumb+h2, Index.cshtml, _AssessmentGroupsTab.cshtml; ikon dipertahankan). Residu identitas sesi backfill (Id baru) ACCEPTED — tak disentuh.
- **#23 CleanupAttemptHistory:** GET preview-count orphan (read-only) + POST `CleanupAttemptHistoryExecute` `[Authorize(Admin)]`+`[ValidateAntiForgeryToken]` RemoveRange + audit `"CleanupAttemptHistory"`. Idempotent (re-run query auto-empty → deleted=0). Orphan SEMPIT: `!AssessmentSessions.Any(s => s.Id == h.SessionId)`. View Bahasa Indonesia + confirm dialog + AntiForgeryToken + tombol disembunyikan saat 0 orphan. Endpoint runtime (BUKAN EF migration/SQL serah-IT), Migration=false.

## Verification

- `dotnet build` — 0 error.
- `dotnet test --filter "OrphanCleanup|ImportTrainingAudit"` — **4/4** (real-SQL: orphan preview=2 → execute → idempotent=0; field-contract Ya/Tidak; audit persist).
- Quick suite `--filter "Category!=Integration"` — **212/212** (no regression).
- Acceptance greps: `GenerateCertificate = isPassed` =1; `AssessmentType = AssessmentConstants.AssessmentType.Manual` =3; `AssessmentType = ""`/`"Standard"` =0; `"ImportTraining"` =1; label baru ≥3 (5 total); label lama `Bulk Backfill (Restore Lost Data)` =0; orphan predikat =2 (GET+POST); CSRF+Authorize di POST; view AntiForgeryToken+OrphanCount ✓.
- Migration = FALSE (no `Migrations/*368*`) ✓.

## Deviations from Plan

**[Rule 1 - Kelengkapan] Line refs interfaces bergeser** — Found during: Task 1. Plan interfaces tulis GenerateCertificate L1297 / AssessmentType L1307 / return L1415 / BulkBackfill L884; aktual L1332 / L1342 / L1450 / L909 (file lebih panjang pasca-367). Lokasi diverifikasi via grep, edit pakai konteks unik (bukan line number). Verification: greps pass.

**Total deviations:** 1 (drift line-number, dikoreksi via grep). **Impact:** Tidak ada — perubahan identik secara semantik.

## Issues Encountered

None.

## Self-Check: PASSED

- ImportTraining: isPassed + Manual konstanta + audit "ImportTraining" ✓; BulkBackfill Manual konstanta + label 3 view ✓.
- CleanupAttemptHistory GET preview + POST CSRF+admin+audit + orphan predikat single-source ×2 ✓; view AntiForgeryToken+OrphanCount+confirm ✓.
- OrphanCleanupTests preview=2/idempotent=0 ✓; ImportTrainingAuditTests field+audit ✓.
- build 0 err; 212/212 quick + 4 integration; Migration=FALSE ✓.

Ready for 368-03 (ResetAssessment RemoveRange ET scores #22). Plan 03 = Wave 1 (file beda: AssessmentAdminController.cs).
