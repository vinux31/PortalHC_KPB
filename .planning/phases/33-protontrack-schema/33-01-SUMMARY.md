---
phase: 33-protontrack-schema
plan: 01
subsystem: database
tags: [efcore, sql-server, migration, schema, data-migration]

# Dependency graph
requires:
  - phase: 31-hc-reporting-actions
    provides: CDPController Proton workflow (AssignTrack, PlanIdp, Deliverable, ApproveDeliverable) baseline
provides:
  - ProtonTrack entity with Id, TrackType, TahunKe, DisplayName, Urutan — single source of truth for track identity
  - ProtonTracks database table with 6 seeded rows (Panelman x3, Operator x3)
  - ProtonKompetensiList.ProtonTrackId FK column (NOT NULL, Cascade delete)
  - ProtonTrackAssignments.ProtonTrackId FK column (NOT NULL, Restrict delete)
  - TrackType + TahunKe string columns eliminated from both tables
  - CDPController updated to query by ProtonTrackId FK instead of string fields
affects:
  - phase 33-02 (CDPController consumer cleanup — now pre-completed in this plan)
  - phase 34-catalog-ui (ProtonTrack dropdown source)
  - phase 35-catalog-ui (catalog management reads ProtonTrackId)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Atomic EF migration: create table → add nullable FK → FK constraints → seed via MERGE → backfill → validate → make NOT NULL → drop old cols"
    - "MERGE INTO pattern for idempotent seeding in migrations"
    - "RAISERROR validation guard inside migration to fail loudly on incomplete backfill"
    - "Include(a => a.ProtonTrack) pattern for eager loading track info in assignment queries"

key-files:
  created:
    - Migrations/20260223060707_CreateProtonTrackTable.cs
    - Migrations/20260223060707_CreateProtonTrackTable.Designer.cs
  modified:
    - Models/ProtonModels.cs
    - Data/ApplicationDbContext.cs
    - Data/SeedProtonData.cs
    - Controllers/CDPController.cs
    - Views/CDP/ProtonMain.cshtml
    - Views/CDP/CreateFinalAssessment.cshtml
    - Migrations/ApplicationDbContextModelSnapshot.cs

key-decisions:
  - "Single atomic migration handles all 10 steps (create, seed, backfill, validate, NOT NULL, drop) — no split migrations"
  - "MERGE INTO used instead of INSERT for idempotent track seeding — safe on repeated migrations"
  - "DeleteBehavior.Cascade for ProtonKompetensi->ProtonTrack (catalog item deletion cascades); DeleteBehavior.Restrict for ProtonTrackAssignment->ProtonTrack (preserve assignment history)"
  - "CDPController consumer fixes implemented now (Rule 3 blocking deviation) rather than deferred to Plan 02 — needed for project to compile and EF to scaffold migration"
  - "AssignTrack action now accepts protonTrackId (int) instead of trackType+tahunKe strings"
  - "ProtonMain.cshtml AssignTrack form replaced two string dropdowns with single ProtonTrack ID dropdown"

patterns-established:
  - "ProtonTrack as FK source: all Proton queries filter by ProtonTrackId, not string fields"

# Metrics
duration: 14min
completed: 2026-02-23
---

# Phase 33 Plan 01: ProtonTrack Schema Migration Summary

**ProtonTrack normalized entity + atomic EF migration seeding 6 tracks, backfilling ProtonTrackId FK on ProtonKompetensiList and ProtonTrackAssignments, and dropping TrackType/TahunKe string columns — CDPController updated to query by ProtonTrackId**

## Performance

- **Duration:** 14 min
- **Started:** 2026-02-23T05:56:18Z
- **Completed:** 2026-02-23T06:10:32Z
- **Tasks:** 3
- **Files modified:** 7 + 2 created

## Accomplishments
- ProtonTrack entity created and registered in DbContext with unique constraint on (TrackType, TahunKe) and proper FK delete behaviors
- Atomic migration applied: ProtonTracks table created, 6 rows seeded (Panelman/Operator x Tahun 1/2/3), existing data backfilled with RAISERROR validation, old string columns dropped
- CDPController refactored to use ProtonTrackId FK for all Proton queries (PlanIdp, AssignTrack, Deliverable, ApproveDeliverable, HCApprovals, BuildCoacheeSubModel, BuildProtonProgressSubModel)
- ProtonMain.cshtml AssignTrack form updated from two string dropdowns to single ProtonTrack ID dropdown populated from ViewBag.ProtonTracks
- SeedProtonData.cs updated to seed only ProtonTrack rows (catalog items managed via Phase 35 UI)
- Project builds with 0 errors and 0 warnings

## Task Commits

Each task was committed atomically:

1. **Task 1: Add ProtonTrack entity and update models** - `0832704` (feat)
2. **Task 2: Register ProtonTrack in DbContext** - `e5aadfe` (feat)
3. **Task 3: Generate and apply migration + blocking consumer fixes** - `c31b758` (feat)

## Files Created/Modified
- `Models/ProtonModels.cs` - ProtonTrack class added; TrackType/TahunKe removed from ProtonKompetensi and ProtonTrackAssignment; ProtonTrackId FK properties added
- `Data/ApplicationDbContext.cs` - DbSet<ProtonTrack>, ProtonTrack entity config with unique index, FK relationship configs for both child tables
- `Migrations/20260223060707_CreateProtonTrackTable.cs` - Full 10-step atomic migration with MERGE seed, backfill, RAISERROR validation
- `Migrations/20260223060707_CreateProtonTrackTable.Designer.cs` - EF model snapshot for migration
- `Migrations/ApplicationDbContextModelSnapshot.cs` - Updated model snapshot with ProtonTracks and ProtonTrackId FKs
- `Data/SeedProtonData.cs` - Updated to seed 6 ProtonTrack rows only (no longer seeds kompetensi catalog)
- `Controllers/CDPController.cs` - All Proton actions updated to use ProtonTrackId FK; AssignTrack accepts protonTrackId int; Include(a => a.ProtonTrack) added to assignment queries
- `Views/CDP/ProtonMain.cshtml` - AssignTrack form uses single protonTrackId dropdown from ViewBag.ProtonTracks; display uses ProtonTrack.DisplayName
- `Views/CDP/CreateFinalAssessment.cshtml` - Track display uses ProtonTrack.DisplayName via ProtonTrackAssignment.ProtonTrack navigation

## Decisions Made
- Single atomic migration chosen over split migrations — simpler rollback story, all-or-nothing semantics
- MERGE INTO used for seeding to be idempotent (safe if migration is retried)
- CDPController consumer fixes implemented now as a blocking deviation rather than deferred to Plan 02 — without them, the project cannot compile and EF cannot scaffold a proper migration

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] CDPController and views updated to compile with removed entity properties**
- **Found during:** Task 3 (Generate and apply migration)
- **Issue:** After removing TrackType/TahunKe from entities, CDPController.cs and 2 views had 45 compile errors. EF migrations add requires a successful build to generate a model-aware migration. Could not defer to Plan 02 because the project wouldn't compile.
- **Fix:** Updated all CDPController Proton actions to use ProtonTrackId FK and Include(ProtonTrack) for display. Updated AssignTrack to accept protonTrackId (int). Updated ProtonMain.cshtml AssignTrack form to use ProtonTrack dropdown. Updated CreateFinalAssessment.cshtml to use ProtonTrack.DisplayName. Updated SeedProtonData.cs to seed only ProtonTrack rows.
- **Files modified:** Controllers/CDPController.cs, Views/CDP/ProtonMain.cshtml, Views/CDP/CreateFinalAssessment.cshtml, Data/SeedProtonData.cs
- **Verification:** dotnet build passes with 0 errors and 0 warnings
- **Committed in:** c31b758 (Task 3 commit)

---

**Total deviations:** 1 auto-fixed (1 Rule 3 blocking)
**Impact on plan:** The blocking fix was essential — it implements what Plan 02 was going to do, but Plan 02 can now verify and potentially enhance the implementation. No scope creep beyond what Plan 02 specified.

## Issues Encountered

None — migration applied cleanly. RAISERROR validation guard did not trigger (all existing rows had valid TrackType+TahunKe that matched the 6 seeded ProtonTrack rows). dotnet build passes with 0 errors.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- ProtonTrack schema complete, migration applied, 6 tracks seeded
- CDPController consumer code already updated (Plan 02 can focus on verification and any remaining edge cases)
- Phase 34 can query ProtonTracks for dropdown population
- Phase 35 catalog UI can use ProtonTrackId FK for all kompetensi filtering

---
## Self-Check: PASSED

All key files verified on disk. All task commits verified in git history.

*Phase: 33-protontrack-schema*
*Completed: 2026-02-23*
