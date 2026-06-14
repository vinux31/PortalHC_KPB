---
phase: 353-admin-backend-gambar-crud-sync-atomic-delete
plan: 01
subsystem: assessment-admin-backend
tags: [sync, test-scaffold, shared-file, nyquist]
requires: []
provides:
  - SyncPackagesToPost copy ImagePath+ImageAlt (SYN-01)
  - test harness ref-count (D-10) + DeletePackage path-collect (D-11)
affects:
  - Controllers/AssessmentAdminController.cs
tech-stack:
  added: []
  patterns: [shared-file-string-copy, refcount-predicate, atomic-delete-loop-mirror]
key-files:
  created:
    - HcPortal.Tests/PackageImageSyncTests.cs
    - HcPortal.Tests/PackageImageDeleteTests.cs
  modified:
    - Controllers/AssessmentAdminController.cs
key-decisions:
  - "Test SYN-01 pakai mirror deep-clone pure in-memory (SyncPackagesToPost private) — komentar keep-in-sync"
  - "Test ref-count/path-collect inline-logic (mirror SHARED-1/2), no Skip permanen — predikat GREEN sekarang"
requirements-completed: [SYN-01]
duration: ~15 min
completed: 2026-06-08
---

# Phase 353 Plan 01: Sync ImagePath + Test Scaffold Summary

Menyelesaikan SYN-01 (SyncPackagesToPost menyalin `ImagePath`+`ImageAlt` soal & opsi Pre→Post sebagai shared-file string copy, tanpa file op) dan membangun 2 file test Nyquist: sync (SYN-01, GREEN) + scaffold ref-count/DeletePackage (D-10/D-11) yang menetapkan kontrak untuk Plan 02/03.

## Tasks
1. **SyncPackagesToPost copy** — sisip 4 baris (`ImagePath`/`ImageAlt` soal @L5378, opsi @L5384) di blok deep-clone. RemoveRange L5347-5351 tak diubah, no File.Delete. Commit `384ea30f`.
2. **PackageImageSyncTests.cs** — 4 [Fact] mirror clone: copy soal, copy opsi, shared-path identity (`Assert.Same`), null-safe. Commit `3ace46ee`.
3. **PackageImageDeleteTests.cs** scaffold — 3 [Fact]: ref-count POSITIF (skip), NOL (delete), DeletePackage path-collect union. inline-logic, temp-dir try/finally, no Skip. Commit `871e2401`.

## Verification
- `dotnet build HcPortal.Tests` → 0 errors (23 warnings pre-existing).
- `dotnet test --filter ~Sync` → 15 passed (4 baru + 11 existing).
- `dotnet test --filter ~RefCount|~DeletePackageImage` → 3 passed.
- Full suite: **127 passed / 0 failed** (120 baseline + 7 baru).

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None. (Catatan operasional: stale `index.lock` worktree dibersihkan 2x saat commit — bukan masalah kode.)

## Next Phase Readiness

Ready for 353-02 (CRUD gambar — CreateQuestion/EditQuestion + OQ1 option-preserve). Test scaffold ref-count siap dipakai sebagai feedback loop logika berisiko.
