---
phase: 20-edit-delete-recordsworkerlist-wiring
plan: 01
subsystem: training-records
tags: [edit, delete, bootstrap-modal, training-records, worker-detail, file-management]
dependency_graph:
  requires:
    - 19-01 (TrainingRecord model, CreateTrainingRecord POST, GetUnifiedRecords helper, SertifikatUrl wiring)
    - 18-01 (TrainingRecord DB table, EF migrations)
  provides:
    - EditTrainingRecord POST action in CMPController
    - DeleteTrainingRecord POST action in CMPController
    - EditTrainingRecordViewModel
    - Per-row Bootstrap edit modals in WorkerDetail
    - Aksi column with Edit/Delete buttons on Training Manual rows
  affects:
    - Views/CMP/WorkerDetail.cshtml
    - Models/UnifiedTrainingRecord.cs (new fields for modal pre-population)
    - Controllers/CMPController.cs (two new POST actions, GetUnifiedRecords updated)
tech_stack:
  added: []
  patterns:
    - Bootstrap 5 modal per-row pre-population via Razor (no separate edit page/GET action)
    - File replace pattern: delete old physical file, save new with timestamp prefix
    - TempData redirect-with-message pattern for POST actions
    - isHCAccess gate (Admin || HC) on both edit and delete POST actions
key_files:
  created:
    - Models/EditTrainingRecordViewModel.cs
  modified:
    - Models/UnifiedTrainingRecord.cs
    - Controllers/CMPController.cs
    - Views/CMP/WorkerDetail.cshtml
decisions:
  - EditTrainingRecord has no GET action — modal is pre-populated inline via Razor in WorkerDetail.cshtml; purely POST-only approach avoids extra page navigation
  - WorkerId and WorkerName stored on EditTrainingRecordViewModel so redirect back to WorkerDetail requires no extra DB lookup in the POST
  - File validation errors on edit use TempData redirect (not ModelState+View) since there is no dedicated edit view to return to
  - UnifiedTrainingRecord extended with Kategori, Kota, NomorSertifikat, TanggalMulai, TanggalSelesai for modal field pre-population
  - Assessment Online rows intentionally excluded from Edit/Delete — guarded by RecordType == "Training Manual" && TrainingRecordId.HasValue
metrics:
  duration: ~15 min
  completed: 2026-02-20
  tasks_completed: 2
  files_changed: 4
---

# Phase 20 Plan 01: Edit and Delete Training Records (WorkerDetail Modal) Summary

**One-liner:** HC/Admin can edit or delete any manual training record from WorkerDetail via per-row Bootstrap modals — no separate edit page; file replace and physical delete handled server-side.

## What Was Built

TRN-02 and TRN-03 — training record edit and delete — fully implemented.

**EditTrainingRecordViewModel** (`Models/EditTrainingRecordViewModel.cs`) mirrors CreateTrainingRecordViewModel but without `UserId` (worker cannot be changed on edit). Adds `WorkerId`, `WorkerName` (no-validation hidden inputs for redirect), and `ExistingSertifikatUrl` (hidden input passed through form so POST can retain old file if no new upload).

**UnifiedTrainingRecord** (`Models/UnifiedTrainingRecord.cs`) gains five new properties: `TrainingRecordId` (null for Assessment Online rows, used to generate modal IDs and action targets), `Kategori`, `Kota`, `NomorSertifikat`, `TanggalMulai`, `TanggalSelesai` — all needed for modal field pre-population. All five are mapped in `GetUnifiedRecords`.

**CMPController** (`Controllers/CMPController.cs`) gains two POST-only actions:
- `EditTrainingRecord`: HC/Admin gate, file type/size validation, old file physical deletion, new file save with timestamp prefix, full field update (UserId intentionally excluded), `SaveChangesAsync`, TempData success, redirect to WorkerDetail.
- `DeleteTrainingRecord`: HC/Admin gate, certificate file physical deletion, `Remove` + `SaveChangesAsync`, TempData success, redirect to WorkerDetail.

**WorkerDetail.cshtml** (`Views/CMP/WorkerDetail.cshtml`) receives five changes:
1. TempData success/error dismissible alerts at page top.
2. Aksi column header (8% width); Sertifikat column reduced from 6% to 5%.
3. Action cell per row: Training Manual rows with a `TrainingRecordId` get Edit (pencil, `data-bs-target`) + Delete (trash, inline POST form with `confirm()` guard); all other rows get an em-dash.
4. Per-row Bootstrap modals rendered after `</table>` — one modal per Training Manual record, pre-populated from `item.*` fields via Razor, with current certificate download link if one exists.
5. CSV export updated: "Aksi" added to header; empty string cell appended to each data row.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | EditTrainingRecordViewModel, controller POST actions, UnifiedTrainingRecord.TrainingRecordId | 6e0a5bb | Models/EditTrainingRecordViewModel.cs (new), Models/UnifiedTrainingRecord.cs, Controllers/CMPController.cs |
| 2 | WorkerDetail.cshtml — per-row Bootstrap modals, action buttons, and TempData alerts | 7f756a3 | Views/CMP/WorkerDetail.cshtml |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing critical fields] Extended UnifiedTrainingRecord with modal pre-population fields**
- **Found during:** Task 2 (noted in plan as NOTE at bottom of Task 2 action)
- **Issue:** Modal forms reference `item.Kategori`, `item.Kota`, `item.NomorSertifikat`, `item.TanggalMulai`, `item.TanggalSelesai` — these properties did not exist on UnifiedTrainingRecord; they existed on TrainingRecord but were not mapped in GetUnifiedRecords.
- **Fix:** Added five properties to UnifiedTrainingRecord and mapped them in GetUnifiedRecords alongside the TrainingRecordId mapping (Task 1 scope extended).
- **Files modified:** Models/UnifiedTrainingRecord.cs, Controllers/CMPController.cs
- **Commit:** 6e0a5bb (included in Task 1 commit as plan noted this requirement explicitly)

## Self-Check: PASSED

- Models/EditTrainingRecordViewModel.cs: FOUND
- Models/UnifiedTrainingRecord.cs: FOUND (TrainingRecordId + 5 new fields)
- Controllers/CMPController.cs: FOUND (EditTrainingRecord POST + DeleteTrainingRecord POST; no GET)
- Views/CMP/WorkerDetail.cshtml: FOUND (TempData alerts, Aksi column, action cells, modals, CSV update)
- Commit 6e0a5bb: FOUND
- Commit 7f756a3: FOUND
- dotnet build: 0 errors (35 pre-existing warnings, none new)
