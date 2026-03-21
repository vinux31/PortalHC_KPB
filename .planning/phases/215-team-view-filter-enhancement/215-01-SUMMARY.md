---
phase: 215-team-view-filter-enhancement
plan: "01"
subsystem: CMP Records Team View
tags: [filter, team-view, assessment, sub-category, client-side-js]
dependency_graph:
  requires: [214-02]
  provides: [FLT-04]
  affects: [Views/CMP/RecordsTeam.cshtml, Services/WorkerDataService.cs, Models/WorkerTrainingStatus.cs]
tech_stack:
  added: []
  patterns: [batch-query-pattern, data-attributes-filter, dependent-dropdown-js, razor-to-js-json]
key_files:
  created: []
  modified:
    - Models/WorkerTrainingStatus.cs
    - Services/WorkerDataService.cs
    - Controllers/CMPController.cs
    - Views/CMP/RecordsTeam.cshtml
decisions:
  - "Sub Category filter hanya dari TrainingRecord.SubKategori — AssessmentSession tidak punya field SubKategori"
  - "Category dropdown di-build dari training UNION assessment records yang ada di data (bukan semua DB categories)"
  - "Exact-match (split+compare) dipakai untuk sub category filter agar tidak ada false positive substring match"
  - "Status Sudah dengan subCategory aktif: cek data-completed-subcategories bukan data-completed-categories"
metrics:
  duration: "~15 menit"
  completed_date: "2026-03-21"
  tasks_completed: 2
  files_changed: 4
---

# Phase 215 Plan 01: Team View Filter Enhancement Summary

Assessment records dimasukkan ke data filterable Team View dan dropdown Sub Category dependent ditambahkan — filter Category sekarang gabungkan training dan assessment, drill-down Sub Category berfungsi client-side.

## What Was Built

### Task 1: Backend (3 file dimodifikasi)

**Models/WorkerTrainingStatus.cs** — Tambah property:
```csharp
public List<AssessmentSession> AssessmentSessions { get; set; } = new List<AssessmentSession>();
```

**Services/WorkerDataService.cs** — Ubah batch query: sebelumnya hanya load count passed assessments, sekarang load semua AssessmentSessions dan derive passedAssessmentLookup dari sana. Set `worker.AssessmentSessions` per-user di foreach loop.

**Controllers/CMPController.cs** — Tambah di RecordsTeam action: query AssessmentCategories hierarchy (ParentId == null + Include Children), build `subCategoryMap` dictionary, serialize ke `ViewBag.SubCategoryMapJson`.

### Task 2: Frontend (1 file dimodifikasi)

**Views/CMP/RecordsTeam.cshtml:**
- Category dropdown: gabungkan `TrainingRecords.Kategori UNION AssessmentSessions.Category`
- Row 1 filter: ubah dari 3 kolom (col-md-4) menjadi 4 kolom (col-md-3) — tambah dropdown Sub Category
- Sub Category dropdown: `disabled` by default, di-enable + di-populate saat Category dipilih
- Data attributes per worker row: tambah `data-subcategories` dan `data-completed-subcategories`, update `data-categories` untuk include assessment categories
- JS `subCategoryMap` dari `ViewBag.SubCategoryMapJson`
- `filterTeamTable()`: tambah `matchSubCategory` exact-match, `matchStatus` aware subCategory aktif
- `resetTeamFilters()`: reset dan disable `subCategoryFilter`
- `updateExportLinks()`: tambah `subCategory` ke URL params

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 | 61cc48b | Backend: AssessmentSessions model + service + controller |
| 2 | f7328c9 | Frontend: SubCategory dropdown + data attributes + JS filter |

## Deviations from Plan

None — plan dieksekusi exactly as written.

## Known Stubs

None — semua data wired dari database.

## Self-Check

- [x] Models/WorkerTrainingStatus.cs — property `AssessmentSessions` ada
- [x] Services/WorkerDataService.cs — `assessmentSessionLookup` ada
- [x] Controllers/CMPController.cs — `SubCategoryMapJson` ada
- [x] Views/CMP/RecordsTeam.cshtml — `subCategoryFilter`, `data-subcategories`, `assessmentCats`, `subCategoryMap`, `matchSubCategory`, `AssessmentSessions` semua ada
- [x] Build: 0 CS compile errors (MSB3027 adalah file lock dari running process, bukan compile error)

## Self-Check: PASSED
