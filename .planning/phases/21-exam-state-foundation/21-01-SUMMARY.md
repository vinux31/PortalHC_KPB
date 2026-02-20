---
phase: 21-exam-state-foundation
plan: 01
subsystem: assessment
tags: [ef-core, migration, assessment-session, monitoring, csharp, razor]

# Dependency graph
requires: []
provides:
  - StartedAt nullable DateTime column on AssessmentSessions table (EF migration)
  - InProgress status string for AssessmentSession lifecycle
  - Idempotent InProgress state write in StartExam GET (guard on StartedAt == null)
  - Three-state UserStatus derivation in AssessmentMonitoringDetail (Not started / InProgress / Completed)
  - InProgress badge (bg-warning) in AssessmentMonitoringDetail view
affects:
  - 22-exam-lifecycle-actions
  - 23-token-enforcement
  - 24-audit-log

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Idempotent state write using null timestamp guard (StartedAt == null) rather than status string comparison"
    - "Three-state UserStatus derivation: CompletedAt/Score -> Completed, StartedAt -> InProgress, else Not started"

key-files:
  created:
    - Migrations/20260220124827_AddExamStateFields.cs
    - Migrations/20260220124827_AddExamStateFields.Designer.cs
  modified:
    - Models/AssessmentSession.cs
    - Models/AssessmentMonitoringViewModel.cs
    - Controllers/CMPController.cs
    - Views/CMP/AssessmentMonitoringDetail.cshtml

key-decisions:
  - "Idempotency guard uses StartedAt == null, not Status != InProgress — timestamp write is authoritative, status is derived"
  - "InProgress sessions count as Open for GroupStatus derivation — HC sees group as Open while any worker is actively examining"
  - "GetMonitorData query filter extended to include Status == InProgress so active sessions appear in monitoring tab"

patterns-established:
  - "Status strings are plain, schema-unconstrained values — new states (InProgress, Abandoned) are valid without DB enum changes"

# Metrics
duration: 2min
completed: 2026-02-20
---

# Phase 21 Plan 01: Exam State Foundation Summary

**Added StartedAt datetime2 NULL column to AssessmentSessions via EF migration and wired idempotent InProgress state on first exam load, with three-state badge rendering in the monitoring detail view**

## Performance

- **Duration:** ~2 min
- **Started:** 2026-02-20T12:48:04Z
- **Completed:** 2026-02-20T12:50:08Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- EF migration `AddExamStateFields` adds `StartedAt datetime2 NULL` to `AssessmentSessions` table — applied to DB
- StartExam GET writes `Status = "InProgress"` and `StartedAt = DateTime.UtcNow` on first load, idempotent via `StartedAt == null` guard
- AssessmentMonitoringDetail controller projection updated to derive three-state `UserStatus` from `CompletedAt`/`Score`/`StartedAt`
- AssessmentMonitoringDetail view renders three distinct badges: bg-success (Completed), bg-warning (InProgress), bg-light (Not started)
- GetMonitorData lazy-load query and GroupStatus logic extended to include InProgress sessions

## Task Commits

Each task was committed atomically:

1. **Task 1: Add StartedAt column — model property + EF migration** - `93ceb50` (feat)
2. **Task 2: Wire InProgress state — StartExam GET + monitoring detail controller + view** - `5430b31` (feat)

**Plan metadata:** _(docs commit follows)_

## Files Created/Modified
- `Models/AssessmentSession.cs` - Added `public DateTime? StartedAt { get; set; }` after `CompletedAt`
- `Migrations/20260220124827_AddExamStateFields.cs` - EF migration adding `StartedAt datetime2 NULL` to `AssessmentSessions`
- `Migrations/20260220124827_AddExamStateFields.Designer.cs` - Migration designer snapshot
- `Controllers/CMPController.cs` - StartExam GET: idempotent InProgress write; AssessmentMonitoringDetail: three-state logic + GroupStatus; GetMonitorData: InProgress in query + hasOpen
- `Models/AssessmentMonitoringViewModel.cs` - Added `public DateTime? StartedAt { get; set; }` to `MonitoringSessionViewModel`
- `Views/CMP/AssessmentMonitoringDetail.cshtml` - Replaced two-way ternary badge with three-way switch expression

## Decisions Made
- Idempotency guard uses `assessment.StartedAt == null` rather than `assessment.Status != "InProgress"` — the timestamp is the authoritative first-write signal; if Status were updated by a race condition, StartedAt would still only be set once.
- InProgress sessions are treated as "Open" for `GroupStatus` — from HC's perspective, a group is still "Open" as long as anyone is actively examining.
- `GetMonitorData` query filter extended to include `Status == "InProgress"` — without this, InProgress sessions would vanish from the monitoring tab.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Extended GetMonitorData query filter to include InProgress**
- **Found during:** Task 2 (Wire InProgress state)
- **Issue:** Plan specified updating AssessmentMonitoringDetail and GroupStatus, but `GetMonitorData` (the lazy-load endpoint for the monitoring tab card list) had a `.Where()` filter that only included `Status == "Open"`, `"Upcoming"`, and recent `"Completed"`. InProgress sessions would be silently excluded from the monitoring tab.
- **Fix:** Added `|| a.Status == "InProgress"` to the query filter and updated the `hasOpen` boolean in GetMonitorData's GroupStatus to also check `a.Status == "InProgress"`.
- **Files modified:** `Controllers/CMPController.cs`
- **Verification:** `dotnet build` 0 errors; logic matches AssessmentMonitoringDetail behaviour.
- **Committed in:** `5430b31` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 2 — missing critical)
**Impact on plan:** Required for correct monitoring tab behaviour — InProgress sessions would be invisible in the monitoring card list without this fix. No scope creep.

## Issues Encountered
None — migration generated cleanly, build succeeded 0 errors.

## User Setup Required
None — no external service configuration required. Migration applied automatically via `dotnet ef database update`.

## Next Phase Readiness
- LIFE-01 satisfied: StartedAt recorded, InProgress visible in monitoring detail
- Phase 22 (Exam Lifecycle Actions: abandon, force-close, server-side timer) can now reference `StartedAt` and `Status = "InProgress"` as inputs
- Phase 23 (token enforcement in StartExam GET) inserts before the InProgress write already added here

---
*Phase: 21-exam-state-foundation*
*Completed: 2026-02-20*

## Self-Check: PASSED

| Item | Status |
|------|--------|
| Models/AssessmentSession.cs | FOUND |
| Models/AssessmentMonitoringViewModel.cs | FOUND |
| Controllers/CMPController.cs | FOUND |
| Views/CMP/AssessmentMonitoringDetail.cshtml | FOUND |
| Migrations/20260220124827_AddExamStateFields.cs | FOUND |
| .planning/phases/21-exam-state-foundation/21-01-SUMMARY.md | FOUND |
| Commit 93ceb50 | FOUND |
| Commit 5430b31 | FOUND |
