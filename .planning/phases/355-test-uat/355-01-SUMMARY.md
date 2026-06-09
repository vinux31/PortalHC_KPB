---
phase: 355-test-uat
plan: 01
subsystem: testing
tags: [testing, image-upload, xunit, file-delete]
requires: []
provides: ["xUnit replace-delete-on-disk coverage"]
affects: ["HcPortal.Tests"]
tech-stack:
  added: []
  patterns: ["logic-mirror in-memory xUnit (temp-dir + helper reuse)"]
key-files:
  created: []
  modified: ["HcPortal.Tests/PackageImageDeleteTests.cs"]
key-decisions:
  - "Gap D-02 ditutup dengan 1 [Fact] logic-mirror (reuse helper existing), bukan integration test controller — patuh Deferred."
requirements-completed: [TST-01]
duration: 6 min
completed: 2026-06-09
---

# Phase 355 Plan 01: xUnit gap-audit (replace-delete-on-disk) Summary

Menutup satu-satunya gap nyata di suite xUnit gambar v24.0 (TST-01, D-02): bukti bahwa saat gambar di-REPLACE, file LAMA benar-benar di-`File.Delete` dari disk — bukan sekadar masuk delete-candidate list (yang sudah dibuktikan `ReplaceConflict_NewFileWins_OverRemoveCheckbox`).

## What Was Built

- **1 `[Fact]` baru** `Replace_NewFileWins_DeletesOldFileOnDisk` di `PackageImageDeleteTests.cs` — menulis `old.jpg`+`new.jpg` nyata ke temp dir, jalankan `ApplyIntent`(file baru) → loop `DeleteIfUnreferenced`, lalu assert `File.Exists(old)==false` + `File.Exists(new)==true` + `target.ImagePath==newPath`.
- Reuse helper existing `MakeTempDir`/`ApplyIntent`/`DeleteIfUnreferenced` — ZERO helper baru, ZERO churn ke 3 file tes existing.

## Tasks

- **Task 1 (baseline gate):** `PackageImage` filter 10 passed, `ValidateImageFile` filter 8 passed — 3 file tes gambar existing hijau (bukan Skip). File unchanged. Run-only, no commit.
- **Task 2 (+[Fact]):** tambah `Replace_NewFileWins_DeletesOldFileOnDisk`, filter exits 0 (1 passed). Commit `a0f8ad42`.
- **Task 3 (post-add gate):** `PackageImage` filter penuh 11 passed (10 baseline + 1 baru), no regression.

## Verification

- `dotnet test --filter "FullyQualifiedName~PackageImage"` → Passed 11 / Failed 0.
- `dotnet test --filter "FullyQualifiedName~FileUploadHelperTests.ValidateImageFile"` → Passed 8 / Failed 0.
- git diff = HANYA tambahan 1 [Fact] di `PackageImageDeleteTests.cs` (no production code, no churn).

## Deviations from Plan

None - plan executed exactly as written. (Signature `ApplyIntent` diverifikasi cocok template sebelum copy — sesuai instruksi action.)

## Issues Encountered

None. (git warning LF→CRLF kosmetik, normal di Windows.)

## Self-Check: PASSED

- key-files modified exist + committed ✓
- `git log --grep="355-01"` → commit `a0f8ad42` present ✓
- all acceptance_criteria re-run green ✓

Ready for 355-02 (Playwright spec). TST-01 xUnit konsolidasi: substansi selesai; final full-suite gate di 355-03.
