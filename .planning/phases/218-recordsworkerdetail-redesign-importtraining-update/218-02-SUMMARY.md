---
phase: 218-recordsworkerdetail-redesign-importtraining-update
plan: "02"
subsystem: ImportTraining
tags: [import, excel-template, training-record, bug-fix]
dependency_graph:
  requires: []
  provides: [12-column-import-template, fixed-cmp-download-url]
  affects: [Controllers/AdminController.cs, Views/Admin/ImportTraining.cshtml, Views/CMP/ImportTraining.cshtml]
tech_stack:
  added: []
  patterns: [excel-template-12col, 12-column-import-mapping]
key_files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/ImportTraining.cshtml
    - Views/CMP/ImportTraining.cshtml
decisions:
  - "Kolom urutan: NIP, Judul, Kategori, SubKategori, Tanggal, TanggalMulai, TanggalSelesai, Penyelenggara, Kota, Status, ValidUntil, NomorSertifikat (D-17)"
  - "CMP download template diarahkan ke Admin controller karena CMPController tidak memiliki action DownloadImportTrainingTemplate"
metrics:
  duration: "10 minutes"
  completed_date: "2026-03-21"
  tasks: 2
  files_modified: 3
---

# Phase 218 Plan 02: ImportTraining 12-Column Update Summary

**One-liner:** Update ImportTraining template Excel dan import logic ke 12 kolom (tambah SubKategori, TanggalMulai, TanggalSelesai, Kota) sesuai D-17, plus fix CMP download URL 404.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Update DownloadImportTrainingTemplate + ImportTraining column mapping | 8184728 | Controllers/AdminController.cs |
| 2 | Update format notes di kedua view + fix CMP download URL | 47c0739 | Views/Admin/ImportTraining.cshtml, Views/CMP/ImportTraining.cshtml |

## What Was Built

### Task 1 — Controller (AdminController.cs)
- **DownloadImportTrainingTemplate:** Ganti 9 header lama dengan 12 header baru per D-17. Example row diupdate ke 12 sel. Styling loop diupdate dari `i <= 9` ke `i <= 12`.
- **ImportTraining POST:** Ganti column mapping lama (tanggal=col4, penyelenggara=col5, dll) ke mapping 12-kolom baru: subKategori=col4, tanggal=col5, tanggalMulai=col6, tanggalSelesai=col7, penyelenggara=col8, kota=col9, status=col10, validUntil=col11, nomorSertifikat=col12.
- **TrainingRecord creation:** Tambah field `TanggalMulai`, `TanggalSelesai`, `Kota` ke object initializer dengan nullable parsing.

### Task 2 — Views
- **Admin ImportTraining.cshtml:** Ganti 9 baris format notes dengan 12 baris per urutan D-17.
- **CMP ImportTraining.cshtml:** Ganti 8 baris format notes dengan 12 baris. Fix URL download template dari `"CMP"` ke `"Admin"` (CMPController tidak punya action DownloadImportTrainingTemplate, sebelumnya 404).

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- Controllers/AdminController.cs: modified (verified via git commit 8184728)
- Views/Admin/ImportTraining.cshtml: modified (verified via git commit 47c0739)
- Views/CMP/ImportTraining.cshtml: modified (verified via git commit 47c0739)
- Build: no compile errors (file lock only due to running dev server)
