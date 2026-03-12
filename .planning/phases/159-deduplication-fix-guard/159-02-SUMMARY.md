---
phase: 159-deduplication-fix-guard
plan: 02
subsystem: database
tags: [coaching-proton, deduplication, cleanup, linq, entity-framework]

# Dependency graph
requires:
  - phase: 159-01
    provides: root cause analysis and migration for assignment creation guard
provides:
  - SeedData.DeduplicateProtonTrackAssignments: one-time cleanup of existing duplicate active assignments
  - CDPController defensive GroupBy guard preventing duplicate rows in CoachingProton UI
  - AdminController.CleanupDuplicateAssignments manual trigger endpoint
affects: [CoachingProton, ProtonTrackAssignment, future phases touching CDPController coaching query]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - GroupBy (CoacheeId, ProtonTrackId) + OrderByDescending(Id).First() for dedup in LINQ queries
    - Idempotent startup cleanup via SeedData static method

key-files:
  created: []
  modified:
    - Data/SeedData.cs
    - Controllers/AdminController.cs
    - Controllers/CDPController.cs

key-decisions:
  - "Used highest Id (not AssignedAt) as tiebreaker inside GroupBy for deterministic EF Core translation"
  - "Cleanup runs automatically at startup via InitializeAsync — no separate migration step needed"

patterns-established:
  - "Defensive dedup guard: GroupBy coachee+track at query time, take max Id per group"
  - "Idempotent startup cleanup: check duplicates exist before doing any DB write"

requirements-completed: [CLN-01, DEF-01]

# Metrics
duration: 10min
completed: 2026-03-12
---

# Phase 159 Plan 02: Deduplication Fix & Guard Summary

**One-time startup cleanup deactivates duplicate active ProtonTrackAssignments and CoachingProton query guards against future duplicates via GroupBy(CoacheeId, ProtonTrackId)**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-03-12T00:00:00Z
- **Completed:** 2026-03-12T00:10:00Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- `SeedData.DeduplicateProtonTrackAssignments` deactivates all duplicate active assignments on startup, keeping the latest per coachee+track pair (idempotent)
- `AdminController.CleanupDuplicateAssignments` POST endpoint gives admin a manual on-demand trigger returning `{ cleaned: N }`
- CDPController CoachingProton query now groups active assignments by (CoacheeId, ProtonTrackId) and takes only the max-Id row, eliminating duplicate deliverable rows in the UI even if bad data slips through

## Task Commits

1. **Task 1: Data cleanup for duplicate active assignments** - `91c4738` (feat)
2. **Task 2: Defensive query guard in CoachingProton** - `cb438e6` (feat)

## Files Created/Modified
- `Data/SeedData.cs` - Added `DeduplicateProtonTrackAssignments` static method; called from `InitializeAsync`
- `Controllers/AdminController.cs` - Added `CleanupDuplicateAssignments` POST action (Admin only)
- `Controllers/CDPController.cs` - Replaced flat `.Select(a => a.Id)` with `.GroupBy(...).Select(g => g.OrderByDescending(a => a.Id).First().Id)` defensive guard

## Decisions Made
- Used `OrderByDescending(a => a.Id).First()` inside GroupBy rather than AssignedAt because EF Core translates this more reliably to SQL, and Id is monotonically increasing so it acts as a proxy for "latest created"
- Cleanup runs at app startup automatically — no manual migration needed; existing data fixed before any UI request lands

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Both CLN-01 and DEF-01 requirements satisfied
- Phase 159 (both plans) complete — milestone v4.1 Coaching Proton Deduplication ready for closure

---
*Phase: 159-deduplication-fix-guard*
*Completed: 2026-03-12*
