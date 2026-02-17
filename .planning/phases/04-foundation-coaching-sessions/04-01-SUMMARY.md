---
phase: 04-foundation-coaching-sessions
plan: 01
subsystem: database
tags: [ef-core, sql-server, models, migrations, coaching]

# Dependency graph
requires:
  - phase: 03-kkj-cpdp-integration
    provides: ApplicationDbContext with UserCompetencyLevel and competency tracking foundation
provides:
  - CoachingSession entity with date, topic, notes, status, coach/coachee string IDs
  - ActionItem entity with FK to CoachingSession (Cascade delete)
  - CoachingHistoryViewModel, CreateSessionViewModel, AddActionItemViewModel
  - CoachCoacheeMapping registered in DbContext
  - EF migration: CoachingSessions, ActionItems, CoachCoacheeMappings tables created; TrackingItemId dropped from CoachingLogs
affects: [04-02, 04-03, 05-proton-assignment, phase5, phase6]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "String IDs for coach/coachee (no FK) — matches CoachingLog pattern for loose coupling"
    - "Cascade delete from CoachingSession to ActionItems via HasForeignKey + OnDelete"
    - "ICollection<ActionItem> navigation property with List<ActionItem> initialization"

key-files:
  created:
    - Models/CoachingSession.cs
    - Models/ActionItem.cs
    - Models/CoachingViewModels.cs
    - Migrations/20260217044811_AddCoachingFoundation.cs
  modified:
    - Models/CoachingLog.cs
    - Data/ApplicationDbContext.cs

key-decisions:
  - "String IDs for CoachId/CoacheeId in CoachingSession — no FK constraint, matches existing CoachingLog pattern"
  - "CoachCoacheeMapping registered in DbContext now (Phase 4) though used in Phase 5 — fixes orphaned model"
  - "dotnet ef commands require --configuration Release flag when Debug exe is locked by running process"

patterns-established:
  - "Use --configuration Release for dotnet ef migrations/database commands when dev server is running"

# Metrics
duration: 4min
completed: 2026-02-17
---

# Phase 4 Plan 01: Foundation — Coaching Sessions Data Layer Summary

**CoachingSession and ActionItem EF Core models with migration creating 3 new tables, registering CoachCoacheeMapping, and dropping the broken TrackingItemId column from CoachingLogs**

## Performance

- **Duration:** 4 min
- **Started:** 2026-02-17T04:45:57Z
- **Completed:** 2026-02-17T04:49:54Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments
- Created CoachingSession entity (date, topic, notes, status Draft/Submitted, string coach/coachee IDs, navigation to ActionItems)
- Created ActionItem entity (description, due date, status Open/InProgress/Done, FK to CoachingSession with Cascade delete)
- Created CoachingViewModels.cs with CoachingHistoryViewModel, CreateSessionViewModel, AddActionItemViewModel
- Removed broken TrackingItemId property from CoachingLog.cs
- Registered CoachingSession, ActionItem, CoachCoacheeMapping in ApplicationDbContext with indexes and relationship config
- Applied migration AddCoachingFoundation: 3 new tables created, TrackingItemId dropped from CoachingLogs

## Task Commits

Each task was committed atomically:

1. **Task 1: Create CoachingSession, ActionItem models and ViewModels** - `b049fd8` (feat)
2. **Task 2: Register entities in DbContext and run migration** - `b9bb330` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified
- `Models/CoachingSession.cs` - New entity: coaching session with date, topic, notes, status, coach/coachee IDs, ActionItems collection
- `Models/ActionItem.cs` - New entity: action item with description, due date, status, FK to CoachingSession
- `Models/CoachingViewModels.cs` - ViewModels for coaching views: history/filters, create session form, add action item form
- `Models/CoachingLog.cs` - Removed TrackingItemId property (broken FK to non-existent table)
- `Data/ApplicationDbContext.cs` - Added 3 DbSets and OnModelCreating config for CoachingSession, ActionItem, CoachCoacheeMapping
- `Migrations/20260217044811_AddCoachingFoundation.cs` - Migration: DropColumn TrackingItemId, CreateTable CoachCoacheeMappings/CoachingSessions/ActionItems

## Decisions Made
- String IDs (no FK constraints) for CoachId/CoacheeId in CoachingSession — consistent with existing CoachingLog pattern, avoids cascading FK issues with Identity User table
- Registered CoachCoacheeMapping in DbContext now (Phase 4) even though it's used in Phase 5 — it was an orphaned model causing model-snapshot drift
- Used `--configuration Release` flag for `dotnet ef` commands because the Debug exe was locked by a running process

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

- The running HcPortal process (PID 9880) locked `bin/Debug/net8.0/HcPortal.exe`, causing `dotnet build` (Debug) to fail on file copy. Resolved by using `dotnet build -c Release` and `dotnet ef ... --configuration Release` for all subsequent commands — compilation was always successful.

## User Setup Required

None - no external service configuration required.

## Self-Check: PASSED

- Models/CoachingSession.cs: FOUND
- Models/ActionItem.cs: FOUND
- Models/CoachingViewModels.cs: FOUND
- Migrations/20260217044811_AddCoachingFoundation.cs: FOUND
- DbSet<CoachingSession> in ApplicationDbContext.cs: FOUND
- DbSet<ActionItem> in ApplicationDbContext.cs: FOUND
- DbSet<CoachCoacheeMapping> in ApplicationDbContext.cs: FOUND
- TrackingItemId removed from CoachingLog.cs: PASS
- Commit b049fd8: FOUND
- Commit b9bb330: FOUND
- dotnet build -c Release: 0 errors

## Next Phase Readiness
- Data layer complete — CoachingSessions and ActionItems tables exist in database with correct schema
- ApplicationDbContext ready for COACH-01/02/03 controller work in Phase 4 plans 02-04
- CoachCoacheeMapping table available for Phase 5 Proton assignment work
- TrackingItemId column removed — no more broken FK reference in CoachingLogs

---
*Phase: 04-foundation-coaching-sessions*
*Completed: 2026-02-17*
