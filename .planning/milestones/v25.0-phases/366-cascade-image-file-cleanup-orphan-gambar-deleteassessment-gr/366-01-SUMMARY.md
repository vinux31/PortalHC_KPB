---
phase: 366-cascade-image-file-cleanup-orphan-gambar-deleteassessment-gr
plan: 01
subsystem: assessment-admin
tags: [refactor, helper-extraction, image-cleanup]
requires: []
provides:
  - "Helpers/ImageFileCleanup.DeleteUnreferencedAsync (static ref-count + File.Delete warn-only)"
affects:
  - Controllers/AssessmentAdminController.cs
tech-stack:
  added: []
  patterns: ["static helper (HcPortal.Helpers)", "post-commit AnyAsync ref-count"]
key-files:
  created:
    - Helpers/ImageFileCleanup.cs
  modified:
    - Controllers/AssessmentAdminController.cs
key-decisions:
  - "Helper static di Helpers/ (D-01), DbContext type = ApplicationDbContext (bukan AppDbContext keliru di CONTEXT)"
  - "3 call-site (DeletePackage/EditQuestion POST/DeleteQuestion) pakai field _logger; posisi panggilan tetap post-sync (D-02)"
requirements-completed: [SC1-helper-extract]
duration: 8 min
completed: 2026-06-12
---

# Phase 366 Plan 01: Ekstrak Helper ImageFileCleanup Summary

Ekstrak helper static `ImageFileCleanup.DeleteUnreferencedAsync` (ref-count `AnyAsync` Q+O + `File.Delete` warn-only, pola Phase 333) dari 3 blok inline byte-identik di `AssessmentAdminController.cs`, lalu swap ketiga call-site lama (`DeletePackage`, `EditQuestion` POST, `DeleteQuestion`) jadi 1 await call. Perilaku identik (SC#1) — 1 sumber kebenaran ref-count, fondasi untuk Plan 02.

**Tasks:** 2 | **Files:** 2 (1 baru, 1 modifikasi) | **Commits:** 2

## Apa yang dibangun
- **Task 1:** `Helpers/ImageFileCleanup.cs` baru — static, namespace `HcPortal.Helpers`, signature `DeleteUnreferencedAsync(ApplicationDbContext ctx, string webRootPath, ILogger logger, IEnumerable<string> paths, string source="")`. Guard `IsNullOrEmpty`, ref-count `PackageQuestions.AnyAsync` + `PackageOptions.AnyAsync`, `Path.Combine` confined webroot, try/catch warn-only. Build 0 error.
- **Task 2:** 3 loop `foreach (var relUrl in imagePathsToDelete.Distinct())` di controller dikompres jadi 1 await call masing-masing (label source dipertahankan: "DeletePackage image" / "question image" / "DeleteQuestion image"). Komentar ordering di atas loop dibiarkan sebagai dokumentasi.

## Verifikasi
- `dotnet build` exit 0 (Build succeeded).
- Grep: inline predikat `_context.PackageQuestions.AnyAsync(x => x.ImagePath` = 0× (turun dari 3); `ImageFileCleanup.DeleteUnreferencedAsync` = 3×; 3 label source masing-masing 1×.
- `dotnet test --filter PackageImageDeleteTests|PackageImageSyncTests` = **11/11 passed, 0 failed** (101 ms) — SC#1 perilaku identik terbukti (sync Pre→Post tak regresi).

## Deviations from Plan
None - plan executed exactly as written.

(Catatan trivial: acceptance criterion Task 1 "AnyAsync cocok 2×" — `grep -c` melaporkan 3× karena kata "AnyAsync" muncul 1× di doc-comment + 2× guard call. Dua guard call Q+O terkonfirmasi ada; intent kriteria terpenuhi.)

## Self-Check: PASSED

- Helpers/ImageFileCleanup.cs ada di disk ✓
- `git log --grep="366-01"` ≥1 commit ✓ (2 commit: feat helper + refactor swap)
- Acceptance criteria semua re-run PASS ✓
- Build + test hijau ✓

**Next:** Ready for 366-02 (pasang helper di 3 method Delete*).
