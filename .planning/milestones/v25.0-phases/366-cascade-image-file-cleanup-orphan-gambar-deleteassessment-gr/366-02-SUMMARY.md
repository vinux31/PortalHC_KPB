---
phase: 366-cascade-image-file-cleanup-orphan-gambar-deleteassessment-gr
plan: 02
subsystem: assessment-admin
tags: [cascade-delete, image-cleanup, atomic-file-delete]
requires:
  - "Helpers/ImageFileCleanup.DeleteUnreferencedAsync (Plan 01)"
provides:
  - "Image file cleanup terpasang di DeleteAssessment/DeleteAssessmentGroup/DeletePrePostGroup (post-commit)"
affects:
  - Controllers/AssessmentAdminController.cs
tech-stack:
  added: []
  patterns: ["collect-before-RemoveRange + helper-after-CommitAsync (Phase 333)", "post-commit AnyAsync batch-aware ref-count (D-05)"]
key-files:
  created: []
  modified:
    - Controllers/AssessmentAdminController.cs
key-decisions:
  - "Batch-aware via post-commit AnyAsync (D-05) — TANPA exclusion-set; baris batch sudah terhapus → AnyAsync false → file orphan dihapus, shared survive"
  - "logger LOKAL (bukan _logger) di 3 Delete* method; _env field kelas; allPackages sudah Include nested → tak perlu Include baru"
requirements-completed: [SC2-cascade-install, SC3-shared-survive]
duration: 7 min
completed: 2026-06-12
---

# Phase 366 Plan 02: Install Image Cleanup di 3 Cascade Delete* Summary

Pasang `ImageFileCleanup.DeleteUnreferencedAsync` (Plan 01) di 3 method cascade-delete `AssessmentAdminController.cs` dgn pola atomic Phase 333: kumpul `ImagePath` Distinct dari packages SEBELUM `RemoveRange`, panggil helper SETELAH `tx.CommitAsync`. Tutup orphan disk file (SC#2) + jamin shared Pre↔Post selamat (SC#3) gratis via post-commit `AnyAsync` (D-05, tanpa exclusion-set).

**Tasks:** 2 | **Files:** 1 (modifikasi) | **Commits:** 1 (3 install kohesif)

## Apa yang dibangun
- **Task 1 — DeleteAssessment** (`:2189`): collect `var imagePaths` (SelectMany Questions+Options ImagePath, Where !empty, Distinct) sisip setelah `packages.ToListAsync()`, SEBELUM RemoveRange. Helper call sisip setelah `tx.CommitAsync()`, label "DeleteAssessment image", pakai `logger` lokal.
- **Task 2 — DeleteAssessmentGroup** (`:2377`, multi-sibling batch) + **DeletePrePostGroup** (`:2563`, Pre+Post 1 batch): pola sama, collect dari `allPackages` sebelum RemoveRange, helper post-commit, label "DeleteAssessmentGroup image" / "DeletePrePostGroup image", `logger` lokal.
- DeletePrePostGroup = kasus shared paling rawan: Pre+Post dihapus 1 batch → post-commit AnyAsync false → file shared dihapus benar (memang harus, tak ada sisi lain). Beda dari single-side delete di Plan 01 (Post survive → file selamat). Tanpa exclusion-set.

## Verifikasi
- `dotnet build` exit 0 (Build succeeded).
- Grep: `ImageFileCleanup.DeleteUnreferencedAsync` total **6×** (3 swap Plan 01 + 3 install Plan 02); 3 label install masing-masing 1×; `var imagePaths` 3×; `excludeSet/exclusionSet` 0× (D-05 murni post-commit AnyAsync).
- Ordering per method (by construction): collect < RemoveRange < CommitAsync < helper call.
- Verifikasi fungsional end-to-end (file fisik terhapus / shared selamat) = Plan 03 (integration test + UAT).

## Deviations from Plan
None - plan executed exactly as written. Anchor line-ref Plan 02 (L2xxx) tak tergeser oleh Plan 01 (edit di L5xxx-6xxx) — semua collect/commit anchor cocok persis.

## Self-Check: PASSED
- AssessmentAdminController.cs modified ✓; `git log --grep="366-02"` ≥1 commit ✓
- Acceptance criteria re-run PASS (6 helper call, 3 label, 0 exclusion-set, build hijau) ✓

**Next:** Ready for 366-03 (integration test real-SQL + rekonsiliasi mirror + UAT @5277 — checkpoint plan).
