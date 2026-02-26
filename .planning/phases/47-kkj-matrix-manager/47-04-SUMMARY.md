---
phase: 47-kkj-matrix-manager
plan: "04"
subsystem: admin-kkjmatrix
tags: [kkj-matrix, read-mode, edit-mode, aksi-column, insert-below, inline-delete]
dependency-graph:
  requires: [47-03]
  provides: [full-21-column-read-mode, per-row-insert-delete-edit-mode]
  affects: [KkjMatrix-page]
tech-stack:
  added: []
  patterns: [insertBefore-for-row-insert, dom-only-delete-unsaved, ajax-delete-saved]
key-files:
  created: []
  modified:
    - Views/Admin/KkjMatrix.cshtml
decisions:
  - Read-mode column headers for Target_* use bagian.Label_* values from the KkjBagian entity — consistent with per-bagian label customization from Plan 03
  - Inline delete for Id=0 rows is DOM-only (no server call) — avoids unnecessary server round-trips for unsaved rows
  - Insert-below copies bagian name from current row's hidden Bagian input — ensures new row stays in correct bagian
  - Aksi th appended after 15 label inputs in renderEditRows() thead — maintains column alignment with makeRow() td order
metrics:
  duration: "2 minutes"
  completed: "2026-02-26"
  tasks_completed: 2
  files_modified: 1
  files_created: 0
---

# Phase 47 Plan 04: Full Read-Mode Table and Edit-Mode Per-Row Aksi Column Summary

**One-liner:** Expanded read-mode table from 5 to 21 columns with bagian.Label_* headers, and added 22nd Aksi column to edit-mode makeRow() with insert-below (DOM insertBefore) and inline delete (DOM-only for unsaved, AJAX for saved rows).

## What Was Built

Addressed UAT gaps 2 and 3: user needs full table visibility in read mode (not compact 5-column view) and per-row insert/delete in edit mode to add/remove rows at any position.

### Task 1: Expand Read-Mode Table to All 21 Columns Per Bagian

- Replaced 5-column `<thead>` (No, Indeks, Kompetensi, SkillGroup, Aksi) with 21-column structure per bagian section
- Column headers for 15 Target_* columns use `@bagian.Label_*` values from the KkjBagian entity
- `<tbody>` rows now render all 20 data columns: No, Indeks, Kompetensi, SkillGroup, SubSkillGroup, and 15 Target_* values
- Aksi column retained as 21st column with delete button per row
- Empty-row colspan updated from 5 to 21
- "Tidak Terkategori" fallback section updated with static 21-column headers (static text since no bagian object available)
- All bagian section tables use existing `class="table-responsive custom-scrollbar mb-3"` wrapper for horizontal scrolling

### Task 2: Add 22nd Aksi Column to Edit-Mode makeRow()

- Added `.col-aksi { min-width: 75px; }` CSS class to the style block
- Added `thAksi` to `renderEditRows()` thead after the 15 label input `<th>` elements — Aksi column header aligns correctly
- Updated `makeRow()` to append a 22nd `<td class="col-aksi text-nowrap">` with two action buttons:
  - **Insert-below button** (bi-plus-circle, btn-outline-success): reads `input[name="Bagian"]` from current `<tr>`, calls `makeEmptyRow()`, sets Bagian on new row, then inserts via `tr.parentNode.insertBefore(newRow, tr.nextSibling)` — row appears immediately below current row
  - **Inline delete button** (bi-trash, btn-outline-danger): for `Id=0` rows removes from DOM directly; for saved rows (`Id > 0`) shows confirmation dialog then AJAX POSTs to `/Admin/KkjMatrixDelete` with `__RequestVerificationToken`, removes `<tr>` from DOM and filters `kkjItems` array on success
- `makeEmptyRow()` inherits the Aksi column automatically via `makeRow()`

## Deviations from Plan

None — plan executed exactly as written.

## Verification

- `dotnet build --configuration Release` — 0 errors, 31 pre-existing warnings (unchanged)
- Read-mode thead for each bagian section has 21 `<th>` elements
- Read-mode tbody rows render SubSkillGroup and all 15 Target_* values
- "Tidak Terkategori" section has 21-column static header
- Edit-mode `renderEditRows()` appends Aksi `<th>` after 15 label inputs
- `makeRow()` appends `tdAksi` as 22nd column with insert-below + delete buttons
- Insert-below uses `insertBefore(newRow, tr.nextSibling)` pattern
- Inline delete for Id=0 calls `tr.remove()` directly
- Inline delete for Id>0 calls `/Admin/KkjMatrixDelete` via AJAX

## Self-Check: PASSED

- Views/Admin/KkjMatrix.cshtml: FOUND with 21-column read-mode and 22nd Aksi edit-mode column
- Commit 52d6efb (Task 1): FOUND in git log
- Commit 2c35081 (Task 2): FOUND in git log
