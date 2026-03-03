---
phase: 93-worker-view-cleanup
plan: 02
subsystem: database
tags: [ef-core, migration, cpdp, cleanup, sql-server]

# Dependency graph
requires:
  - phase: 93-01
    provides: CMPController.Mapping rewritten to use CpdpFiles — CpdpItem no longer queried in CMPController

provides:
  - CpdpItem model class and GapAnalysisItem class removed from Models/KkjModels.cs
  - DbSet<CpdpItem> and OnModelCreating config removed from ApplicationDbContext.cs
  - SeedCpdpItemsAsync method removed from Data/SeedMasterData.cs
  - SeedCpdpItemsAsync call removed from Program.cs
  - Views/CMP/MappingSectionSelect.cshtml deleted
  - Admin CpdpItems CRUD actions (5 actions) removed from AdminController.cs
  - Views/Admin/CpdpItems.cshtml deleted
  - KKJ-IDP Mapping card removed from Views/Admin/Index.cshtml
  - EF Core migration DropCpdpItems applied — CpdpItems table dropped from database
  - Zero CpdpItem references remain in Controllers/, Models/, Data/, Views/

affects:
  - future phases touching AdminController (CpdpItems CRUD gone)
  - future phases touching database schema (CpdpItems table gone)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Total removal pattern: delete model + DbSet + OnModelCreating + seed method + seed call + view + controller actions in one plan, then create+apply migration"

key-files:
  created:
    - Migrations/20260303044201_DropCpdpItems.cs
    - Migrations/20260303044201_DropCpdpItems.Designer.cs
  modified:
    - Models/KkjModels.cs
    - Data/ApplicationDbContext.cs
    - Data/SeedMasterData.cs
    - Program.cs
    - Controllers/AdminController.cs
    - Views/Admin/Index.cshtml
    - Migrations/ApplicationDbContextModelSnapshot.cs
  deleted:
    - Views/CMP/MappingSectionSelect.cshtml
    - Views/Admin/CpdpItems.cshtml

key-decisions:
  - "[93-02]: Admin CpdpItems CRUD actions and view removed as part of total cleanup — required for build to pass after model deletion"
  - "[93-02]: GapAnalysisItem deleted — verified no references outside KkjModels.cs (only in planning files)"
  - "[93-02]: KKJ-IDP Mapping card removed from Admin/Index.cshtml — no replacement needed, file-based CPDP is accessed via CPDP File Management card"

patterns-established:
  - "EF migration naming: DropXxx for table removal migrations"

requirements-completed:
  - CPDP-07

# Metrics
duration: 15min
completed: 2026-03-03
---

# Phase 93 Plan 02: CpdpItem Infrastructure Removal Summary

**CpdpItem model, DbSet, seed data, Admin CRUD, and database table permanently removed via EF migration DropCpdpItems**

## Performance

- **Duration:** 15 min
- **Started:** 2026-03-03T04:40:00Z
- **Completed:** 2026-03-03T04:55:00Z
- **Tasks:** 2
- **Files modified:** 9 (7 modified, 2 deleted, 2 created)

## Accomplishments
- Removed CpdpItem and GapAnalysisItem classes from KkjModels.cs — model layer clean
- Removed all Admin CpdpItems CRUD (5 actions + view + hub card) — controller and view layers clean
- Created and applied EF migration 20260303044201_DropCpdpItems — database table physically dropped
- Build passes with 0 errors; zero CpdpItem references in Controllers/Models/Data/Views/

## Task Commits

Each task was committed atomically:

1. **Task 1: Remove CpdpItem model, DbSet, seed code, and delete views** - `90f0db9` (feat)
2. **Task 2: Create and apply EF Core migration to drop CpdpItems table** - `87d0952` (feat)

## Files Created/Modified
- `Models/KkjModels.cs` - CpdpItem and GapAnalysisItem classes deleted
- `Data/ApplicationDbContext.cs` - DbSet<CpdpItem> and OnModelCreating CpdpItem config removed
- `Data/SeedMasterData.cs` - SeedCpdpItemsAsync method removed; class comment updated
- `Program.cs` - SeedCpdpItemsAsync call removed
- `Controllers/AdminController.cs` - 5 CpdpItems CRUD actions removed (CpdpItems, CpdpItemsSave, CpdpItemDelete, CpdpItemsExport, CpdpItemsBackup)
- `Views/Admin/Index.cshtml` - KKJ-IDP Mapping card removed
- `Migrations/20260303044201_DropCpdpItems.cs` - EF migration: DropTable("CpdpItems") in Up(), CreateTable for rollback in Down()
- `Migrations/20260303044201_DropCpdpItems.Designer.cs` - EF migration designer file
- `Migrations/ApplicationDbContextModelSnapshot.cs` - Snapshot updated to reflect CpdpItems removed
- `Views/CMP/MappingSectionSelect.cshtml` - DELETED (replaced by tabbed layout from 93-01)
- `Views/Admin/CpdpItems.cshtml` - DELETED (obsolete spreadsheet editor, replaced by file-based CPDP)

## Decisions Made
- Admin CpdpItems CRUD actions and view removed as part of total cleanup — the plan's truth "no CpdpItem references in controllers or views" required removing these too (they wouldn't compile after model deletion)
- GapAnalysisItem deleted — verified no references in Controllers/Views (only in .planning files)
- KKJ-IDP Mapping card removed from Admin/Index.cshtml — workers access CPDP documents via CMP/Mapping (file download view); admins manage uploads via Admin/CpdpFiles

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Removed Admin CpdpItems CRUD and view not listed in plan artifacts**
- **Found during:** Task 1 (Remove CpdpItem model, DbSet, seed code)
- **Issue:** Plan listed artifacts for KkjModels.cs, ApplicationDbContext.cs, SeedMasterData.cs, Program.cs but did NOT list AdminController.cs or Views/Admin/CpdpItems.cshtml. However, these files reference CpdpItem directly and would not compile after model deletion. The plan's truth "No reference to CpdpItem...remains in controllers or views" required their removal.
- **Fix:** Removed 5 AdminController actions (CpdpItems, CpdpItemsSave, CpdpItemDelete, CpdpItemsExport, CpdpItemsBackup), deleted Views/Admin/CpdpItems.cshtml, removed CpdpItems card from Views/Admin/Index.cshtml
- **Files modified:** Controllers/AdminController.cs, Views/Admin/CpdpItems.cshtml (deleted), Views/Admin/Index.cshtml
- **Verification:** Build passes, zero CpdpItem references in Controllers/ and Views/
- **Committed in:** 90f0db9 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - build-blocking bug)
**Impact on plan:** Auto-fix required for build to pass. No scope creep — Admin CpdpItems CRUD was obsolete infrastructure superseded by Phase 92 file-based replacement.

## Issues Encountered
None — migration scaffolding correctly detected CpdpItems table removal and generated DropTable in Up().

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Phase 93 complete: worker view (93-01) and cleanup (93-02) both done
- CpdpItems table permanently gone from database — no rollback needed
- Build is clean, application starts without CpdpItem-related errors
- Ready for next phase or milestone close

## Self-Check: PASSED

- FOUND: Migrations/20260303044201_DropCpdpItems.cs
- FOUND: Migrations/20260303044201_DropCpdpItems.Designer.cs
- CONFIRMED DELETED: Views/CMP/MappingSectionSelect.cshtml
- CONFIRMED DELETED: Views/Admin/CpdpItems.cshtml
- FOUND commits: 90f0db9 (task 1), 87d0952 (task 2)
- Zero CpdpItem references in Controllers/Models/Data/Views/ (only in comment)
- Zero MappingSectionSelect references anywhere
- dotnet build passes with 0 errors

---
*Phase: 93-worker-view-cleanup*
*Completed: 2026-03-03*
