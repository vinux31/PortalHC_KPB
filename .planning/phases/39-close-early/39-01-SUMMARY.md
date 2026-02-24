---
phase: 39-close-early
plan: 01
subsystem: api
tags: [assessment, scoring, package-mode, legacy-mode, audit-log, competency, close-early]

# Dependency graph
requires:
  - phase: 38-auto-hide-filter
    provides: ExamWindowCloseDate enforcement pattern (StartExam checks it to block exam access)
  - phase: 33-protontrack-schema
    provides: UserCompetencyLevel / AssessmentCompetencyMap models used in competency update block
provides:
  - CloseEarly POST action in CMPController scoring InProgress sessions from actual submitted answers
  - Bulk ExamWindowCloseDate lock on all sessions in a group
  - Competency level auto-update for passed InProgress sessions (parity with SubmitExam)
  - AuditLog entry with ActionType='CloseEarly'
affects: [39-02-frontend, phase-40]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Bulk preload to avoid N+1: load all UserPackageAssignments + packages before the per-session loop"
    - "InProgress detection: StartedAt!=null && CompletedAt==null && Score==null (4-state timestamp logic)"
    - "Package scoring: PackageUserResponses loaded as dictionary per session; maxScore=Sum(ScoreValue)"
    - "Single SaveChangesAsync at end for atomicity across all session/competency mutations"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs

key-decisions:
  - "CloseEarly inserted after ForceCloseAll (~line 783) before RESHUFFLE PACKAGE block"
  - "isInProgress check uses timestamps (StartedAt!=null && CompletedAt==null && Score==null), not Status field — 4-state display logic source of truth"
  - "maxScore uses pkg.Questions.Sum(q => q.ScoreValue) not Count*10 — safe against non-standard ScoreValue (Pitfall 6)"
  - "Competency update block included for both package and legacy paths when IsPassed==true — parity with SubmitExam lines 2878-2921"
  - "CloseEarly reads PackageUserResponses (does NOT write new ones) — SubmitExam writes them, CloseEarly reads existing"
  - "Single SaveChangesAsync call at end covers all session fields + UserPackageAssignment.IsCompleted + UserCompetencyLevels"

patterns-established:
  - "CloseEarly pattern: ExamWindowCloseDate lock on ALL sessions, score only InProgress ones"
  - "Bulk preload before loop: sessionAssignmentMap + packageMap dictionaries populated before foreach"

# Metrics
duration: 5min
completed: 2026-02-24
---

# Phase 39 Plan 01: CloseEarly Backend Summary

**CloseEarly POST action in CMPController that locks an entire assessment group via ExamWindowCloseDate and scores InProgress sessions from their actual submitted PackageUserResponse/UserResponse answers using the same grading logic as SubmitExam**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-02-24T09:30:01Z
- **Completed:** 2026-02-24T09:35:00Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- Added CloseEarly POST action (246 lines) in CMPController.cs after ForceCloseAll at line 783
- Implements fair scoring: InProgress sessions receive a score calculated from their actual submitted answers (not Score=0 like ForceClose)
- Handles both package mode and legacy mode assessments; detects mode once for the group then routes per-session accordingly
- Competency levels auto-updated for passed InProgress sessions (same block as SubmitExam lines 2878-2921)
- Single SaveChangesAsync for atomicity; AuditLog entry with ActionType='CloseEarly' recording counts
- dotnet build: 0 errors, 36 pre-existing warnings (none from new code)

## Task Commits

Each task was committed atomically:

1. **Task 1: Add CloseEarly POST action to CMPController after ForceCloseAll (~line 783)** - `06084f8` (feat)

**Plan metadata:** (docs commit below)

## Files Created/Modified
- `Controllers/CMPController.cs` - CloseEarly POST action inserted after ForceCloseAll closing brace (line 783), 246 lines added

## Decisions Made
- `isInProgress` check uses `StartedAt != null && CompletedAt == null && Score == null` (not `Status == "InProgress"`) — consistent with 4-state display logic timestamps as source of truth
- `maxScore` uses `pkg.Questions.Sum(q => q.ScoreValue)` not `Count * 10` — safe against questions with non-standard ScoreValue
- Competency update block runs for both package and legacy InProgress sessions where IsPassed==true — ensures parity with SubmitExam
- CloseEarly reads PackageUserResponses (never adds them) — SubmitExam creates PUR records on submit, CloseEarly uses whatever was already persisted
- Bulk preload pattern (sessionAssignmentMap + packageMap) used before the per-session loop to avoid N+1 queries

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- CloseEarly backend action is complete and compiles cleanly
- Ready for Phase 39-02: frontend button + confirmation modal in AssessmentMonitoringDetail.cshtml
- Phase 39-02 is a human-verify checkpoint plan (Wave 2)

## Self-Check: PASSED

- Controllers/CMPController.cs: FOUND
- 39-01-SUMMARY.md: FOUND
- Commit 06084f8: FOUND

---
*Phase: 39-close-early*
*Completed: 2026-02-24*
