---
phase: 05-proton-deliverable-tracking
plan: 01
subsystem: database
tags: [efcore, sqlserver, migrations, seed-data, models]

# Dependency graph
requires:
  - phase: 04-foundation-coaching-sessions
    provides: CoachCoacheeMapping model registered in DbContext, ApplicationUser with RoleLevel

provides:
  - "5 Proton entity models: ProtonKompetensi, ProtonSubKompetensi, ProtonDeliverable, ProtonTrackAssignment, ProtonDeliverableProgress"
  - "3 Proton ViewModels: ProtonMainViewModel, ProtonPlanViewModel, DeliverableViewModel"
  - "5 new database tables via AddProtonDeliverableTracking migration"
  - "Seed data: Operator Tahun 1 (3 Kompetensi, 6 SubKompetensi, 13 Deliverables) plus placeholders for Panelman and Tahun 2/3"
  - "DbContext configured with FK relationships (DeleteBehavior.Restrict) and performance indexes"

affects:
  - 05-02 (ProtonController controller — builds on models and DbSets)
  - 05-03 (Views — uses ProtonMainViewModel, ProtonPlanViewModel, DeliverableViewModel)
  - 05-04 (Evidence upload — ProtonDeliverableProgress.EvidencePath and EvidenceFileName)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "No FK constraints on CoacheeId/AssignedById — matches CoachingLog/CoachCoacheeMapping string ID pattern"
    - "DeleteBehavior.Restrict on all Proton FK relationships — avoids SQL Server multiple cascade path"
    - "Unique index on (CoacheeId, ProtonDeliverableId) enforces one progress record per user per deliverable"
    - "Idempotent seed with AnyAsync() guard — matches SeedMasterData pattern"

key-files:
  created:
    - Models/ProtonModels.cs
    - Models/ProtonViewModels.cs
    - Data/SeedProtonData.cs
    - Migrations/20260217063156_AddProtonDeliverableTracking.cs
  modified:
    - Data/ApplicationDbContext.cs
    - Program.cs

key-decisions:
  - "String IDs for CoacheeId/AssignedById in ProtonTrackAssignment — no FK constraint, matches existing CoachingLog pattern"
  - "DeleteBehavior.Restrict on all Proton FK relationships to avoid SQL Server multiple cascade path limitation"
  - "Unique index on (CoacheeId, ProtonDeliverableId) in ProtonDeliverableProgress to enforce one record per user per deliverable"
  - "Seed Operator Tahun 1 with real CPDP-derived data, placeholders for Panelman and Tahun 2/3 with TODO comments"

patterns-established:
  - "ProtonKompetensi.TrackType values: 'Panelman' or 'Operator'"
  - "ProtonKompetensi.TahunKe values: 'Tahun 1', 'Tahun 2', 'Tahun 3'"
  - "ProtonDeliverableProgress.Status values: 'Locked', 'Active', 'Submitted', 'Approved', 'Rejected'"

# Metrics
duration: 3min
completed: 2026-02-17
---

# Phase 5 Plan 01: Proton Deliverable Tracking — Data Foundation Summary

**Five EF Core entity models with complete Kompetensi > SubKompetensi > Deliverable hierarchy, per-user tracking tables, ViewModels for all PROTN screens, and seeded Operator Tahun 1 real data via AddProtonDeliverableTracking migration**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-02-17T06:29:47Z
- **Completed:** 2026-02-17T06:33:39Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments

- Created 5 entity models covering master hierarchy (ProtonKompetensi, ProtonSubKompetensi, ProtonDeliverable) and per-user tracking (ProtonTrackAssignment, ProtonDeliverableProgress)
- Created 3 ViewModels ready for Phase 5 Plans 02 and 03: ProtonMainViewModel (coach view), ProtonPlanViewModel (coachee IDP view), DeliverableViewModel (evidence upload view)
- Applied migration creating all 5 tables with correct schema, all FKs using ReferentialAction.Restrict, and appropriate indexes including unique (CoacheeId, ProtonDeliverableId)
- Seeded Operator Tahun 1 with 3 real Kompetensi, 6 SubKompetensi, 13 Deliverables derived from actual CPDP data; placeholders for Panelman Tahun 1, Operator Tahun 2, Operator Tahun 3

## Task Commits

Each task was committed atomically:

1. **Task 1: Create Proton entity models and ViewModels** - `c6a88b2` (feat)
2. **Task 2: Register entities in DbContext, create migration, seed data** - `cb440fc` (feat)

## Files Created/Modified

- `Models/ProtonModels.cs` — 5 entity classes: ProtonKompetensi, ProtonSubKompetensi, ProtonDeliverable, ProtonTrackAssignment, ProtonDeliverableProgress
- `Models/ProtonViewModels.cs` — 3 ViewModels: ProtonMainViewModel (with ActiveProgresses), ProtonPlanViewModel (with ActiveProgress), DeliverableViewModel (with IsAccessible, CanUpload)
- `Data/ApplicationDbContext.cs` — 5 new DbSet properties and OnModelCreating FK + index configuration
- `Data/SeedProtonData.cs` — Idempotent seed class with Operator Tahun 1 real data and placeholders
- `Program.cs` — Added SeedProtonData.SeedAsync call in startup pipeline
- `Migrations/20260217063156_AddProtonDeliverableTracking.cs` — Migration with 5 CreateTable operations

## Decisions Made

- String IDs (no FK constraint) for CoacheeId/AssignedById in ProtonTrackAssignment — consistent with CoachingLog and CoachCoacheeMapping pattern in this codebase
- DeleteBehavior.Restrict on all Proton FK relationships — required to avoid SQL Server multiple cascade path limitation
- Unique index on (CoacheeId, ProtonDeliverableId) in ProtonDeliverableProgress — enforces one progress record per user per deliverable at the database level
- Seeded Operator Tahun 1 with real CPDP-derived data (K1: Safe Work Practice, K2: Dasar Operasi Kilang, K3: Keselamatan Proses); Panelman and Tahun 2/3 left as clearly-marked placeholders with TODO comments

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- All 5 Proton tables exist in database with seeded master data
- All DbSet properties registered and accessible for controller queries
- ProtonMainViewModel.ActiveProgresses and ProtonPlanViewModel.ActiveProgress properties ready for PROTN-01/02 navigation logic
- DeliverableViewModel ready for evidence upload (PROTN-04/05)
- Plans 02 and 03 can proceed with controller actions and views building on this foundation

---
*Phase: 05-proton-deliverable-tracking*
*Completed: 2026-02-17*

## Self-Check: PASSED

- Models/ProtonModels.cs: FOUND
- Models/ProtonViewModels.cs: FOUND
- Data/SeedProtonData.cs: FOUND
- Migrations/20260217063156_AddProtonDeliverableTracking.cs: FOUND
- Commit c6a88b2 (Task 1): FOUND
- Commit cb440fc (Task 2): FOUND
