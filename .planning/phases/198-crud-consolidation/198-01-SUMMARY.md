---
phase: 198-crud-consolidation
plan: 01
subsystem: training-crud
tags: [consolidation, refactor, admin-controller]
dependency_graph:
  requires: []
  provides: [unified-training-import, clean-cmp-controller]
  affects: [CMPController, AdminController, ManageAssessment, RecordsTeam]
tech_stack:
  added: []
  patterns: [import-excel-pattern]
key_files:
  created:
    - Views/Admin/ImportTraining.cshtml
  modified:
    - Controllers/CMPController.cs
    - Controllers/AdminController.cs
    - Views/Admin/ManageAssessment.cshtml
    - Views/CMP/RecordsTeam.cshtml
decisions:
  - ImportTraining view uses strongly-typed model (List<ImportTrainingResult>) instead of ViewBag pattern
metrics:
  duration: 4.6min
  completed: 2026-03-18
  tasks_completed: 2
  tasks_total: 2
---

# Phase 198 Plan 01: Training CRUD Consolidation Summary

Moved training import/edit/delete from CMPController to AdminController, created ImportTraining view following ImportWorkers pattern, cleaned up RecordsTeam references.

## Tasks Completed

| Task | Name | Commit | Key Files |
|------|------|--------|-----------|
| 1 | Hapus CMP orphan actions + pindahkan import ke Admin | ad6baed | CMPController.cs, AdminController.cs |
| 2 | Buat view ImportTraining + update ManageAssessment + bersihkan RecordsTeam | 77d56c7 | ImportTraining.cshtml, ManageAssessment.cshtml, RecordsTeam.cshtml |

## What Was Done

1. **Removed from CMPController:** EditTrainingRecord (POST), DeleteTrainingRecord (POST), ImportTraining (GET+POST), DownloadImportTrainingTemplate (GET) -- total ~310 lines removed
2. **Added to AdminController:** ImportTraining (GET+POST) and DownloadImportTrainingTemplate (GET) with `[Authorize(Roles = "Admin, HC")]` -- logic copied from CMP with improved model passing
3. **Created ImportTraining.cshtml:** Following exact ImportWorkers pattern -- breadcrumb, summary cards, results table, upload card with drag-and-drop, format notes table
4. **Updated ManageAssessment.cshtml:** Added "Import Excel" button next to "Tambah Training" in tab=training header
5. **Cleaned RecordsTeam.cshtml:** Removed import/download template buttons block

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Improvement] Changed ImportTraining GET to return strongly-typed model**
- **Found during:** Task 1
- **Issue:** Original CMP ImportTraining GET returned `View()` without model, view used ViewBag
- **Fix:** Changed to `View(new List<ImportTrainingResult>())` for type safety, view uses `@model` directive
- **Files modified:** AdminController.cs, ImportTraining.cshtml

## Verification

- dotnet build: 0 errors
- CMPController.cs: no matches for EditTrainingRecord/DeleteTrainingRecord/ImportTraining/DownloadImportTrainingTemplate
- RecordsTeam.cshtml: no matches for ImportTraining
- ManageAssessment.cshtml: contains `Url.Action("ImportTraining", "Admin")`
- Views/Admin/ImportTraining.cshtml: exists with 228 lines
