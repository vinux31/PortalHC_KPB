---
phase: 83-master-data-qa
plan: 05
subsystem: data-model
tags: [soft-delete, ef-core, migration, identity, models]
dependency_graph:
  requires: []
  provides: [ApplicationUser.IsActive, ProtonKompetensi.IsActive, DB schema AddIsActiveToUserAndSilabus]
  affects: [AspNetUsers table, ProtonKompetensiList table]
tech_stack:
  added: []
  patterns: [IsActive soft-delete flag, EF Core data migration, SQL UPDATE via migration]
key_files:
  created:
    - Migrations/20260303073626_AddIsActiveToUserAndSilabus.cs
    - Migrations/20260303073626_AddIsActiveToUserAndSilabus.Designer.cs
    - Migrations/20260303073729_SetExistingRecordsActive.cs
    - Migrations/20260303073729_SetExistingRecordsActive.Designer.cs
  modified:
    - Models/ApplicationUser.cs
    - Models/ProtonModels.cs
    - Migrations/ApplicationDbContextModelSnapshot.cs
decisions:
  - IsActive added to ApplicationUser with XML doc comment after SelectedView property
  - IsActive added to ProtonKompetensi after Urutan property before ProtonTrackId
  - Migration created with --no-build using freshly built temp DLL to bypass running server file lock
  - Data migration SetExistingRecordsActive added to document UPDATE intent; existing 10 users set via direct SQL due to empty migration already applied to DB history
metrics:
  duration: 12 min
  completed_date: "2026-03-03"
  tasks: 2
  files_changed: 7
---

# Phase 83 Plan 05: Add IsActive Schema Foundation Summary

**One-liner:** Added `IsActive bool` to ApplicationUser and ProtonKompetensi models with EF Core migration adding `bit NOT NULL` columns to Users and ProtonKompetensiList tables; all 10 existing users set to active.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Add IsActive to ApplicationUser and ProtonKompetensi models | c4603a1 | Models/ApplicationUser.cs, Models/ProtonModels.cs |
| 2 | Create and apply EF Core migration AddIsActiveToUserAndSilabus | 5b8ce7a | Migrations/20260303073626_*, Migrations/20260303073729_*, Migrations/ApplicationDbContextModelSnapshot.cs |

## Verification Results

- `ApplicationUser.IsActive` — present at line 66, after `SelectedView`
- `ProtonKompetensi.IsActive` — present at line 32, after `Urutan`
- Migration files `20260303073626_AddIsActiveToUserAndSilabus.cs` and `.Designer.cs` exist
- ModelSnapshot contains `IsActive` entries for both `Users` and `ProtonKompetensiList`
- `dotnet build` — 0 CS compiler errors (only MSB3027/MSB3021 file-lock warnings from running server)
- Database: `Users` table has `IsActive bit NOT NULL` column; all 10 existing users set to `IsActive = 1`
- Database: `ProtonKompetensiList` has `IsActive bit NOT NULL` column; 0 rows (no seeded data yet)

## Decisions Made

1. **IsActive placement in ApplicationUser** — After `SelectedView`, before navigation property `TrainingRecords`, with XML doc comment matching project style
2. **IsActive placement in ProtonKompetensi** — After `Urutan`, before `ProtonTrackId`, no doc comment (matches surrounding property style in that class)
3. **Migration approach** — Running server (PID 6964) locks `HcPortal.exe`, preventing normal `dotnet ef` build. Used `dotnet build -o /tmp/hcbuild` to compile to temp dir, then copied DLL to `obj/Debug/net8.0/`, then ran `dotnet ef migrations add --no-build`. This is a standard pattern for EF migrations on running dev servers.
4. **Data initialization** — `SetExistingRecordsActive` migration was created to document the UPDATE intent, but the empty-body migration had already been recorded in DB history. Existing 10 users were set to `IsActive = 1` via direct SQL Server PowerShell command. ProtonKompetensiList had 0 rows requiring no update.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Migration created with empty body due to stale DLL**
- **Found during:** Task 2
- **Issue:** Running server process (PID 6964) locks `HcPortal.exe`, causing `dotnet ef migrations add` to fail at build step. First attempt with `--no-build` used stale DLL (pre-IsActive changes), generating empty migration body.
- **Fix:** Built to `/tmp/hcbuild` temp directory (avoids lock), copied new DLL to `obj/Debug/net8.0/`, re-ran migration. Reverted and cleaned up empty migration before re-running.
- **Files modified:** Migrations/20260303073626_AddIsActiveToUserAndSilabus.cs (proper migration), Migrations/20260303073729_SetExistingRecordsActive.cs (data migration)
- **Commit:** 5b8ce7a

**2. [Rule 1 - Bug] Existing records had IsActive = 0 after migration**
- **Found during:** Task 2 verification
- **Issue:** EF Core migration `AddColumn` with `defaultValue: false` sets column default to `0` for existing rows. SetExistingRecordsActive migration's empty Up() body was already recorded in DB history, so UPDATE SQL in modified migration didn't execute.
- **Fix:** Ran `UPDATE Users SET IsActive = 1` and `UPDATE ProtonKompetensiList SET IsActive = 1` directly via SQL Server PowerShell connection. All 10 users confirmed `IsActive = 1`.
- **Files modified:** None (data-only fix via SQL)
- **Commit:** N/A (data fix only)

## Self-Check: PASSED

- `Models/ApplicationUser.cs` contains `public bool IsActive { get; set; } = true;` at line 66
- `Models/ProtonModels.cs` contains `public bool IsActive { get; set; } = true;` at line 32 (ProtonKompetensi class)
- `Migrations/20260303073626_AddIsActiveToUserAndSilabus.cs` exists with proper Up()/Down() AddColumn calls
- `Migrations/20260303073729_SetExistingRecordsActive.cs` exists with UPDATE SQL statements
- Commits c4603a1 and 5b8ce7a confirmed in git log
- All 10 users in DB have `IsActive = 1` (verified via SQL Server PowerShell query)
