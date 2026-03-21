---
phase: 217
plan: 01
subsystem: CMP/RecordsTeam
tags: [bugfix, dropdown, master-data, filter]
dependency_graph:
  requires: []
  provides: [category-dropdown-from-master]
  affects: [Views/CMP/RecordsTeam.cshtml, Controllers/CMPController.cs]
tech_stack:
  added: []
  patterns: [ViewBag JSON serialization, JS IIFE DOM population]
key_files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/RecordsTeam.cshtml
decisions:
  - "MasterCategoriesJson reuse allCats query yang sudah ada di RecordsTeam action — tidak ada query tambahan"
  - "JS IIFE populate category dropdown agar konsisten dengan pola SubCategoryMap yang sudah ada"
  - "data-categories per worker row tetap dari actual records (bukan dari master) agar filter berfungsi"
metrics:
  duration: "5 minutes"
  completed_date: "2026-03-21"
  tasks_completed: 1
  files_modified: 2
---

# Phase 217 Plan 01: Fix Category Dropdown RecordsTeam Summary

**One-liner:** Dropdown Category di RecordsTeam kini mengambil dari master AssessmentCategories via ViewBag JSON, menggantikan union string dari records.

## What Was Built

RecordsTeam action di CMPController diperkaya dengan `ViewBag.MasterCategoriesJson` yang berisi list nama kategori dari tabel `AssessmentCategories` (reuse `allCats` query yang sudah ada). Di View, blok Razor union `trainingCats + assessmentCats` dihapus dan diganti dengan JS IIFE yang populate dropdown dari master JSON.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Tambah MasterCategoriesJson di Controller dan ganti dropdown source di View | 9f835c9 | CMPController.cs, RecordsTeam.cshtml |

## Decisions Made

1. **Reuse allCats query:** `ViewBag.MasterCategoriesJson` menggunakan `allCats` yang sudah di-query untuk SubCategoryMap — tidak ada round-trip database tambahan.
2. **JS IIFE pattern:** Konsisten dengan pola yang sudah ada di codebase (Phase 214).
3. **data-categories tetap dari records:** Per D-06, `data-categories` attribute per worker row tetap dari actual TrainingRecord/AssessmentSession agar filter `matchCategory` berfungsi benar.

## Deviations from Plan

None — plan dieksekusi persis sesuai instruksi.

## Known Stubs

None.

## Self-Check: PASSED

- Controllers/CMPController.cs contains `MasterCategoriesJson`: FOUND
- Views/CMP/RecordsTeam.cshtml contains `masterCategories`: FOUND
- Views/CMP/RecordsTeam.cshtml no longer contains `trainingCats`/`assessmentCats` (union logic): CONFIRMED
- Views/CMP/RecordsTeam.cshtml still contains `data-categories`: FOUND
- Views/CMP/RecordsTeam.cshtml still contains `filterTeamTable`: FOUND
- Views/CMP/RecordsTeam.cshtml still contains `SubCategoryMapJson`: FOUND
- `dotnet build` exits 0: CONFIRMED (0 Errors, 72 Warnings)
- Commit 9f835c9: FOUND
