---
phase: 91-data-model-migration
plan: 02
subsystem: database
tags: [ef-core, migration, csharp, sql-server, cpdp]

# Dependency graph
requires:
  - phase: 91-01
    provides: CpdpItemsBackup export so existing data is safe before new table is added
provides:
  - CpdpFile entity class in Models/KkjModels.cs (mirrors KkjFile exactly)
  - DbSet<CpdpFile> CpdpFiles registered in ApplicationDbContext
  - EF Core migration 20260303000729_AddCpdpFiles applied to database
  - CpdpFiles table in SQL Server with FK to KkjBagians (ON DELETE CASCADE)
affects:
  - 91-03 (AdminController CPDP rewrite will use CpdpFile)
  - 92 (admin rewrite phase depends on CpdpFiles table existing)
  - 93 (worker view + cleanup phase)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "CpdpFile mirrors KkjFile exactly — same 11 properties, same FK pattern to KkjBagian"
    - "WithMany() (no collection nav on principal) used for CpdpFile FK to avoid adding nav property to KkjBagian"

key-files:
  created:
    - Migrations/20260303000729_AddCpdpFiles.cs
    - Migrations/20260303000729_AddCpdpFiles.Designer.cs
  modified:
    - Models/KkjModels.cs
    - Data/ApplicationDbContext.cs
    - Migrations/ApplicationDbContextModelSnapshot.cs

key-decisions:
  - "CpdpFile.Bagian uses WithMany() (no nav collection on KkjBagian) — EF Core handles FK without bidirectional collection"
  - "CpdpItems table NOT dropped in this plan — Phase 93 handles cleanup"

patterns-established:
  - "CpdpFile mirrors KkjFile: identical property set, same FK pattern, same upload metadata fields"

requirements-completed: [CPDP-06]

# Metrics
duration: 1min
completed: 2026-03-03
---

# Phase 91 Plan 02: CpdpFile Model + EF Core Migration Summary

**CpdpFile entity added to KkjModels.cs mirroring KkjFile, registered in ApplicationDbContext, and CpdpFiles SQL Server table created via EF Core migration AddCpdpFiles with FK to KkjBagians**

## Performance

- **Duration:** 1 min
- **Started:** 2026-03-03T00:06:23Z
- **Completed:** 2026-03-03T00:07:30Z
- **Tasks:** 3
- **Files modified:** 5 (Models/KkjModels.cs, Data/ApplicationDbContext.cs, 3 migration files)

## Accomplishments
- CpdpFile class added to Models/KkjModels.cs with all 11 properties matching KkjFile exactly
- DbSet<CpdpFile> CpdpFiles and OnModelCreating config block added to ApplicationDbContext
- EF Core migration 20260303000729_AddCpdpFiles created and applied — CpdpFiles table exists in database
- CpdpItems table untouched (confirmed by successful build)

## Task Commits

Each task was committed atomically:

1. **Tasks 1+2: CpdpFile model + ApplicationDbContext registration** - `c5e6c89` (feat)
2. **Task 3: EF Core migration AddCpdpFiles** - `fb78f96` (chore)

## Files Created/Modified
- `Models/KkjModels.cs` - Added CpdpFile class after KkjFile (lines 30-44)
- `Data/ApplicationDbContext.cs` - Added DbSet<CpdpFile> CpdpFiles and entity config block
- `Migrations/20260303000729_AddCpdpFiles.cs` - Migration Up() creates CpdpFiles table with all columns
- `Migrations/20260303000729_AddCpdpFiles.Designer.cs` - EF Core designer snapshot
- `Migrations/ApplicationDbContextModelSnapshot.cs` - Updated model snapshot

## Decisions Made
- CpdpFile.Bagian FK uses `.WithMany()` (no collection nav property on KkjBagian) — avoids cluttering KkjBagian with a second nav collection while EF Core still enforces the FK constraint correctly.
- Tasks 1 and 2 committed together since model and DbContext registration are tightly coupled and must compile as a unit.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required. Migration applied automatically via `dotnet ef database update`.

## Next Phase Readiness
- CpdpFiles table is live in the database
- CpdpFile entity is fully registered and usable in controllers/services
- Ready for Phase 92 (admin rewrite) to implement upload, list, and archive actions against CpdpFiles
- CpdpItems table still intact for Phase 93 cleanup

---
*Phase: 91-data-model-migration*
*Completed: 2026-03-03*
