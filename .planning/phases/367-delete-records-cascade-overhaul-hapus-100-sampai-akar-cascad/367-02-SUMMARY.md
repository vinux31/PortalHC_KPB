---
phase: 367-delete-records-cascade-overhaul
plan: 02
subsystem: services
tags: [cascade-delete, transaction, soft-cancel, proton, file-cleanup, integration-test]
requires:
  - "RecordCascadeDeleteService.CollectCascadeIds (367-01)"
  - "ProtonCompletionService.RemoveExamOriginAsync"
provides:
  - "RecordCascadeDeleteService.ExecuteAsync (1-tx cascade + CascadeResult)"
affects:
  - Services/RecordCascadeDeleteService.cs
tech-stack:
  added: []
  patterns: ["1-transaction cascade + post-commit file/audit (pola Phase 331-334)", "disposable real-SQL fixture (pola Phase 344/366)"]
key-files:
  created:
    - HcPortal.Tests/RecordCascadeIntegrationTests.cs
    - HcPortal.Tests/RecordCascadeFileTests.cs
  modified:
    - Services/RecordCascadeDeleteService.cs
    - HcPortal.Tests/RecordCascadeServiceTests.cs
    - HcPortal.Tests/MirrorHeuristicTests.cs
key-decisions:
  - "ExecuteAsync signature += actorId, actorName (service tak punya User; controller meneruskan aktor untuk AuditLog)"
  - "RemoveExamOriginAsync dipanggil DALAM tx walau ber-SaveChanges internal (A4): flush partial, rollback tetap utuh (terbukti test Rollback)"
  - "Image SOAL TIDAK di engine (Opsi B) — hanya file sertifikat (ManualSertifikatUrl/SertifikatUrl); image-soal = ranah 366/plan 05"
  - "OQ-1 L-05 konservatif: notif eksak /CMP/StartExam/{id} + 2 dormant; TANPA /CMP/Certificate (tak ada di kode)"
requirements-completed: ["#5", "#6", "#8", "#9", "#10", "#11", "#19", "L-04", "L-08", "spec-3.1-execute"]
duration: "32 min"
completed: 2026-06-12
---

# Phase 367 Plan 02: ExecuteAsync (Mutasi Cascade 1-TX) Summary

Menambah `ExecuteAsync` (mutasi, 1-transaction) ke `RecordCascadeDeleteService` — jantung fix kasus Rino (#3). Hapus semua node hasil `CollectCascadeIds` (SAMA dengan preview → invariant preview==execute) + seluruh artefak per node, parity gold-standard `DeleteAssessment` + 4 delta 367. Divalidasi 11 [Fact] real-SQL per-tabel.

**Tasks:** 2/2 | **Files:** 2 created + 3 modified | **Tests:** 11 [Fact] (8 integration + 3 file)

## What was built

- **`ExecuteAsync(rootType, rootId, mirrorTrainingIds, actorId, actorName)` → `CascadeResult`** dalam 1 `BeginTransactionAsync`:
  - Per node session: artefak gold-standard verbatim (EditLogs→Responses→AttemptHistory→UPA→Packages+Q+O, urutan Restrict-FK).
  - **#8** `LinkedSessionId = null` pasangan sebelum Remove. **L-04** `PendingProtonBypass` → `Status="Dibatalkan"` + `ResolvedAt` (soft-cancel, BUKAN Remove). **#9** `RemoveExamOriginAsync` jika `ProtonTrackId.HasValue` (Interview/Bypass kebal). **#6** `UserNotifications` eksak-match `/CMP/StartExam/{id}` + 2 dormant (OQ-1 konservatif, tanpa Certificate).
  - **#19** file sertifikat collect-before-Remove → `File.Delete` POST-commit warn-only (confined webroot, V12). **L-08** AuditLog 1 entri post-commit (warn-only). Catch → pesan GENERIK (no `ex.Message`, V7).
  - **V5/IDOR:** mirror-ID dari client divalidasi server-side via `FindMirrorCandidates` (UserId match) sebelum hapus.
- ctor diperluas: `+ProtonCompletionService, AuditLogService, IWebHostEnvironment`. Plan 01 test ctor di-null-substitute.

## Verification

- `dotnet build` — 0 error.
- Integration+file `dotnet test --filter "~RecordCascadeIntegrationTests|~RecordCascadeFileTests"` — **11/11 pass** (real-SQL @localhost\SQLEXPRESS).
- **Full suite `dotnet test` — 258/258 pass** (no regression; +22 dari 367-01/02).
- Bukti per-tabel: full cascade #5/#11, soft-cancel #10/L-04, null-clear #8, Origin Exam removed + Interview kebal #9, notif #6, audit L-08, file #19, rollback-utuh, preview==execute.

## Deviations from Plan

**[Rule 1 - Bug seed test] ProtonFinalAssessment Exam+Interview butuh assignment/track BERBEDA** — Found during: Task 2. DB punya unique index `IX_ProtonFinalAssessments_ProtonTrackAssignmentId` (1 FA per assignment) + `AK_ProtonTracks_TrackType_TahunKe` (6 track standar sudah di-seed migration). Fix: REUSE 2 track seeded berbeda, 1 assignment per Origin. Files: `RecordCascadeIntegrationTests.cs`. Verification: 11/11 pass.

**Total deviations:** 1 auto-fixed (test seed FK/unique-constraint). **Impact:** Tidak ada pada kode produksi — hanya desain seed test.

## Issues Encountered

None (produksi). Catatan: integration test WAJIB `localhost\SQLEXPRESS` aktif (skip via `--filter "Category!=Integration"`).

## Self-Check: PASSED

- `grep "public async Task<CascadeResult> ExecuteAsync"` ✓; `BeginTransactionAsync`+`tx.CommitAsync` ✓; `Status = "Dibatalkan"`+`ResolvedAt` ✓; `RemoveExamOriginAsync`+`ProtonTrackId.HasValue` ✓; `LinkedSessionId = null` ✓; `/CMP/StartExam/` ✓; `/CMP/Certificate/` = 0 match ✓; `CollectCascadeIds` di ExecuteAsync ✓.
- File.Delete POST `CommitAsync` ✓; image SOAL tidak di-collect di engine (Opsi B) ✓.
- Migration = FALSE ✓.

Ready for 367-03 (badge recompute) / wiring endpoint (05/06).
