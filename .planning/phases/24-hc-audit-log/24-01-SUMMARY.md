---
phase: 24-hc-audit-log
plan: 01
subsystem: database
tags: [audit-log, ef-migration, sql-server, csharp, asp-net]

# Dependency graph
requires:
  - phase: 23-package-answer-integrity
    provides: PackageUserResponse table and CMPController reset/force-close actions that needed audit wiring

provides:
  - AuditLogs table in SQL Server with CreatedAt/ActorUserId/ActionType indexes
  - AuditLogService scoped DI service with LogAsync helper
  - 7 audit log call sites in CMPController covering all HC assessment management actions

affects:
  - 24-02 (phase 2 of 24 — will add audit log viewer UI that reads from AuditLogs table)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "AuditLogService pattern: scoped service injected into controller, calls SaveChangesAsync internally"
    - "Actor name captured at write time as NIP + FullName string to survive user deletion"
    - "Audit calls placed after primary SaveChangesAsync — no phantom rows on failed operations"
    - "Delete actions wrap audit call in try/catch with LogWarning to avoid breaking the primary operation"

key-files:
  created:
    - Models/AuditLog.cs
    - Services/AuditLogService.cs
    - Migrations/20260221032754_AddAuditLog.cs
    - Migrations/20260221032754_AddAuditLog.Designer.cs
  modified:
    - Data/ApplicationDbContext.cs
    - Program.cs
    - Controllers/CMPController.cs

key-decisions:
  - "AuditLogService calls SaveChangesAsync internally — audit rows are written immediately after each action"
  - "Actor name stored as 'NIP - FullName' string at write time so log remains readable if user is later deleted"
  - "Audit calls placed AFTER primary SaveChangesAsync — failed operations produce no audit rows"
  - "Delete actions (DeleteAssessment, DeleteAssessmentGroup) wrap audit call in try/catch with LogWarning — audit failure must not roll back a successful delete"
  - "editUser fetched before the EditAssessment try block so it is in scope for both edit and bulk-assign audit calls"

patterns-established:
  - "Audit-after-success: _auditLog.LogAsync always called after await _context.SaveChangesAsync() / transaction.CommitAsync()"
  - "Destructive-action audit guard: wrap _auditLog.LogAsync in try/catch(Exception auditEx) { logger.LogWarning } for delete paths"

# Metrics
duration: 8min
completed: 2026-02-21
---

# Phase 24 Plan 01: HC Audit Log Infrastructure Summary

**AuditLog entity, EF migration, scoped AuditLogService, and 7 instrumented audit call sites in CMPController covering CreateAssessment, EditAssessment, BulkAssign, DeleteAssessment, DeleteAssessmentGroup, ForceCloseAssessment, and ResetAssessment**

## Performance

- **Duration:** 8 min
- **Started:** 2026-02-21T03:22:00Z
- **Completed:** 2026-02-21T03:30:52Z
- **Tasks:** 2
- **Files modified:** 7

## Accomplishments

- Created AuditLog entity with 8 fields (Id, ActorUserId, ActorName, ActionType, Description, TargetId, TargetType, CreatedAt) and applied EF migration to SQL Server
- Created Services/AuditLogService.cs as a scoped DI service with LogAsync; registered in Program.cs
- Instrumented all 7 HC assessment management actions in CMPController with audit log calls that fire only after successful primary operations

## Task Commits

Each task was committed atomically:

1. **Task 1: Create AuditLog entity, DbContext registration, EF migration, and AuditLogService** - `22d5b8e` (feat)
2. **Task 2: Instrument all HC assessment management actions with audit log calls** - `fe5f4ec` (feat)

## Files Created/Modified

- `Models/AuditLog.cs` - AuditLog entity with 8 fields and data annotations
- `Services/AuditLogService.cs` - Scoped DI service with LogAsync that calls SaveChangesAsync internally
- `Data/ApplicationDbContext.cs` - DbSet<AuditLog> AuditLogs registered; OnModelCreating config with 3 indexes + CreatedAt default
- `Program.cs` - builder.Services.AddScoped<AuditLogService>() registration
- `Controllers/CMPController.cs` - AuditLogService injection + 7 audit call sites
- `Migrations/20260221032754_AddAuditLog.cs` - EF migration creating AuditLogs table
- `Migrations/20260221032754_AddAuditLog.Designer.cs` - Migration designer file

## Decisions Made

- AuditLogService calls SaveChangesAsync internally — audit rows are written immediately after each action, keeping the service simple and self-contained
- Actor name stored as "NIP - FullName" string at write time so the log remains readable if the user is later deleted
- Audit calls placed AFTER primary SaveChangesAsync — failed operations produce no audit rows (correct behavior)
- Delete actions (DeleteAssessment, DeleteAssessmentGroup) wrap audit call in try/catch with LogWarning — audit failure must not roll back a successful delete operation
- editUser fetched before the EditAssessment try block so it is in scope for both the edit and bulk-assign audit calls

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- AuditLogs table populated on every HC assessment management action
- Phase 24-02 (audit log viewer) can now query AuditLogs table — all required data (actor, action type, description, timestamp) is present
- No blockers

## Self-Check

- [x] `Models/AuditLog.cs` exists
- [x] `Services/AuditLogService.cs` exists
- [x] `Migrations/20260221032754_AddAuditLog.cs` exists
- [x] `Data/ApplicationDbContext.cs` contains `DbSet<AuditLog> AuditLogs`
- [x] `Program.cs` contains `AddScoped<HcPortal.Services.AuditLogService>`
- [x] `CMPController.cs` contains `_auditLog.LogAsync` 7 times
- [x] Build passes with 0 errors
- [x] Database migration applied (AuditLogs table created with 3 indexes)

## Self-Check: PASSED

---
*Phase: 24-hc-audit-log*
*Completed: 2026-02-21*
