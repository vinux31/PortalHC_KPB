---
phase: 180-import-training-export-historiproton
plan: "01"
subsystem: CMP/CDP
tags: [import, export, excel, training, histori-proton]
dependency_graph:
  requires: []
  provides: [ImportTraining endpoint, ExportHistoriProton endpoint]
  affects: [CMPController, CDPController, RecordsTeam view, HistoriProton view]
tech_stack:
  added: [Models/ImportTrainingResult.cs]
  patterns: [ClosedXML Excel generation, ImportWorkers pattern, role-scoped query reuse]
key_files:
  created:
    - Models/ImportTrainingResult.cs
    - Views/CMP/ImportTraining.cshtml
  modified:
    - Controllers/CMPController.cs
    - Controllers/CDPController.cs
    - Views/CMP/RecordsTeam.cshtml
    - Views/CDP/HistoriProton.cshtml
decisions:
  - "ImportTrainingResult defined as public model class in Models/ (not private inner class) for clean view casting"
  - "ExportHistoriProton duplicates HistoriProton worker-building logic to avoid coupling; applies filters post-build"
  - "Export href updated client-side via updateExportHref() JS hooked to all 5 filter elements"
metrics:
  duration: "~25 minutes"
  completed_date: "2026-03-16"
  tasks_completed: 3
  files_changed: 6
---

# Phase 180 Plan 01: Import Training & Export HistoriProton Summary

**One-liner:** Bulk training record import via 8-column Excel template with NIP lookup, plus filter-aware HistoriProton export to .xlsx.

## Tasks Completed

| # | Name | Commit | Key Files |
|---|------|--------|-----------|
| 1 | Add ImportTraining and DownloadImportTrainingTemplate actions to CMPController | 1c6766b | Controllers/CMPController.cs, Models/ImportTrainingResult.cs |
| 2 | Create ImportTraining.cshtml and add import buttons to RecordsTeam | bb5d2e0 | Views/CMP/ImportTraining.cshtml, Views/CMP/RecordsTeam.cshtml |
| 3 | Add ExportHistoriProton action and export button to HistoriProton view | 47b7ea3 | Controllers/CDPController.cs, Views/CDP/HistoriProton.cshtml |

## What Was Built

### Import Training (IMP-05, IMP-06)
- `GET /CMP/DownloadImportTrainingTemplate` — returns `training_import_template.xlsx` with 8 columns (NIP, Judul, Kategori, Tanggal, Penyelenggara, Status, ValidUntil, NomorSertifikat), green header row, example data row, notes rows
- `GET /CMP/ImportTraining` — upload form page
- `POST /CMP/ImportTraining` — validates file (type, size), iterates rows, looks up user by NIP, creates `TrainingRecord` entries, returns per-row results (Success/Error with message)
- `Models/ImportTrainingResult.cs` — public class for ViewBag casting in view
- `Views/CMP/ImportTraining.cshtml` — full import page with breadcrumb, results summary cards, results table, drag-drop upload zone, format notes table
- `Views/CMP/RecordsTeam.cshtml` — Import Excel (primary) + Download Template (outline) buttons added to Export Buttons row, visible to Admin/HC only

### Export HistoriProton (EXP-05)
- `GET /CDP/ExportHistoriProton` — accepts search/section/unit/jalur/status query params, replicates HistoriProton worker-building query, applies filters, returns `histori_proton_YYYYMMDD.xlsx` with 9 columns (No, NIP, Nama, Unit, Jalur, Tahun 1, Tahun 2, Tahun 3, Status)
- Level 6 coachee redirect preserved
- `Views/CDP/HistoriProton.cshtml` — Export Excel (green) button added to filter toolbar; `updateExportHref()` JS function updates button href as user changes any of the 5 filters

## Deviations from Plan

None - plan executed exactly as written. The `ImportTrainingResult` model class was placed in `Models/` as a public class (as specified in the revised approach in Task 2's plan), so both controller and view can reference it cleanly.

## Self-Check: PASSED

Files confirmed present:
- Controllers/CMPController.cs — contains DownloadImportTrainingTemplate, ImportTraining (GET+POST)
- Controllers/CDPController.cs — contains ExportHistoriProton
- Models/ImportTrainingResult.cs — exists
- Views/CMP/ImportTraining.cshtml — exists
- Views/CMP/RecordsTeam.cshtml — contains ImportTraining links
- Views/CDP/HistoriProton.cshtml — contains ExportHistoriProton and updateExportHref

Commits verified: 1c6766b, bb5d2e0, 47b7ea3
