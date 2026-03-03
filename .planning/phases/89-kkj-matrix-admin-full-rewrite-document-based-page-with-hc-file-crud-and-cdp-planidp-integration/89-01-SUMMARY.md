---
phase: 90-kkj-matrix-admin-full-rewrite
plan: 01
subsystem: kkj-matrix
tags: [data-model, ef-migration, cleanup, kkj, phase90]
dependency_graph:
  requires: []
  provides: [KkjFile model, KkjFiles DB table, cleaned ApplicationDbContext]
  affects: [AdminController.cs, CMPController.cs, Views/Admin/KkjMatrix.cshtml, Views/CMP/Kkj.cshtml]
tech_stack:
  added: [KkjFile entity class, EF migration DropKkjTablesAddKkjFiles]
  patterns: [orphaned-int-column for backward-compatible FK removal, TODO-Phase90 comment markers]
key_files:
  created:
    - Migrations/20260302125630_DropKkjTablesAddKkjFiles.cs
  modified:
    - Models/KkjModels.cs
    - Models/Competency/AssessmentCompetencyMap.cs
    - Models/Competency/UserCompetencyLevel.cs
    - Models/ProtonViewModels.cs
    - Data/ApplicationDbContext.cs
    - Data/SeedMasterData.cs
    - Data/SeedCompetencyMappings.cs
    - Controllers/AdminController.cs
    - Controllers/CMPController.cs
    - Controllers/CDPController.cs
    - Views/Admin/KkjMatrix.cshtml
    - Views/CMP/Kkj.cshtml
    - Program.cs
  deleted:
    - Helpers/PositionTargetHelper.cs
decisions:
  - "KkjMatrixItemId preserved as orphaned int in AssessmentCompetencyMap and UserCompetencyLevel — FK constraint removed, column kept for data continuity"
  - "All old KKJ controller code commented with TODO-Phase90 markers rather than deleted — makes Plan 02 rewrite easier to locate placeholders"
  - "Stub views written for KkjMatrix.cshtml and Kkj.cshtml to unblock build — full rewrites in Plans 02 and 03"
  - "CDPController.cs had unused HcPortal.Helpers import — removed as Rule 3 auto-fix (blocking build)"
metrics:
  duration_minutes: 10
  tasks_completed: 3
  files_modified: 14
  files_deleted: 1
  files_created: 2
  completed_date: "2026-03-02"
---

# Phase 90 Plan 01: Drop KKJ Table Models, Add KkjFile, Apply Migration — Summary

**One-liner:** Dropped 4 KKJ relational tables (KkjMatrices, KkjColumns, KkjTargetValues, PositionColumnMappings), added KkjFile document model with BagianId FK, and applied EF migration — establishing Phase 90 data foundation.

## What Was Built

Plan 01 established the new data model foundation for the KKJ Matrix document-based rewrite:

1. **Models/KkjModels.cs** rewritten to contain only KkjBagian and KkjFile. KkjMatrixItem, KkjColumn, KkjTargetValue, and PositionColumnMapping removed.

2. **KkjFile model** added with fields: BagianId (FK), FileName, FilePath, FileSizeBytes, FileType, Keterangan (optional), UploadedAt, UploaderName, IsArchived.

3. **ApplicationDbContext** cleaned: removed 4 old DbSets and their entity configurations; removed FK constraints from AssessmentCompetencyMap and UserCompetencyLevel to KkjMatrices; added KkjFiles DbSet and entity config with Cascade delete from KkjBagian.

4. **EF Migration** `DropKkjTablesAddKkjFiles` created and applied: drops all 4 old tables, removes FK constraints, creates KkjFiles table.

5. **PositionTargetHelper.cs** deleted entirely.

6. **Controller stubs**: Old KKJ CRUD regions in AdminController and CMPController commented with `// TODO-Phase90` markers; `KkjMatrix()` and `Kkj()` actions replaced with minimal stubs that return empty views.

7. **View stubs**: KkjMatrix.cshtml and Kkj.cshtml rewritten as placeholder stubs pending Plan 02/03 full rewrites.

## Verification Results

| Check | Result |
|-------|--------|
| dotnet build (0 CS errors) | PASSED |
| KkjFile model exists in KkjModels.cs | PASSED |
| KkjMatrixItem/KkjColumn/KkjTargetValue/PositionColumnMapping removed | PASSED |
| ApplicationDbContext has KkjFiles DbSet only (not old DbSets) | PASSED |
| PositionTargetHelper.cs deleted | PASSED |
| EF migration applied (database up to date) | PASSED |
| KkjFiles table exists in DB | PASSED |
| KkjMatrices/KkjColumns/KkjTargetValues/PositionColumnMappings tables dropped | PASSED |

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| Task 1 + 2 | 3371b38 | chore(90-01): remove old KKJ table models, add KkjFile model, clean up codebase |
| Task 3 | 0227a64 | feat(90-01): add EF migration DropKkjTablesAddKkjFiles — apply schema changes |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Removed unused HcPortal.Helpers import from CDPController.cs**
- **Found during:** Task 2 (after deleting PositionTargetHelper.cs)
- **Issue:** CDPController.cs had `using HcPortal.Helpers;` import but never used any class from it. Deletion of PositionTargetHelper.cs made HcPortal.Helpers namespace disappear, causing CS0234 build error in CDPController.cs
- **Fix:** Removed the `using HcPortal.Helpers;` line from CDPController.cs
- **Files modified:** Controllers/CDPController.cs
- **Commit:** 3371b38

**2. [Rule 3 - Blocking] Program.cs still called SeedKkjMatrixAsync after method was removed**
- **Found during:** Task 2 (after cleaning SeedMasterData.cs)
- **Issue:** Program.cs line 107 called `SeedMasterData.SeedKkjMatrixAsync(context)` which no longer existed, causing CS0117 build error
- **Fix:** Removed the call and added a TODO-Phase90 comment
- **Files modified:** Program.cs
- **Commit:** 3371b38

**3. [Scope decision] View stubs created earlier than planned**
- **Found during:** Task 2 build check
- **Issue:** Views/Admin/KkjMatrix.cshtml and Views/CMP/Kkj.cshtml used `@model List<HcPortal.Models.KkjMatrixItem>` which no longer compiles after model removal. These views needed replacement to enable the build for migration creation.
- **Fix:** Replaced with minimal stub views (no model type, info message only) pending full Plan 02/03 rewrites
- **Files modified:** Views/Admin/KkjMatrix.cshtml, Views/CMP/Kkj.cshtml
- **Commit:** 3371b38

## Self-Check: PASSED

| Check | Result |
|-------|--------|
| Models/KkjModels.cs exists | FOUND |
| Migration file 20260302125630_DropKkjTablesAddKkjFiles.cs exists | FOUND |
| PositionTargetHelper.cs deleted | CONFIRMED |
| Commit 3371b38 exists | FOUND |
| Commit 0227a64 exists | FOUND |
