# Phase 352 Plan 01 — Summary

**Plan:** 352-01 Data Foundation + Image-Only Upload
**Status:** ✅ COMPLETE
**Date:** 2026-06-06
**REQ:** IMG-04

## What shipped

1. **Entity** (`Models/AssessmentPackage.cs`) — 4 properti nullable: `PackageQuestion.ImagePath` + `ImageAlt`, `PackageOption.ImagePath` + `ImageAlt`. `ImageAlt` `[MaxLength(255)]`.
2. **Konstanta image-only** (`Models/AssessmentConstants.cs`) — `MaxImageFileSizeBytes = 5 * 1024 * 1024` (5MB, D-03) + `AllowedImageExtensions {.jpg,.jpeg,.png}` (D-01/02, TANPA .pdf). MagicBytes dict TIDAK bertambah (reuse).
3. **Validasi** (`Helpers/FileUploadHelper.cs`) — method baru `ValidateImageFile(IFormFile?)` (D-05 terpisah, `ValidateCertificateFile` tak diubah). Tolak PDF/non-image/oversize, terima JPG/PNG, null/empty→valid. `read<3` guard + `stream.Position=0` identik.
4. **Test** (`HcPortal.Tests/FileUploadHelperTests.cs`) — 8 [Fact] ValidateImageFile (null, empty, JPG, PNG, JPEG, PDF-reject, exe-magic-reject, oversize-5MB).
5. **Migration** `20260606030844_AddImageToPackageQuestionAndOption` — 4 kolom nullable (ImageAlt nvarchar(255), ImagePath nvarchar(max) × 2 tabel), Down() simetris, snapshot terupdate.

## Verifikasi (lokal)

- `dotnet build` → Build succeeded, 0 error
- `dotnet test --filter ValidateImageFile` → 8/8 Passed
- `dotnet test` full → **120/120 Passed** (8 baru + 112 existing, 0 regresi cert helper)
- `dotnet ef database update` → **Done.** (applied ke DB Dev `HcPortalDB_Dev` SQLEXPRESS lokal)
- DB snapshot pre-apply: `C:/Temp/HcPortalDB_Dev_pre352_*.bak` (1930 pages)

## IT-NOTIFY ⚠️

- **Migration baru: `20260606030844_AddImageToPackageQuestionAndOption`** — flag migration = **TRUE**
- Non-destruktif (4 kolom nullable, data lama aman)
- Promosi Dev/Prod = tanggung jawab IT (commit hash di-isi orchestrator saat commit)

## Scope guard

Hanya migration + entity + helper + test. TIDAK ada controller/form/render/sync — itu phase 353-355. Belum ada call-site untuk `ValidateImageFile` (dipakai phase 353).

## Threat model

6 STRIDE threat (T-352-01..06), 4 HIGH semua mitigate (magic-byte, allowlist tanpa PDF, 5MB cap, SVG excluded), T-352-06 (migration) accept via nullable + snapshot. ASVS L1 block-on-high terpenuhi.
