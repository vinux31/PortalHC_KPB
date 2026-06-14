---
phase: 366-cascade-image-file-cleanup-orphan-gambar-deleteassessment-gr
plan: 03
subsystem: assessment-admin
tags: [integration-test, real-sql, uat, image-cleanup]
requires:
  - "Helpers/ImageFileCleanup.DeleteUnreferencedAsync (Plan 01)"
  - "3 cascade Delete* install (Plan 02)"
provides:
  - "Integration test real-SQL membuktikan SC#2 (orphan deleted) + SC#3 (shared survives)"
  - "Mirror PackageImageDeleteTests ditandai sumber-kebenaran produksi (D-04)"
  - "UAT browser SC#2 end-to-end (DeleteAssessmentGroup → file fisik bersih)"
affects:
  - HcPortal.Tests/ImageCleanupIntegrationTests.cs
  - HcPortal.Tests/PackageImageDeleteTests.cs
tech-stack:
  added: []
  patterns: ["disposable real-SQL fixture (Phase 344 TEST-05)", "IClassFixture/IAsyncLifetime"]
key-files:
  created:
    - HcPortal.Tests/ImageCleanupIntegrationTests.cs
  modified:
    - HcPortal.Tests/PackageImageDeleteTests.cs
key-decisions:
  - "Integration test seed user dulu (FK_AssessmentSessions_Users_UserId) — deviation Rule 2"
  - "Mirror D-04 opsi (b): tandai sumber-kebenaran, mirror in-memory dipertahankan sbg fast logic-contract test"
requirements-completed: [SC4-test-uat, SC3-shared-survive]
duration: 35 min
completed: 2026-06-12
---

# Phase 366 Plan 03: Test + UAT Summary

Integration test real-SQL membuktikan helper produksi bekerja end-to-end (orphan deleted SC#2 + shared Pre/Post survives SC#3), mirror divergen direkonsiliasi (D-04), dan UAT browser mengonfirmasi DeleteAssessmentGroup membersihkan file fisik orphan. SC#4 terpenuhi.

**Tasks:** 3 (2 auto + 1 checkpoint human-verify) | **Files:** 2 | **Commits:** 2 code + metadata

## Apa yang dibangun / diverifikasi
- **Task 1 — `ImageCleanupIntegrationTests.cs`** (baru): fixture disposable `HcPortalDB_Test_<guid>` @SQLEXPRESS (pola Phase 344 TEST-05). 2 [Fact] exercise helper PRODUKSI atas real-SQL:
  - `OrphanPath_Deleted_WhenFullCascade` (SC#2) — PASS
  - `SharedPrePostPath_Survives_WhenOneSideDeleted` (SC#3) — PASS
  - **2/2 passed.**
- **Task 2 — rekonsiliasi `PackageImageDeleteTests.cs`** (D-04 opsi b): komentar header di mirror `DeleteIfUnreferenced` menandai *Production source of truth: Helpers/ImageFileCleanup.cs; Integration coverage: ImageCleanupIntegrationTests.cs*. `ApplyIntent`/`PathStillReferenced` (out of scope) utuh. **Full suite 229/229 passed, 0 failed.**
- **Task 3 — UAT browser @localhost:5277** (AD off, Playwright MCP, admin@pertamina.com):
  - **Skenario A (SC#2) — PASS:** buat assessment individual "Pre Test UAT366 Cleanup Orphan" (OJT, Standard, 1 peserta) → package "Paket A" (pkg 63) → soal bergambar (upload PNG → file `wwwroot/uploads/questions/63/20260612032822864_84ef8b90_01-login.png`, DB question 753 ImagePath match) → "Hapus Grup" (jalur DeleteAssessmentGroup). **Hasil:** file fisik TERHAPUS + DB cascade bersih (session/pkg/question = 0). UI sukses, tanpa error.

## Findings UAT
1. ✅ **SC#2 end-to-end confirmed** — DeleteAssessmentGroup endpoint nyata memanggil helper post-commit; file gambar orphan ikut terhapus dari disk; cascade DB tuntas.
2. ℹ️ **Folder kosong `uploads/questions/63/` tetap ada** setelah file terhapus — **EXPECTED, sesuai desain** (CONTEXT Deferred Area 4: cleanup folder kosong out-of-scope). BUKAN defect.
3. ✅ Tidak ada error/exception saat delete. UI flash sukses.
4. SC#3 (shared Pre/Post survive) — tidak diuji ulang via browser (butuh setup Pre-Post SamePackage shared-image + single-side delete, banyak langkah); sudah terbukti rigor di integration test [Fact]2 real-SQL. DeletePrePostGroup memakai helper + pola identik dgn DeleteAssessmentGroup yang sudah browser-verified.

## Deviations from Plan
**[Rule 2 - Missing critical] Seed user di integration test** — Found during: Task 1. Issue: seed AssessmentSession `UserId=""` melanggar `FK_AssessmentSessions_Users_UserId` (FK ke Users tak terdeteksi di grep model awal, ada by convention). Fix: tambah `SeedUserAsync` (buat ApplicationUser minimal: UserName/Email/FullName) sebelum seed session, pass user.Id. Files: `ImageCleanupIntegrationTests.cs`. Verification: 2/2 [Fact] PASS setelah fix. Commit: test(366-03) integration.

**Total deviations:** 1 auto-fixed (missing-critical FK seed). **Impact:** rendah — test-only, ditangkap saat run pertama, fix langsung hijau.

## SEED_WORKFLOW
DB snapshot `C:\Temp\HcPortalDB_Dev_pre366uat_20260612.bak` (1962 pages) sebelum UAT; RESTORE WITH REPLACE setelah selesai (verify UAT366 sessions=0 + folder 63 removed); `docs/SEED_JOURNAL.md` ditandai `cleaned`. Integration test pakai DB disposable per-guid (tak sentuh HcPortalDB_Dev).

## Self-Check: PASSED
- ImageCleanupIntegrationTests.cs ada; 2 [Fact] PASS real-SQL ✓
- Full suite 229/229, 0 failed ✓
- UAT browser SC#2 PASS (file terhapus + DB bersih) ✓
- Mirror D-04 sumber-kebenaran ditandai; ApplyIntent/PathStillReferenced utuh ✓
- DB restored + journal cleaned ✓

**Phase 366 complete — siap verifikasi goal.**
