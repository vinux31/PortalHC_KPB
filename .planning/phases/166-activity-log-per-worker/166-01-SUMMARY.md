---
phase: 166-activity-log-per-worker
plan: "01"
subsystem: database
tags: [signalr, ef-core, sqlite, audit-log, exam-monitoring]

requires:
  - phase: 165-worker-to-hc-push
    provides: AssessmentHub SignalR infrastructure and IHubContext injection pattern

provides:
  - ExamActivityLog entity with SessionId FK, EventType, Detail, Timestamp
  - EF migration AddExamActivityLog (cascade delete, SessionId index)
  - AssessmentHub.LogPageNav hub method (fire-and-forget via IServiceScopeFactory)
  - AssessmentHub.OnConnectedAsync / OnDisconnectedAsync lifecycle overrides
  - CMPController.LogActivityAsync private helper (try-catch swallowing errors)
  - CMPController StartExam logs 'started', SubmitExam logs 'submitted' (both paths)
  - AdminController.GetActivityLog GET endpoint returning summary + chronological events JSON

affects:
  - 166-02 (frontend audit trail viewer will call GetActivityLog)

tech-stack:
  added: []
  patterns:
    - IServiceScopeFactory injected into Hub to resolve DbContext per Task.Run scope
    - Fire-and-forget pattern for activity logging with try-catch swallowing errors
    - Hub lifecycle overrides (OnConnectedAsync/OnDisconnectedAsync) for disconnect tracking

key-files:
  created:
    - Models/ExamActivityLog.cs
    - Migrations/20260313045808_AddExamActivityLog.cs
    - Migrations/20260313045808_AddExamActivityLog.Designer.cs
  modified:
    - Data/ApplicationDbContext.cs
    - Hubs/AssessmentHub.cs
    - Controllers/CMPController.cs
    - Controllers/AdminController.cs

key-decisions:
  - "IServiceScopeFactory injected into Hub (not DbContext directly) — Hub instances are transient, DbContext must be scoped per Task.Run to avoid threading issues"
  - "LogActivityAsync in CMPController reuses _context (request-scoped) — safe because it runs in the same request, not in Task.Run"
  - "OnConnectedAsync logs 'reconnected' only when user has InProgress session — avoids noise from HC/Admin hub connections"
  - "Duplicate migration (45835) removed — created by failed `dotnet ef migrations add` before --no-build flag was used"

patterns-established:
  - "Hub DB writes: always use IServiceScopeFactory + Task.Run, never direct DbContext"
  - "Activity logging: fire-and-forget, all errors swallowed, exam flow never blocked"

requirements-completed: [LOG-01, LOG-02]

duration: 20min
completed: 2026-03-13
---

# Phase 166 Plan 01: Activity Log — Model, Hub, Controllers Summary

**ExamActivityLog entity with EF migration, Hub disconnect/reconnect tracking via IServiceScopeFactory, and GetActivityLog JSON endpoint for HC audit trail**

## Performance

- **Duration:** 20 min
- **Started:** 2026-03-13T04:50:00Z
- **Completed:** 2026-03-13T05:10:00Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments

- Created ExamActivityLog entity (SessionId FK with cascade delete, EventType, Detail, Timestamp) and registered in DbContext with EF migration
- Updated AssessmentHub with IServiceScopeFactory DI, LogPageNav method, and OnConnectedAsync/OnDisconnectedAsync lifecycle overrides — all fire-and-forget with error swallowing
- Added LogActivityAsync helper to CMPController and wired it to StartExam (started) and SubmitExam (submitted, both package and legacy paths)
- Added GetActivityLog endpoint to AdminController returning JSON with summary (answered count, disconnect count, time spent) and chronological event list

## Task Commits

1. **Task 1: ExamActivityLog model, DbSet, Hub logging methods** - `a39ab9e` (feat)
2. **Task 2: Controller activity logging + GetActivityLog endpoint** - `5619d26` (feat)

## Files Created/Modified

- `Models/ExamActivityLog.cs` - Entity with SessionId FK, EventType, Detail, Timestamp
- `Data/ApplicationDbContext.cs` - Added DbSet<ExamActivityLog> + OnModelCreating configuration
- `Hubs/AssessmentHub.cs` - IServiceScopeFactory DI, LogPageNav, OnConnectedAsync, OnDisconnectedAsync
- `Controllers/CMPController.cs` - LogActivityAsync helper, StartExam/SubmitExam fire-and-forget calls
- `Controllers/AdminController.cs` - GetActivityLog endpoint (Admin, HC authorized)
- `Migrations/20260313045808_AddExamActivityLog.cs` - EF migration

## Decisions Made

- IServiceScopeFactory injected into Hub (not DbContext directly) — Hub instances are transient; DbContext must be resolved in a new scope per Task.Run to avoid concurrency issues
- LogActivityAsync in CMPController reuses `_context` safely (same HTTP request scope), not in background thread
- OnConnectedAsync logs "reconnected" only when user has an InProgress session — avoids noise from HC/Admin hub connections
- Duplicate migration file (20260313045835) removed — created by first `dotnet ef migrations add` attempt which failed to copy exe (app was running); second `--no-build` call created correct migration

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Removed duplicate migration file**
- **Found during:** Task 2 verification (dotnet build)
- **Issue:** Two migrations named AddExamActivityLog (timestamps 045808 and 045835) caused duplicate class errors
- **Fix:** Deleted the 045835 pair — identical content, only timestamp differed
- **Files modified:** Migrations/ directory
- **Verification:** `dotnet build --no-restore` returned no CS errors
- **Committed in:** 5619d26 (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (blocking)
**Impact on plan:** Necessary cleanup from interrupted migration command. No scope creep.

## Issues Encountered

- App process was running (pid 9760) which locked the exe and caused `dotnet ef migrations add` to fail on first attempt. Used `--no-build` on second attempt to bypass, which succeeded.

## User Setup Required

None — migration will be applied automatically on next app start (auto-migrate pattern in use).

## Next Phase Readiness

- All server-side data infrastructure for activity logging is in place
- Phase 166-02 can build the frontend audit trail viewer calling `Admin/GetActivityLog?sessionId=X`
- No blockers

---
*Phase: 166-activity-log-per-worker*
*Completed: 2026-03-13*
