---
phase: 47-kkj-matrix-manager
plan: "03"
subsystem: admin-kkjmatrix
tags: [kkj-matrix, bagian, entity, ef-migration, admin-controller, view-rewrite]
dependency-graph:
  requires: [47-02]
  provides: [KkjBagian-entity, KkjBagianSave-endpoint, KkjBagianAdd-endpoint, per-bagian-view]
  affects: [KkjMatrix-page, KkjMatrixItem-model]
tech-stack:
  added: []
  patterns: [EF-seeding-on-first-load, ViewBag-bagians, hidden-input-per-tr, per-bagian-edit-tables]
key-files:
  created:
    - Migrations/20260226104042_AddKkjBagianAndBagianField.cs
    - Migrations/20260226104042_AddKkjBagianAndBagianField.Designer.cs
  modified:
    - Models/KkjModels.cs
    - Data/ApplicationDbContext.cs
    - Controllers/AdminController.cs
    - Views/Admin/KkjMatrix.cshtml
decisions:
  - KkjBagian seeded on first GET rather than in a migration data seed — avoids hard-coded migration data
  - Bagian stored as string name on KkjMatrixItem (FK by name, not int FK) — consistent with CpdpItem.Section precedent
  - btnAddRow removed from toolbar; each bagian has its own per-section add-row button
  - collectBagians() queries by bagianId on label inputs — more reliable than DOM traversal
  - Paste handler uses e.target.closest('.kkj-edit-tbl') to support multi-table event delegation
metrics:
  duration: "7 minutes"
  completed: "2026-02-26"
  tasks_completed: 3
  files_modified: 4
  files_created: 2
---

# Phase 47 Plan 03: KKJ Matrix Per-Bagian Grouping Summary

**One-liner:** KkjBagian entity with 15 editable column header labels, EF migration, per-bagian grouped view rendering with editable headers in edit mode, and KkjBagianSave/KkjBagianAdd server endpoints.

## What Was Built

Added per-bagian table structure to the KKJ Matrix admin page, addressing UAT gap 1 (user requires separate tables for RFCC, GAST, NGP, DHT/HMU bagians with ability to add bagians and edit column headers).

### Task 1: KkjBagian Entity + EF Migration
- Added `KkjBagian` class to `Models/KkjModels.cs` with: `Id`, `Name`, `DisplayOrder`, and 15 `Label_*` properties (one per Target column)
- Added `Bagian` string property to `KkjMatrixItem` (FK by name to `KkjBagian.Name`)
- Registered `DbSet<KkjBagian> KkjBagians` in `ApplicationDbContext`
- Added EF Fluent configuration: `KkjBagians` table, unique index on `Name`, index on `DisplayOrder`
- EF migration `AddKkjBagianAndBagianField` created and applied — `KkjBagians` table and `Bagian` column on `KkjMatrices` exist in SQL Server

### Task 2: AdminController Endpoints
- `KkjMatrix GET`: seeds 4 default bagians (RFCC, GAST, NGP, DHT/HMU) on first load; passes `ViewBag.Bagians` ordered by `DisplayOrder`
- `KkjBagianSave POST`: upserts bagian headers using `FindAsync` pattern; updates all 15 `Label_*` fields with null-coalescing defaults
- `KkjBagianAdd POST`: creates new bagian with `Name = "Bagian Baru"` and next `DisplayOrder`; returns new `id`, `name`, `displayOrder` as JSON
- `KkjMatrixSave`: added `existing.Bagian = row.Bagian ?? ""` to persist the bagian field

### Task 3: KkjMatrix View Rewrite
- **JS data injection**: both `kkjItems` and `kkjBagians` embedded as JS vars at page top
- **Read mode**: per-bagian sections with `<h6>` heading + item count badge; unassigned items shown in "Tidak Terkategori" section
- **Edit mode**: `#editTablesContainer` populated by `renderEditRows()` — one table per bagian with editable `<input>` headers in `<thead>` and per-bagian add-row buttons
- **Save flow**: `btnSave` calls `collectBagians()` then `KkjBagianSave`, on success calls `collectRows()` then `KkjMatrixSave`, then `location.reload()`
- **Add Bagian**: AJAX to `KkjBagianAdd`, pushes to `kkjBagians` array, re-renders edit container
- **Hidden Bagian field**: each `<tr>` has a hidden `input[name="Bagian"]` preserving bagian assignment
- **Paste handler**: updated to use `editTablesContainer` with `e.target.closest('.kkj-edit-tbl')` for multi-table event delegation
- **Keyboard nav**: Tab/Enter updated to query `#editTablesContainer .bagian-tbody input.edit-input`
- **escHtml helper**: added for safe HTML injection

## Deviations from Plan

None — plan executed exactly as written.

## Verification

- `dotnet build --configuration Release` — 0 errors, 31 pre-existing warnings (unchanged)
- EF migration `AddKkjBagianAndBagianField` created and applied to database
- `KkjBagian` class present in `Models/KkjModels.cs` with all 15 `Label_*` properties
- `DbSet<KkjBagian>` registered in `ApplicationDbContext`
- `AdminController` has KkjMatrix GET (seeding + ViewBag.Bagians), `KkjBagianSave` POST, `KkjBagianAdd` POST
- `KkjMatrix.cshtml` renders per-bagian grouped read-mode tables and per-bagian edit-mode tables

## Self-Check: PASSED

- Models/KkjModels.cs: FOUND with KkjBagian class and Bagian field on KkjMatrixItem
- Data/ApplicationDbContext.cs: FOUND with DbSet<KkjBagian>
- Controllers/AdminController.cs: FOUND with KkjBagianSave, KkjBagianAdd, updated KkjMatrix GET
- Views/Admin/KkjMatrix.cshtml: FOUND with per-bagian structure
- Migrations/20260226104042_AddKkjBagianAndBagianField.cs: FOUND
- Commits: 78f0625, cb6e0c2, f90673f all present in git log
