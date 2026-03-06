---
phase: 83-master-data-qa
plan: 08
subsystem: ManageWorkers UI / Worker Soft Delete
tags: [soft-delete, inactive-users, export, import, ui]
requirements: [DATA-05, DATA-06]

dependency_graph:
  requires: [83-06]
  provides: [worker-soft-delete-ui, export-inactive, import-review]
  affects: [Views/Admin/ManageWorkers.cshtml, Controllers/AdminController.cs, Views/Admin/ImportWorkers.cshtml, Models/ImportWorkerResult.cs]

tech_stack:
  added: []
  patterns: [soft-delete-toggle, conditional-buttons, inline-form-post]

key_files:
  created: []
  modified:
    - Views/Admin/ManageWorkers.cshtml
    - Controllers/AdminController.cs
    - Views/Admin/ImportWorkers.cshtml
    - Models/ImportWorkerResult.cs

decisions:
  - "ManageWorkers toggle uses anchor-link GET pattern (not form checkbox) — simpler, compatible with existing filter form"
  - "Hapus (hard delete) modal removed from UI; backend DeleteWorker action preserved for programmatic use"
  - "ImportWorkers PerluReview shows inline Aktifkan Kembali form — no redirect, user stays on results page"
  - "ExportWorkers Status column added only when showInactive=true — keeps normal export backward compatible"

metrics:
  duration: 3 min
  completed_date: "2026-03-03"
  tasks_completed: 2
  tasks_total: 2
  files_modified: 4
---

# Phase 83 Plan 08: Worker Soft Delete UI Summary

**One-liner:** ManageWorkers soft delete UI wired: Tampilkan Inactive toggle, Nonaktifkan/Aktifkan Kembali conditional buttons, Export Status column, and ImportWorkers PerluReview detection with Reaktivasi button.

## What Was Built

Plan 83-08 completed the UI layer for the worker soft delete workflow established in Plan 83-06. The backend (DeactivateWorker/ReactivateWorker POST actions, ManageWorkers showInactive filter) was already in place — this plan connected it to visible controls in the browser.

### ManageWorkers.cshtml

- Added `showInactive` variable from `ViewBag.ShowInactive`
- Added Tampilkan/Sembunyikan Inactive toggle button in the header action area
- Updated Export Excel button to include `showInactive` query param so exported file respects current view state
- Replaced the hard-delete Hapus button with conditional soft-delete buttons:
  - Active users: Nonaktifkan button (btn-outline-warning, bi-person-dash icon) with `onsubmit="return confirmDeactivate(...)"`
  - Inactive users: Aktifkan Kembali button (btn-outline-success, bi-person-check icon)
- Added `table-secondary text-muted` CSS class to inactive user rows for visual distinction
- Added `confirmDeactivate(userName)` JS function with informative confirmation message about coaching/assessment side-effects
- Removed old deleteModal and confirmDelete JS function (replaced by inline form approach)

### AdminController.cs — ExportWorkers

- Added `bool showInactive = false` parameter to ExportWorkers
- Added `if (!showInactive) query = query.Where(u => u.IsActive)` filter
- Added conditional headers array: includes "Status" column when showInactive=true
- Writes `u.IsActive ? "Aktif" : "Tidak Aktif"` to column 11 when showInactive=true

### AdminController.cs — ImportWorkers

- Updated existing-email check: detects `!existing.IsActive` and sets `Status = "PerluReview"` with `ExistingUserId = existing.Id`
- Active existing emails still get `Status = "Skip"`
- Updated audit log to include `reviewCount` in description

### Models/ImportWorkerResult.cs

- Added `public string? ExistingUserId { get; set; }` property for PerluReview rows

### Views/Admin/ImportWorkers.cshtml

- Added `reviewCount` variable
- Changed summary cards from 3-column to 4-column layout; added Perlu Review card (text-info)
- Updated results table switch to include `"PerluReview" => "table-warning"` row class and info badge
- Added inline Aktifkan Kembali form (POST to ReactivateWorker) for PerluReview rows

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Update ManageWorkers.cshtml with Tampilkan Inactive toggle and soft delete buttons | cda5182 | Views/Admin/ManageWorkers.cshtml |
| 2 | Update ExportWorkers (showInactive + Status column) and ImportWorkers (inactive email match) | 66006ea | Controllers/AdminController.cs, Models/ImportWorkerResult.cs, Views/Admin/ImportWorkers.cshtml |

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- Views/Admin/ManageWorkers.cshtml: modified — showInactive toggle, Nonaktifkan/Aktifkan buttons, row styling present
- Controllers/AdminController.cs: ExportWorkers accepts showInactive, ImportWorkers detects PerluReview
- Models/ImportWorkerResult.cs: ExistingUserId property added
- Views/Admin/ImportWorkers.cshtml: PerluReview card and Aktifkan Kembali button present
- Commits: cda5182, 66006ea — both exist in git log
- dotnet build: no CS compiler errors (file-lock warning from running process is not a code error)
