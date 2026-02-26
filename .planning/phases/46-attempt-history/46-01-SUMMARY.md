---
phase: 46-attempt-history
plan: 01
subsystem: database
tags: [efcore, migration, sqlserver, csharp, aspnet]

# Dependency graph
requires:
  - phase: 24-audit-log
    provides: AuditLog pattern for entity configuration in OnModelCreating
  - phase: 17-test-packages
    provides: AssessmentSession model with Status, Score, IsPassed, StartedAt, CompletedAt fields
provides:
  - AssessmentAttemptHistory table in SQL Server database
  - DbSet<AssessmentAttemptHistory> registered in ApplicationDbContext
  - Archival logic in CMPController.ResetAssessment for Completed sessions
  - AttemptNumber sequential counter per (UserId, Title) pair
affects:
  - 46-02 (History tab will query AssessmentAttemptHistory)
  - AllWorkersHistoryRow (AttemptNumber column added in Plan 02)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Archive-before-clear: capture session state into history table before wiping fields, sharing one SaveChangesAsync"
    - "AttemptNumber computed as count of existing rows + 1 (no sequence column needed)"

key-files:
  created:
    - Models/AssessmentAttemptHistory.cs
    - Migrations/20260226012858_AddAssessmentAttemptHistory.cs
    - Migrations/20260226012858_AddAssessmentAttemptHistory.Designer.cs
  modified:
    - Data/ApplicationDbContext.cs
    - Controllers/CMPController.cs
    - Migrations/ApplicationDbContextModelSnapshot.cs

key-decisions:
  - "Archival block inserted BEFORE UserResponse deletion so session data (Score, IsPassed, etc.) is still intact"
  - "Archive and session reset share one SaveChangesAsync — no separate transaction needed"
  - "DeleteBehavior.Cascade on UserId FK so history rows clean up if user is deleted"
  - "Debug build fails due to running process lock; used --configuration Release for migration commands"

patterns-established:
  - "Attempt archive pattern: count existing rows for same (UserId, Title), use count+1 as AttemptNumber"

requirements-completed: [HIST-01]

# Metrics
duration: 3min
completed: 2026-02-26
---

# Phase 46 Plan 01: Attempt History Summary

**AssessmentAttemptHistory EF Core model + SQL Server table + archival logic that saves Score/IsPassed/AttemptNumber when a Completed session is reset**

## Performance

- **Duration:** 3 min
- **Started:** 2026-02-26T01:27:25Z
- **Completed:** 2026-02-26T01:31:00Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments
- Created AssessmentAttemptHistory model with all required fields (SessionId, UserId, Title, Category, Score, IsPassed, StartedAt, CompletedAt, AttemptNumber, ArchivedAt, CreatedAt)
- Registered DbSet and configured EF entity (FK cascade, composite index on UserId+Title, GETUTCDATE default)
- Applied migration 20260226012858_AddAssessmentAttemptHistory — table live in SQL Server
- Inserted archival block in CMPController.ResetAssessment: archives Completed sessions only, computes AttemptNumber as existing count+1, shares one SaveChangesAsync with session reset

## Task Commits

Each task was committed atomically:

1. **Task 1: AssessmentAttemptHistory model + DbContext registration + migration** - `86b1896` (feat)
2. **Task 2: Archival logic in ResetAssessment** - `432be91` (feat)

## Files Created/Modified
- `Models/AssessmentAttemptHistory.cs` - Archive record model with all required fields
- `Data/ApplicationDbContext.cs` - DbSet<AssessmentAttemptHistory> + OnModelCreating entity config
- `Migrations/20260226012858_AddAssessmentAttemptHistory.cs` - CreateTable migration
- `Migrations/20260226012858_AddAssessmentAttemptHistory.Designer.cs` - Migration designer file
- `Migrations/ApplicationDbContextModelSnapshot.cs` - Updated model snapshot
- `Controllers/CMPController.cs` - Archival block inserted in ResetAssessment before UserResponse deletion

## Decisions Made
- Used `--configuration Release` for dotnet ef commands because the Debug exe is locked by the running HcPortal process; Release uses a separate output path and succeeds
- Archival block placed before UserResponse deletion so session field values (Score, IsPassed, etc.) are still available for the archive row
- Both archive Add() and session field resets share one existing SaveChangesAsync at line 649 — no separate call needed per plan specification

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Debug build fails because HcPortal process (PID 8956) is running and locking HcPortal.exe — this is an environment condition, not a code error. All ef migration commands used `--configuration Release` to work around it. Release build confirms 0 errors.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- AssessmentAttemptHistory table is live in the database with correct schema
- Archival logic is active — any Completed session reset will now produce a history row
- Ready for Plan 46-02: History tab UI that queries and displays AssessmentAttemptHistory rows

---
*Phase: 46-attempt-history*
*Completed: 2026-02-26*
