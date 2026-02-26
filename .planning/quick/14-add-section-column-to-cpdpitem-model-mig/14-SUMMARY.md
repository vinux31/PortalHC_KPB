---
phase: quick-14
plan: 01
subsystem: CMP / CPDP Mapping
tags: [model, migration, controller, section-filter, sql-server]
dependency_graph:
  requires: []
  provides: [CpdpItem.Section, CpdpItems.Section column, Mapping()-per-section filtering]
  affects: [Controllers/CMPController.cs, Models/KkjModels.cs, Data/SeedMasterData.cs, Migrations/]
tech_stack:
  added: []
  patterns: [EF Core migration with backfill SQL, per-section LINQ filter]
key_files:
  created:
    - Migrations/20260226010749_AddSectionToCpdpItem.cs
    - Migrations/20260226010749_AddSectionToCpdpItem.Designer.cs
  modified:
    - Models/KkjModels.cs
    - Data/SeedMasterData.cs
    - Controllers/CMPController.cs
    - Migrations/ApplicationDbContextModelSnapshot.cs
decisions:
  - "EF Core migration applied via app auto-migrate (context.Database.Migrate()) rather than dotnet ef database update due to running process"
  - "Backfill SQL was run manually via PowerShell against SQL Server because migration was already recorded before the UPDATE clause was added"
  - "Build verification: only MSBuild EXE copy errors (EXE locked by running process 8956); C# compilation (DLL) succeeded with 0 error CS"
metrics:
  duration: ~15min
  completed: 2026-02-26
  tasks_completed: 2
  files_modified: 5
---

# Quick Task 14: Add Section Column to CpdpItem Model and Migration

**One-liner:** Section discriminator column on CpdpItems table with EF Core migration, backfill of 10 existing rows to RFCC, and per-section WHERE filter in CMPController.Mapping().

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Add Section property to CpdpItem model and seed data | 4a2fefb | Models/KkjModels.cs, Data/SeedMasterData.cs |
| 2 | EF Core migration + backfill + CMPController.Mapping() filter | 58ec72d | Migrations/20260226010749_AddSectionToCpdpItem.cs, Controllers/CMPController.cs, ApplicationDbContextModelSnapshot.cs |

## What Was Built

### Task 1: Model and Seed Data
- Added `public string Section { get; set; } = "";   // RFCC | GAST | NGP | DHT` to `CpdpItem` class in `Models/KkjModels.cs`
- Added `Section = "RFCC"` to all 10 `CpdpItem` objects in `SeedCpdpItemsAsync()` in `Data/SeedMasterData.cs`

### Task 2: Migration and Controller Filter
- Generated EF Core migration `20260226010749_AddSectionToCpdpItem` with `AddColumn<string>` for `Section` on `CpdpItems` table
- Added backfill SQL: `UPDATE CpdpItems SET Section = 'RFCC' WHERE Section IS NULL OR Section = ''`
- Migration was auto-applied by the running app via `context.Database.Migrate()` at startup; backfill was applied manually via PowerShell to SQL Server
- Updated `CMPController.Mapping()` query:
  ```csharp
  var cpdpData = await _context.CpdpItems
      .Where(c => c.Section == section)
      .OrderBy(c => c.No)
      .ToListAsync();
  ```

## Verification Results

- Section column exists in `CpdpItems` table on `HcPortalDB_Dev` (SQL Server): CONFIRMED
- Migration `20260226010749_AddSectionToCpdpItem` recorded in `__EFMigrationsHistory`: CONFIRMED
- All 10 existing `CpdpItems` rows have `Section = 'RFCC'`: CONFIRMED (10 rows updated)
- `CMPController.Mapping()` has `.Where(c => c.Section == section)`: CONFIRMED
- C# compilation: 0 `error CS` errors (DLL updated successfully)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Backfill SQL not executed by dotnet ef database update**

- **Found during:** Task 2
- **Issue:** The project uses SQL Server (`appsettings.Development.json` overrides SQLite with `Server=localhost\SQLEXPRESS;Database=HcPortalDB_Dev`). The running app (hot-reload or auto-startup) had already applied the migration to SQL Server before we added the backfill SQL to the migration file. As a result, `dotnet ef database update` reported "already up to date" and the UPDATE never ran.
- **Fix:** Executed the backfill SQL directly via PowerShell against SQL Server: `UPDATE CpdpItems SET Section = 'RFCC' WHERE Section IS NULL OR Section = ''`
- **Result:** 10 rows updated, verified with COUNT query
- **Files modified:** None (data change only)
- **Commit:** N/A (data fix, not code change)

**2. [Rule 3 - Blocking] Build exits non-zero due to locked EXE**

- **Found during:** Task 2 verification
- **Issue:** The app process (PID 8956) holds a lock on `HcPortal.exe`, preventing MSBuild from copying the output EXE. `dotnet build -q` exits early with exit code 1 on this file copy error.
- **Fix:** Used `dotnet build --no-restore` and filtered for `error CS*` pattern to confirm zero C# compilation errors. The DLL was compiled and written successfully (timestamp confirmed). The EXE copy failure is a non-blocking constraint of the running dev server.
- **Files modified:** None

## Success Criteria Check

- [x] CpdpItem model compiles with Section property
- [x] Migration AddSectionToCpdpItem is applied to HcPortalDB_Dev (SQL Server)
- [x] All 10 existing CpdpItems rows have Section = 'RFCC' in the database
- [x] CMPController.Mapping() filters by section (.Where(c => c.Section == section))
- [x] dotnet build DLL compilation exits 0 (no error CS; EXE copy blocked by locked process is non-blocking)

## Self-Check: PASSED

- Models/KkjModels.cs: Section property added at line 81
- Data/SeedMasterData.cs: Section = "RFCC" in all 10 CpdpItem objects
- Migrations/20260226010749_AddSectionToCpdpItem.cs: created with backfill SQL
- Controllers/CMPController.cs: .Where(c => c.Section == section) confirmed
- Commits: 4a2fefb (Task 1), 58ec72d (Task 2) — both in git log
- SQL Server: Section column exists, 10 rows = 'RFCC', migration recorded
