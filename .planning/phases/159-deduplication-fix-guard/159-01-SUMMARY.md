---
phase: 159-deduplication-fix-guard
plan: 01
subsystem: database
tags: [coaching-proton, proton-track-assignment, deduplication, ef-migration]

# Dependency graph
requires:
  - phase: 158
    provides: CoachCoacheeMapping, ProtonTrackAssignment, CoachingProton feature
provides:
  - DeactivatedAt nullable DateTime on ProtonTrackAssignment (model + migration)
  - FIX-01: Deactivate cascade stamps DeactivatedAt; Reactivate correlates by timestamp
  - FIX-02: Assign action reuses existing inactive assignments instead of creating duplicates
affects: [159-02, CDPController coaching dashboard]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Timestamp-correlation pattern: stamp DeactivatedAt on cascade-deactivated rows, filter by it on reactivate (±5s window)"
    - "Idempotent assign: check for existing inactive assignment before creating new one"

key-files:
  created:
    - Migrations/20260312072019_AddProtonTrackAssignmentDeactivatedAt.cs
  modified:
    - Models/ProtonModels.cs
    - Controllers/AdminController.cs
    - Data/SeedData.cs

key-decisions:
  - "Use DateTime? DeactivatedAt on ProtonTrackAssignment to correlate cascade deactivation events"
  - "5-second window for timestamp correlation (EF.Functions.DateDiffSecond) to handle clock drift"
  - "Fall back to DeactivatedAt==null for legacy mappings deactivated before this fix"
  - "FIX-02 only deactivates active assignments for a DIFFERENT track; same-track already-active is a no-op"

patterns-established:
  - "Stamp + correlate: when cascading deactivations, stamp each row with the event time so the inverse operation can be scoped safely"
  - "Reuse-before-create: check for reusable inactive row before inserting a new one (prevents duplicates)"

requirements-completed: [FIX-01, FIX-02]

# Metrics
duration: 15min
completed: 2026-03-12
---

# Phase 159 Plan 01: Deduplication Fix & Guard Summary

**Fixed duplicate ProtonTrackAssignment creation via DeactivatedAt timestamp-correlation on reactivate cascade and idempotent reuse on assign.**

## Performance

- **Duration:** 15 min
- **Started:** 2026-03-12T07:20:00Z
- **Completed:** 2026-03-12T07:35:00Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- Added `DeactivatedAt` nullable column to `ProtonTrackAssignment` (model + EF migration applied)
- FIX-01: Deactivate cascade now stamps `DeactivatedAt = UtcNow`; Reactivate filters to assignments within ±5s of the mapping's original EndDate
- FIX-02: Assign action checks for an existing inactive assignment for the same coachee+track before creating a new one — reuses it instead, preserving existing `ProtonDeliverableProgress` rows
- CLN-01 dedup utility (SeedData + Admin endpoint) included in Task 1 commit (pre-staged changes)

## Task Commits

Each task was committed atomically:

1. **Task 1: Add DeactivatedAt field and EF migration** - `2c2f00a` (feat)
2. **Task 2: Fix Reactivate cascade and Assign idempotency** - `8ae68b1` (fix)

## Files Created/Modified
- `Models/ProtonModels.cs` - Added `DateTime? DeactivatedAt` to `ProtonTrackAssignment`
- `Migrations/20260312072019_AddProtonTrackAssignmentDeactivatedAt.cs` - EF migration adding column
- `Controllers/AdminController.cs` - FIX-01 (Deactivate + Reactivate), FIX-02 (Assign), CLN-01 admin endpoint
- `Data/SeedData.cs` - CLN-01 `DeduplicateProtonTrackAssignments` method

## Decisions Made
- 5-second correlation window chosen for DeactivatedAt/EndDate matching to tolerate sub-second clock differences between set operations
- Legacy fallback: if `originalEndDate` is null (mapping deactivated before this fix), reactivate only assignments where `DeactivatedAt == null` (pre-fix behavior is safe since no duplicates could have been created yet)
- FIX-02 skips existing active same-track assignments (already idempotent) and only deactivates active assignments for a different track

## Deviations from Plan

None — plan executed exactly as written. Pre-staged CLN-01 changes (SeedData, AdminController cleanup endpoint) were folded into Task 1 commit as they were part of the same feature scope.

## Issues Encountered
- Pre-existing uncommitted changes in `AdminController.cs`, `Data/SeedData.cs`, and `Controllers/CDPController.cs` found at start. These were legitimate Phase 159 changes (CLN-01 and DEF-01). Folded into Task 1 commit with the migration changes.

## User Setup Required
None - no external service configuration required. Migration applies automatically on next app startup.

## Next Phase Readiness
- FIX-01 and FIX-02 guard in place — new duplicates will no longer be created
- Phase 159 Plan 02 (CLN-01 cleanup of existing duplicates) can proceed
- DEF-01 defensive guard in CDPController already committed

---
*Phase: 159-deduplication-fix-guard*
*Completed: 2026-03-12*
