---
phase: 214-subcategory-model-crud
plan: "01"
subsystem: training-records
tags: [model, migration, viewmodel, controller, import]
dependency_graph:
  requires: []
  provides: [TrainingRecord.SubKategori, SetTrainingCategoryViewBag, import-subcategory-column]
  affects: [AdminController, TrainingRecord, CreateTrainingRecordViewModel, EditTrainingRecordViewModel]
tech_stack:
  added: []
  patterns: [EF Core migration, ViewBag category options, nullable string field]
key_files:
  created:
    - Migrations/20260321080029_AddSubKategoriToTrainingRecord.cs
    - Migrations/20260321080108_AddSubKategoriColumn.cs
  modified:
    - Models/TrainingRecord.cs
    - Models/CreateTrainingRecordViewModel.cs
    - Models/EditTrainingRecordViewModel.cs
    - Controllers/AdminController.cs
decisions:
  - "Dua migration dibuat: pertama kosong (empty Up karena --no-build dengan binary lama), kedua berisi AddColumn yang benar — kolom SubKategori berhasil ditambahkan ke database"
  - "EditTraining POST tidak memanggil SetTrainingCategoryViewBag karena seluruh error path redirect ke ManageAssessment (tidak ada return View)"
metrics:
  duration: "12 menit"
  completed: "2026-03-21"
  tasks: 2
  files: 6
---

# Phase 214 Plan 01: SubKategori Model + Controller Summary

**One-liner:** Tambah nullable SubKategori field ke TrainingRecord model, ViewModels, EF Core migration, dan seluruh AdminController flow (ViewBag, POST handlers, import template + logic).

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Model + Migration + ViewModel SubKategori | 0dca111 | TrainingRecord.cs, CreateTrainingRecordViewModel.cs, EditTrainingRecordViewModel.cs, 2 migration files |
| 2 | Controller ViewBag, POST handlers, Import logic | 5d04ab9 | Controllers/AdminController.cs |

## What Was Built

### Task 1 — Model + Migration + ViewModel

- `Models/TrainingRecord.cs`: tambah `public string? SubKategori { get; set; }` setelah field `Kota`
- `Models/CreateTrainingRecordViewModel.cs`: tambah `[Display(Name = "Sub Kategori")] public string? SubKategori { get; set; }` setelah field `Kategori`
- `Models/EditTrainingRecordViewModel.cs`: idem
- Migration `AddSubKategoriToTrainingRecord`: placeholder (dibuat dengan binary lama, Up() kosong)
- Migration `AddSubKategoriColumn`: berisi `migrationBuilder.AddColumn<string>("SubKategori", "TrainingRecords", nullable: true)` — kolom berhasil ditambahkan ke database

### Task 2 — Controller

- **Helper method** `SetTrainingCategoryViewBag()`: query AssessmentCategories untuk KategoriOptions (ParentId==null, IsActive) dan SubKategoriOptions (ParentId!=null, IsActive)
- **AddTraining GET**: panggil `await SetTrainingCategoryViewBag()` sebelum `return View(model)`
- **AddTraining POST validation failure**: panggil `await SetTrainingCategoryViewBag()` sebelum `return View(model)`
- **AddTraining POST bulk renewal**: tambah `SubKategori = model.SubKategori` di TrainingRecord initializer
- **AddTraining POST single record**: tambah `SubKategori = model.SubKategori` di TrainingRecord initializer
- **EditTraining GET**: tambah `SubKategori = record.SubKategori` di model mapping; panggil `await SetTrainingCategoryViewBag()`
- **EditTraining POST**: tambah `record.SubKategori = model.SubKategori` di field update block
- **DownloadImportTrainingTemplate**: tambah `"SubKategori (opsional)"` sebagai header kolom ke-9
- **ImportTraining POST**: baca `row.Cell(9).GetString().Trim()` sebagai `subKategori`; simpan `SubKategori = string.IsNullOrWhiteSpace(subKategori) ? null : subKategori`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Dua migration diperlukan karena binary lama**
- **Found during:** Task 1
- **Issue:** `dotnet ef migrations add --no-build` menggunakan binary lama (aplikasi sedang berjalan dan mengunci file exe), sehingga migration pertama memiliki Up() kosong tetapi langsung ditandai applied di database
- **Fix:** Buat migration kedua `AddSubKategoriColumn` yang berisi AddColumn yang benar; kolom berhasil ditambahkan ke database
- **Files modified:** Migrations/20260321080108_AddSubKategoriColumn.cs
- **Commit:** 0dca111

## Known Stubs

None — semua data flow dari AssessmentCategories ke ViewBag, tidak ada hardcoded values.

## Self-Check: PASSED

- Models/TrainingRecord.cs: FOUND SubKategori property
- Models/CreateTrainingRecordViewModel.cs: FOUND SubKategori property
- Models/EditTrainingRecordViewModel.cs: FOUND SubKategori property
- Migrations/20260321080108_AddSubKategoriColumn.cs: FOUND dengan AddColumn
- Controllers/AdminController.cs: FOUND SetTrainingCategoryViewBag, SubKategori assignments
- Commit 0dca111: FOUND
- Commit 5d04ab9: FOUND
