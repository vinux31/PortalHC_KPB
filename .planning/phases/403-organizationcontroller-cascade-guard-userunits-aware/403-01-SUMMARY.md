---
phase: 403-organizationcontroller-cascade-guard-userunits-aware
plan: 01
subsystem: organization
tags: [organization, cascade, userunits, multi-unit, guard, tdd]
requires: [UserUnits junction (Phase 399), GetSectionUnitsDictAsync]
provides: [UserUnits-aware rename cascade, reparent split-block, secondary-membership delete/deactivate guard, affectedUserUnitsCount, EditOrganizationUnit transaction]
affects: [OrganizationController]
tech-stack:
  added: []
  patterns: [BeginTransactionAsync wrap (idiom WorkerController.cs:665), correlated _context.UserUnits query, parity rule rename/preview no-IsActive vs guard with-IsActive]
key-files:
  created:
    - .planning/phases/403-organizationcontroller-cascade-guard-userunits-aware/403-01-SUMMARY.md
  modified:
    - Controllers/OrganizationController.cs
    - HcPortal.Tests/OrganizationControllerTests.cs
key-decisions:
  - "Rename + preview count UserUnits TANPA filter IsActive (parity Sub-1==Sub-4); guard + split-detect DENGAN && uu.IsActive"
  - "Mirror Users.Unit baris primary tetap konsisten karena rename tak menyentuh IsPrimary (Invariant #3)"
  - "Split-block return-early di dalam tx scope = dispose tanpa Commit = rollback; recompute mirror INLINE (D-04a, tak panggil helper WorkerController)"
  - "Test 5 (single-unit reparent allowed) hijau sejak RED = regression guard atas perilaku existing, bukan TDD failure"
requirements-completed: [ORG-01, ORG-02]
duration: ~25 min
completed: 2026-06-19
---

# Phase 403 Plan 01: OrganizationController UserUnits-Aware Cascade/Guard Summary

Membuat `OrganizationController` sadar junction `UserUnits` (fondasi Phase 399) di 4 operasi yang sebelumnya hanya menyentuh scalar `Users.Unit`/`Users.Section`: rename cascade, reparent split-detect hard-block, delete+deactivate guard, dan `PreviewEditCascade.affectedUserUnitsCount` — plus membungkus seluruh cascade `EditOrganizationUnit` dalam `BeginTransactionAsync`. TDD RED→GREEN.

**Duration:** ~25 min | **Tasks:** 2 | **Files:** 2 modified

## What Was Built

- **Task 1 (RED, commit `02d32d6f`):** 6 test ORG-01/02 di `OrganizationControllerTests.cs` + helper `GetMessage` + suppress `TransactionIgnoredWarning` di `MakeController()` (Pitfall 1, prep tx wrap). 5 test new-behavior RED; Test 5 (single-unit reparent allowed) hijau = regression guard.
- **Task 2 (GREEN, commit `0994c948`):** 6 sub-perubahan di `OrganizationController.cs`:
  - Sub-1: rename Level>=1 cascade ke SEMUA baris `UserUnits.Unit==oldName` (incl sekunder & IsActive=false), `IsPrimary` tak disentuh → mirror `Users.Unit` konsisten.
  - Sub-2: reparent split-detect hard-BLOCK via `GetSectionUnitsDictAsync` — blok HANYA bila pekerja terpecah >1 Bagian; pesan sebut NIP/FullName (max 5 + suffix).
  - Sub-3: `using var tx = await _context.Database.BeginTransactionAsync()` + `await tx.CommitAsync()` membungkus Level-recompute→cascade→SaveChanges.
  - Sub-4: `affectedUserUnitsCount` di `PreviewEditCascade` (Level>=1, TANPA IsActive = match Sub-1 → preview==actual).
  - Sub-5/6: `DeleteOrganizationUnit` + `ToggleOrganizationUnitActive` scan `UserUnits.AnyAsync(uu => uu.Unit==name && uu.IsActive)`, pesan spesifik "sekunder" saat dampak murni membership sekunder.

## Verification

- `dotnet build` 0 error.
- `dotnet test --filter "FullyQualifiedName~OrganizationController"` → 14/14 pass.
- `dotnet test --nologo` full suite → **532 passed, 0 failed, 5 skipped** (SQLEXPRESS-gated, milik Phase 404).
- Grep acceptance: rename no-IsActive ×1, guard with-IsActive ×2, affectedUserUnitsCount ×1, GetSectionUnitsDictAsync ×1, BeginTransactionAsync/CommitAsync ×1, "sekunder" ×5, "terpecah" ×2, Authorize Admin,HC ×9, ValidateAntiForgeryToken ×7, nav-prop `u.UserUnits` ×0.

## Deviations from Plan

**[Regression-guard, not a defect] Test 5 hijau sejak RED** — `EditOrganizationUnit_ReparentSingleUnitWorker_Allowed` menguji perilaku reparent Section cascade yang SUDAH ada di kode existing, jadi lulus sebelum implementasi split-block. Plan acceptance optimistis "6 test RED"; aktual 5 RED + 1 regression-guard. Tidak dipaksa gagal artifisial (praktik TDD lazim untuk guard test). Tidak ada dampak.

**Total deviations:** 1 (klarifikasi, bukan fix). **Impact:** none.

## Issues Encountered

None.

## Next Phase Readiness

Ready for **Plan 403-02** — UI cascade-confirm modal baris ke-5 `cascadeUserUnits`, mengonsumsi field JSON `affectedUserUnitsCount` yang dipancarkan plan ini (`PreviewEditCascade`). Plan 02 berisi checkpoint UAT browser (autonomous: false).

## Self-Check: PASSED
- key-files.modified ada di disk (OrganizationController.cs, OrganizationControllerTests.cs) ✓
- `git log --grep="403-01"` → 2 commit (RED test + GREEN feat) ✓
- Semua acceptance_criteria Task 1 & Task 2 re-run PASS ✓
- Plan-level `<verification>` re-run: build 0 error, OrganizationController 14/14, full suite 532/0/5 ✓
