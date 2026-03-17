---
phase: 191-wizard-ui
plan: 01
subsystem: database
tags: [ef-core, migration, assessment-session, model]

# Dependency graph
requires: []
provides:
  - "DateTime? ValidUntil nullable column on AssessmentSessions table"
  - "ValidUntil property on AssessmentSession model for wizard view binding"
  - "ModelState.Remove(ValidUntil) guard in AdminController POST"
affects: [191-02, 191-03, certificate-generation]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Nullable DateTime? property pattern for optional date fields (mirrors ExamWindowCloseDate)"
    - "ModelState.Remove guard for optional nullable properties before model validation"

key-files:
  created:
    - Migrations/20260317132516_AddValidUntilToAssessmentSession.cs
    - Migrations/20260317132516_AddValidUntilToAssessmentSession.Designer.cs
  modified:
    - Models/AssessmentSession.cs
    - Controllers/AdminController.cs
    - Migrations/ApplicationDbContextModelSnapshot.cs

key-decisions:
  - "ValidUntil is nullable (DateTime?) — null means no expiry, consistent with ExamWindowCloseDate pattern"
  - "ModelState.Remove added immediately after ExamWindowCloseDate guard to maintain grouping clarity"

patterns-established:
  - "Optional date fields use DateTime? + ModelState.Remove pattern — established by ExamWindowCloseDate, extended here"

requirements-completed: [FORM-01]

# Metrics
duration: 5min
completed: 2026-03-17
---

# Phase 191 Plan 01: ValidUntil Model Property Summary

**DateTime? ValidUntil added to AssessmentSession with EF migration applied and POST guard in AdminController**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-03-17T13:25:00Z
- **Completed:** 2026-03-17T13:30:00Z
- **Tasks:** 1
- **Files modified:** 3 (+ 2 migration files created)

## Accomplishments
- Added `public DateTime? ValidUntil { get; set; }` to AssessmentSession model after ExamWindowCloseDate
- Created and applied EF migration `AddValidUntilToAssessmentSession` (column `datetime2 NULL` in AssessmentSessions table)
- Added `ModelState.Remove("ValidUntil")` in AdminController POST action to guard the optional field

## Task Commits

Each task was committed atomically:

1. **Task 1: Add ValidUntil property and EF migration** - `f8f800b` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified
- `Models/AssessmentSession.cs` - Added DateTime? ValidUntil property with XML doc comment
- `Controllers/AdminController.cs` - Added ModelState.Remove("ValidUntil") after ExamWindowCloseDate guard
- `Migrations/20260317132516_AddValidUntilToAssessmentSession.cs` - EF migration adding nullable column
- `Migrations/20260317132516_AddValidUntilToAssessmentSession.Designer.cs` - Migration snapshot
- `Migrations/ApplicationDbContextModelSnapshot.cs` - Updated model snapshot

## Decisions Made
- ValidUntil is nullable (DateTime?) — null means no expiry, consistent with the ExamWindowCloseDate pattern already in the model
- Placed ValidUntil immediately after ExamWindowCloseDate for logical grouping

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- ValidUntil property is live in DB and model — Plan 02 (wizard view) can now use `asp-for="ValidUntil"` without compile errors
- No blockers

---
*Phase: 191-wizard-ui*
*Completed: 2026-03-17*
