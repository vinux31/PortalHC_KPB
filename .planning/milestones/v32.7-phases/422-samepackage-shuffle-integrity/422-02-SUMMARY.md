---
phase: 422-samepackage-shuffle-integrity
plan: 02
subsystem: assessment-samepackage
tags: [samepackage, shuffle, sync, lock, inherit, sibling-key, kill-drift, hardening]
requires: [SessionEditLockRules, SyncPackagesToPost, SiblingSessionQuery]
provides: [import-autosync, samepackage-lock-server-side, samepackage-inherit, sibling-type-aware-lock]
affects: [Controllers/AssessmentAdminController.cs]
tech-stack:
  added: []
  patterns: [kill-drift-sync-helper, server-authoritative-guard, type-aware-sibling-predicate]
key-files:
  created:
    - HcPortal.Tests/SamePackageSyncTests.cs
    - HcPortal.Tests/SessionEditLockTests.cs
    - HcPortal.Tests/SamePackageInheritTests.cs
    - HcPortal.Tests/SiblingTypeAwareLockTests.cs
  modified:
    - Controllers/AssessmentAdminController.cs
key-decisions:
  - "SyncToLinkedPostIfSamePackageAsync(preSessionId) ekstrak dari 5 blok copy-paste + wire ke 6 jalur incl Import terminal yang BOCOR (SHUF-ISS-03 HIGH). Kill-drift: satu sumber sync."
  - "IsSessionEditLocked guard di AWAL 5 endpoint POST (CreatePackage/DeletePackage/CreateQuestion/EditQuestion/DeleteQuestion) + Import — tolak keras TempData+redirect (server-authoritative, bukan view-only)."
  - "Import (Pitfall 3): IsSessionEditLocked guard di AWAL (tolak Import langsung ke Post terkunci), SyncToLinkedPostIfSamePackageAsync di AKHIR (sync Pre->Post setelah import Pre)."
  - "SHFX-06 scope LOCK-ONLY (Pitfall 4): sibling key type-aware HANYA untuk lock-detection (lockSiblingIds); propagation write UpdateShuffleSettings (propagationSiblingIds) TIDAK diubah — test assert propagation tak regresi."
  - "newPost tambah-peserta warisi SamePackage = repPost.SamePackage (SHFX-04/PA-02)."
requirements-completed: [SHFX-01, SHFX-03, SHFX-04, SHFX-06]
duration: 1 sesi
completed: 2026-06-23
---

# Phase 422 Plan 02: Sync + Lock + Inherit + Sibling Key Summary

Wave 2 WIRING: konsolidasi auto-sync SamePackage ke satu helper + tutup jalur Import bocor (SHUF-ISS-03 HIGH), lock server-side 5 endpoint (SHUF-ISS-02), pewarisan SamePackage peserta baru (PA-02), dan sibling key type-aware untuk lock-detection (SHUF-ISS-01). Semua di `AssessmentAdminController.cs`.

**Durasi:** 1 sesi · **Task:** 3 · **File:** 5 (4 test baru + 1 controller).

## Yang dibangun

- **Task 1 (SHFX-01/SHUF-ISS-03 HIGH):** ekstrak `SyncToLinkedPostIfSamePackageAsync(int preSessionId)` dari 5 blok copy-paste "Pre && linkedPost.SamePackage → SyncPackagesToPost" + wire ke 6 jalur termasuk **terminal Import yang BOCOR** (`:6483`). grep call-site ≥ 6 (kill-drift). `SamePackageSyncTests` (Import sync vs no-sync + helper no-op guards).
- **Task 2 (SHFX-03/SHUF-ISS-02):** guard `SessionEditLockRules.IsSessionEditLocked` di AWAL 5 endpoint POST paket/soal + Import → tolak keras `TempData["Error"]` + redirect (server-authoritative). Import: guard di awal (Pitfall 3). `SessionEditLockTests` (reject saat locked / lolos saat tak-locked, no-write).
- **Task 3 (SHFX-04 + SHFX-06):** newPost tambah-peserta warisi `SamePackage = repPost.SamePackage`. Sibling key type-aware (`SiblingSessionQuery`/predicate) HANYA untuk lock-detection (`lockSiblingIds`) — propagation write `UpdateShuffleSettings` (`propagationSiblingIds`) TIDAK diubah (Pitfall 4 scope guard). `SamePackageInheritTests` + `SiblingTypeAwareLockTests` (Pre mulai → Post tidak terkunci + propagation tak regresi).

## Verifikasi

- `dotnet build` → **0 error**.
- `dotnet test` 4 file Wave-2 (`SamePackageSync`/`SessionEditLock`/`SamePackageInherit`/`SiblingTypeAwareLock`) → **15/15 hijau**.
- grep: `SyncToLinkedPostIfSamePackageAsync` = 7 (1 def + 6 call), `IsSessionEditLocked` = 6 (5 endpoint + Import/helper).
- Tidak ada regresi Wave-1 (helpers + migration).

## Deviations from Plan

**[Recovery — executor crash mid-finalize]** Executor menyelesaikan + commit ke-3 task (d8b2ca76, c13576c3, 85be14be) tetapi koneksi putus SEBELUM menulis SUMMARY.md + update STATE/ROADMAP. Orchestrator memverifikasi ulang (build 0-err + 15/15 test hijau, tree clean) lalu menulis SUMMARY + update tracking. Tidak ada perubahan scope/kode — hanya finalisasi metadata.

## Self-Check: PASSED

- key-files (4 test + controller) ada di disk + ter-commit (3 commit feat 422-02).
- Acceptance criteria 3 task PASS (grep counts + test exit 0).
- Verifikasi re-run hijau (build 0-err + 15 integration).

## Issues Encountered

Executor connection-closed mid-finalize (kedua kalinya di fase ini) — kerja kode utuh + ter-commit, hanya metadata finalisasi yang diselesaikan orchestrator.

## Next

Ready for **422-03** (SHFX-02 toggle SamePackage endpoint + UI render warning/lock/mismatch + checkpoint UAT @5270). 422-03 panggil `SyncToLinkedPostIfSamePackageAsync` (Wave 2) di ON-path + `IsSessionEditLocked` (Wave 1) di view friendly-disable.

## Merge Reconciliation (v32.6 branch main Scoped-Shuffle)

- Sibling key type-aware (`lockSiblingIds` vs `propagationSiblingIds`) + `SyncToLinkedPostIfSamePackageAsync` ekstraksi = touchpoint merge. Jangan tarik kode Scoped-Shuffle dari main; rekonsiliasi manual saat merge.
