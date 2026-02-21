---
phase: 29-auto-transition-upcoming-to-open
plan: 01
subsystem: api
tags: [assessment, status, auto-transition, datetime, csharp, efcore]

# Dependency graph
requires:
  - phase: 27-monitoring-status-fix
    provides: GetMonitorData AJAX endpoint and MonitoringGroupViewModel groupStatus logic
  - phase: 22-exam-lifecycle-actions
    provides: StartExam action with InProgress transition and SaveChangesAsync pattern
provides:
  - Upcoming→Open auto-transition applied at all three read locations in CMPController.cs
  - Display-only transition in GetMonitorData (no DB write)
  - Display-only transition in worker assessment list (no DB write)
  - Persisted transition in StartExam (SaveChangesAsync before status checks)
affects: [30-any-reporting, 31-forceclose-all, StartExam, GetMonitorData, Assessment worker list]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Read-time status override: re-project anonymous list after ToListAsync with conditional Status field"
    - "Persisted transition before status checks: write DB then let existing guards run against corrected state"
    - "todayUtc = DateTime.UtcNow.Date for date-level (not time-level) comparison"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs

key-decisions:
  - "Display-only in GetMonitorData via anonymous-type re-projection — no SaveChangesAsync, avoids audit log spam"
  - "Persisted in StartExam via SaveChangesAsync before Completed/Abandoned status checks — first worker access fixes DB state"
  - "Display-only in worker list via foreach over EF-tracked entities — discarded at request end, count already computed from DB before transition"
  - "Schedule.Date <= DateTime.UtcNow.Date comparison at date granularity — all sessions on scheduled day open at midnight UTC"

patterns-established:
  - "Auto-transition pattern: re-project after ToListAsync with ternary Status override for display-only endpoints"
  - "Persist-first pattern: SaveChangesAsync inside transition block, then let existing status checks run on corrected value"

# Metrics
duration: 2min
completed: 2026-02-21
---

# Phase 29 Plan 01: Auto-transition Upcoming to Open Summary

**Query-time Upcoming→Open status transition at three CMPController call sites: display-only in GetMonitorData + worker list, persisted in StartExam using Schedule.Date <= DateTime.UtcNow.Date**

## Performance

- **Duration:** 2 min
- **Started:** 2026-02-21T13:45:56Z
- **Completed:** 2026-02-21T13:47:45Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments
- GetMonitorData now re-projects the anonymous-type list after ToListAsync, overriding Status to "Open" for any session with Status=="Upcoming" and Schedule.Date <= today — HC dashboard shows correct Open status on every AJAX poll without any DB writes
- Worker assessment list applies a foreach loop after ToListAsync to override Upcoming status in-memory — workers see Open badge and can click Start for assessments whose scheduled date has arrived
- StartExam persists the Upcoming→Open transition to DB (with SaveChangesAsync) before the Completed/Abandoned/token checks — after first worker access, the DB row has Status="Open" so all sibling readers (GetMonitorData, other workers) see the correct state from DB

## Task Commits

Each task was committed atomically:

1. **Task 1: Apply display-only auto-transition in GetMonitorData** - `7817e2f` (feat)
2. **Task 2: Apply persisted auto-transition in StartExam + display-only in worker list** - `959663d` (feat)

**Plan metadata:** `(pending docs commit)` (docs: complete plan)

## Files Created/Modified
- `Controllers/CMPController.cs` - Three transition sites added: re-projection in GetMonitorData, foreach in worker list, SaveChangesAsync block in StartExam

## Decisions Made
- Display-only in GetMonitorData (no DB write): avoids audit log spam and keeps UpdatedAt meaningful as HC-action timestamp
- Persisted in StartExam only: single-session save, no sibling cascade — sibling display handled by GetMonitorData display-only transition
- Date-level comparison (`Schedule.Date <= DateTime.UtcNow.Date`): sessions open at midnight UTC on scheduled day, not at exact scheduled time
- Re-projection pattern for anonymous-type list: cannot mutate anonymous properties directly, so `.Select(a => new { ... Status = ... })` creates a new list

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Auto-transition fully implemented across all three read locations in CMPController.cs
- Workers will see Open status as soon as scheduled date arrives on next page load
- HC dashboard will reflect Open status on next GetMonitorData AJAX poll
- DB is corrected on first worker StartExam access — subsequent reads see Open from DB
- No blockers for remaining v1.8 phases (Phase 30 reporting, Phase 31 ForceCloseAll)

---
*Phase: 29-auto-transition-upcoming-to-open*
*Completed: 2026-02-21*

## Self-Check: PASSED

- Controllers/CMPController.cs: FOUND
- 29-01-SUMMARY.md: FOUND
- Commit 7817e2f: FOUND
- Commit 959663d: FOUND
