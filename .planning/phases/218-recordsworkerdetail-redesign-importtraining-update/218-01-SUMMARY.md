---
phase: 218-recordsworkerdetail-redesign-importtraining-update
plan: 01
subsystem: CMP/Records
tags: [view-redesign, table-columns, modal, cascade-filter, unified-records]
dependency_graph:
  requires: []
  provides: [RecordsWorkerDetail 7-column table, trainingDetailModal, SubCategory cascade filter]
  affects: [Views/CMP/RecordsWorkerDetail.cshtml, Models/UnifiedTrainingRecord.cs, Services/WorkerDataService.cs, Controllers/CMPController.cs]
tech_stack:
  added: []
  patterns: [cascade dropdown filter, Bootstrap modal via data attributes, master-data-driven filter]
key_files:
  created: []
  modified:
    - Models/UnifiedTrainingRecord.cs
    - Services/WorkerDataService.cs
    - Controllers/CMPController.cs
    - Views/CMP/RecordsWorkerDetail.cshtml
decisions:
  - SubKategori ditambahkan ke UnifiedTrainingRecord untuk training rows (Assessment tidak punya SubKategori)
  - Assessment rows populate Kategori dari AssessmentSession.Category (field non-nullable)
  - Category filter menggunakan master AssessmentCategories (sama seperti Records/RecordsTeam) bukan data dari records
  - SubCategory cascade JS pattern sama dengan RecordsTeam — disabled sampai Category dipilih
  - filterTable menggunakan exact-match untuk category dan subcategory
metrics:
  duration: ~15m
  completed_date: "2026-03-21"
  tasks_completed: 2
  files_modified: 4
---

# Phase 218 Plan 01: RecordsWorkerDetail Redesign Summary

**One-liner:** 7-column RecordsWorkerDetail table with Kategori/SubKategori columns, Action column (Detail modal + Sertifikat), and master-data-driven SubCategory cascade filter replacing Score/Sertifikat columns.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Backend: SubKategori model, Kategori/SubKategori mapping, AssessmentCategories to view | 25944e9 | UnifiedTrainingRecord.cs, WorkerDataService.cs, CMPController.cs |
| 2 | View: 7-column table, Action buttons, modal, SubCategory cascade filter | cd40a13 | RecordsWorkerDetail.cshtml |

## Changes Made

### Task 1 — Backend

- `Models/UnifiedTrainingRecord.cs`: Added `public string? SubKategori { get; set; }` after `Kategori` property
- `Services/WorkerDataService.cs`: Added `Kategori = a.Category` to Assessment mapping in `GetUnifiedRecords()`, added `SubKategori = t.SubKategori` to Training mapping
- `Controllers/CMPController.cs`: In `RecordsWorkerDetail` action, added AssessmentCategories query pattern (same as Records action) to pass `SubCategoryMapJson` and `MasterCategoriesJson` to ViewBag

### Task 2 — View Redesign

- Removed Score column, removed Sertifikat column
- Added Kategori column (column 4), Sub Kategori column (column 5)
- Added Action column (column 7): Training rows get Detail button (opens modal) + conditional Download Sertifikat link; Assessment rows get Sertifikat button (Certificate action) if GenerateCertificate
- Added `trainingDetailModal` Bootstrap modal with 6 fields: Nama Kegiatan, Penyelenggara, Kota, Tanggal Mulai, Tanggal Selesai, Nomor Sertifikat
- Category filter uses master `MasterCategoriesJson` (not records-derived data)
- Added SubCategory cascade dropdown (disabled until Category selected)
- Added `data-subcategory` attribute to table rows
- Removed `onclick`, `cursor:pointer`, `table-row-clickable` from all rows
- Updated `filterTable()` to include subCategory exact-match filter
- Updated `clearFilters()` to reset subCategoryFilter and disable it
- Updated empty-state `colspan` from 6 to 7

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- Models/UnifiedTrainingRecord.cs: FOUND
- Views/CMP/RecordsWorkerDetail.cshtml: FOUND
- .planning/phases/218-.../218-01-SUMMARY.md: FOUND
- Commit 25944e9: FOUND
- Commit cd40a13: FOUND
