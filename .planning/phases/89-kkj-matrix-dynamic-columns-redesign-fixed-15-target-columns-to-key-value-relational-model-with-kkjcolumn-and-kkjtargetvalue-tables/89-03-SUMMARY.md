---
phase: 89
plan: "89-03"
subsystem: Views + CMPController
tags: [kkj-matrix, dynamic-columns, views, razor, javascript]
dependency_graph:
  requires: [89-01, 89-02]
  provides: [Admin/KkjMatrix dynamic UI, CMP/Kkj dynamic view, KkjColumn CRUD panel, PositionMapping CRUD panel]
  affects: [CMPController, AdminController, Views/Admin/KkjMatrix.cshtml, Views/CMP/Kkj.cshtml]
tech_stack:
  added: []
  patterns:
    - Dynamic Razor loops over ViewBag.Columns (List<KkjColumn>)
    - data-column-id inputs replacing named Target_* inputs
    - AJAX panels for KkjColumn CRUD and PositionMapping CRUD
key_files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/Admin/KkjMatrix.cshtml
    - Views/CMP/Kkj.cshtml
    - Controllers/AdminController.cs (Rule 3 - fix blocking GetTargetLevel calls)
decisions:
  - "CMPController.Kkj() uses KkjBagian.Name match to find columns for selected section"
  - "Clipboard paste (native + Ctrl+V) updated to use dynamic column index mapping"
  - "Ctrl+V handler uses bagian.Columns from kkjBagians cache for column offset mapping"
  - "Kelola Kolom panel uses collapse/expand (Bootstrap accordion) to keep UI clean"
metrics:
  duration: "~35 minutes"
  completed_date: "2026-03-02"
  tasks_completed: 3
  files_modified: 4
---

# Phase 89 Plan 03: Views — Admin/KkjMatrix.cshtml + CMP/Kkj.cshtml Dynamic Columns Summary

Dynamic column rendering fully implemented in both Admin and CMP views — hardcoded 15 Label_*/Target_* column structure replaced with KkjColumn-driven dynamic rendering, plus new Kelola Kolom and Kelola Pemetaan Jabatan management panels in Admin view.

## Tasks Completed

| Task | Description | Commit |
|------|-------------|--------|
| 89-03-01 | Update CMPController.Kkj() — load dynamic columns + target values | 1497d9e |
| 89-03-02 | Rewrite Admin/KkjMatrix.cshtml — dynamic columns | bd81680 |
| 89-03-03 | Rewrite CMP/Kkj.cshtml — dynamic target columns | fc34126 |

## What Was Built

### Task 89-03-01: CMPController.Kkj()
- Loads KkjBagian by Name match for the selected section
- Includes `Columns` nav property (ordered by DisplayOrder)
- Sets `ViewBag.Columns` as `List<KkjColumn>`
- Items loaded with `.Include(m => m.TargetValues).ThenInclude(v => v.KkjColumn)`
- Handles worker users (uses `user.Unit` when no section param) vs admin/HC (uses section param)

### Task 89-03-02: Admin/KkjMatrix.cshtml
- **Razor serialization**: items serialized with TargetValues array `{KkjColumnId, Value}`, bagians with Columns array `{Id, Name, DisplayOrder}` — no Label_*/Target_* fields
- **renderReadTable()**: uses `bagian.Columns` for dynamic thead/tbody; empty state colspan calculated as `5 + columns.length + 1`
- **renderEditRows()**: uses `bagianColumns` for dynamic editable header inputs (`col-name-input` with `data-column-id`)
- **makeRow(item, columns)**: dynamic target value cells with `data-column-id` inputs; TargetValues lookup by KkjColumnId
- **makeEmptyRow(columns)**: creates empty TargetValues for each column
- **collectRows()**: reads `input[data-column-id]` instead of named Target_* inputs; builds TargetValues array
- **collectBagians()**: removed Label_* collection
- **collectColumnRenames()**: new function collects col-name-input changes
- **btnSave**: Step 0 added — save column renames via KkjColumnSave before bagian/rows
- **Clipboard paste**: both native paste and Ctrl+V updated to use dynamic column index mapping
- **Kelola Kolom Panel**: Bootstrap collapse card with add/save/delete KkjColumn UI, AJAX calls to GetKkjColumns, KkjColumnAdd, KkjColumnSave, KkjColumnDelete
- **Kelola Pemetaan Jabatan Panel**: Bootstrap collapse card with add/delete PositionColumnMapping UI, AJAX calls to GetPositionMappings, PositionMappingSave, PositionMappingDelete
- **Filter change + initial load**: both panels auto-loaded when bagian is selected

### Task 89-03-03: CMP/Kkj.cshtml
- Removed hardcoded 3-row thead with 15 fixed position columns
- 2-row dynamic thead: colspan header shows bagian name, second row loops `@foreach (var col in columns)`
- tbody renders `item.TargetValues.FirstOrDefault(v => v.KkjColumnId == col.Id)` per cell
- Empty state: "Belum ada kolom target untuk bagian ini" if no columns exist
- Removed `style="min-width: 2000px"` — table width now determined by actual column count
- Crosshair hover JS unchanged (cellIndex >= 5 still correct for 5 sticky columns)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Fixed 4 unresolved GetTargetLevel() sync calls**
- **Found during:** Pre-execution build check
- **Issue:** Plan 89-02 task 89-02-03 partially missed — PositionTargetHelper was refactored to async in commit 9d78dcc but GetTargetLevel callers in AdminController (x2, lines ~2327 and ~2399) and CMPController (x2, lines ~1425 and ~1556) were not updated
- **Fix:** Replaced `PositionTargetHelper.GetTargetLevel(mapping.KkjMatrixItem!, position)` with `await PositionTargetHelper.GetTargetLevelAsync(_context, mapping.KkjMatrixItemId, position)` in all 4 locations
- **Files modified:** Controllers/AdminController.cs, Controllers/CMPController.cs
- **Commit:** 1497d9e

## Self-Check: PASSED

All key files verified present:
- FOUND: Controllers/CMPController.cs
- FOUND: Views/Admin/KkjMatrix.cshtml
- FOUND: Views/CMP/Kkj.cshtml

All commits verified:
- FOUND: 1497d9e (feat: CMPController.Kkj() dynamic columns + fix GetTargetLevel callers)
- FOUND: bd81680 (feat: Admin/KkjMatrix.cshtml dynamic columns)
- FOUND: fc34126 (feat: CMP/Kkj.cshtml dynamic target columns)

Build status: 0 C# errors (only file-lock MSB errors from running app process)
