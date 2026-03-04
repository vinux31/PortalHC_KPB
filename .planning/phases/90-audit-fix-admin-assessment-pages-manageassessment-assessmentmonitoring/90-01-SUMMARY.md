---
phase: 90-audit-fix-admin-assessment-pages-manageassessment-assessmentmonitoring
plan: 01
subsystem: assessment
tags: [assessment, soft-delete, cascade-delete, token, AdminController]

# Dependency graph
requires:
  - phase: 83-master-data-qa
    provides: IsActive field on ApplicationUser (soft-delete foundation)
provides:
  - IsActive-filtered user queries in CreateAssessment GET, all POST reload paths, EditAssessment GET, and GetWorkersInSection
  - RegenerateToken updates all sibling sessions sharing same Title+Category+Schedule.Date
  - DeleteAssessment and DeleteAssessmentGroup correctly cascade PackageUserResponses and AssessmentAttemptHistory
affects: [91-cmp-assessment-audit]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "IsActive filter: always prepend .Where(u => u.IsActive) before .OrderBy/.Include on _context.Users in assessment actions"
    - "Group token sync: RegenerateToken queries siblings by Title+Category+Schedule.Date and applies same token to all"
    - "Cascade delete order: PackageUserResponses (Restrict FK) and AssessmentAttemptHistory (no FK) must be manually removed before deleting AssessmentSession"

key-files:
  created: []
  modified:
    - Controllers/AdminController.cs

key-decisions:
  - "[90-01] IsActive filter added to all 5 user query locations in assessment section (CreateAssessment GET + 3 POST reload paths + EditAssessment GET + GetWorkersInSection helper)"
  - "[90-01] RegenerateToken now syncs all siblings in same group — single new token applied to all sessions with matching Title+Category+Schedule.Date"
  - "[90-01] DeleteAssessment/DeleteAssessmentGroup: PackageUserResponses manually deleted (Restrict FK blocks auto cascade); AssessmentAttemptHistory manually deleted (SessionId is plain int, no FK — would orphan)"
  - "[90-01] UserPackageAssignments: already cascade-deleted by DB (Cascade FK configured in ApplicationDbContext) — no manual deletion needed"
  - "[90-01] ForceCloseAssessment: intentionally does NOT archive to AssessmentAttemptHistory — force-close is admin override, not a real completion"

patterns-established:
  - "Assessment user pickers: always filter IsActive — deactivated workers must not appear in assignment pickers"
  - "Token regeneration: token belongs to the group, not individual session — always sync all siblings"

requirements-completed: []

# Metrics
duration: 20min
completed: 2026-03-04
---

# Phase 90 Plan 01: Audit & Fix Admin Assessment Pages Summary

**IsActive filter on 5 user query locations, group-wide RegenerateToken sync, and cascade-delete gaps fixed for PackageUserResponses and AssessmentAttemptHistory in AdminController**

## Performance

- **Duration:** 20 min
- **Started:** 2026-03-04T00:00:00Z
- **Completed:** 2026-03-04T00:20:00Z
- **Tasks:** 3
- **Files modified:** 1

## Accomplishments
- Applied `.Where(u => u.IsActive)` to all 5 user query locations in assessment-related actions — deactivated workers are now excluded from CreateAssessment, EditAssessment pickers and the Training tab
- Fixed `RegenerateToken` to update all sibling sessions sharing the same Title+Category+Schedule.Date — previously only the representative session's token was updated, leaving other workers with stale tokens
- Fixed `DeleteAssessment` cascade: now manually deletes `PackageUserResponses` (Restrict FK — would have thrown FK violation) and `AssessmentAttemptHistory` rows (plain int SessionId — would have orphaned records)
- Fixed `DeleteAssessmentGroup` with same cascade fix applied across all siblings in batch
- Added `90-review` comments to `AssessmentMonitoring` (7-day window intent), `AssessmentMonitoringDetail` (GroupTahunKe null-safety), and `ForceCloseAssessment` (no-archive design)

## Task Commits

Each task was committed atomically:

1. **Task 1: Apply IsActive filter to all user queries in assessment actions** - `a44617b` (fix)
2. **Task 2: Fix RegenerateToken to update all sibling sessions** - `6f6f8c2` (fix)
3. **Task 3: Code review remaining assessment actions for edge cases** - `7b1be2c` (fix)

## Files Created/Modified
- `Controllers/AdminController.cs` - 5 IsActive filter additions, RegenerateToken sibling sync, cascade fixes in DeleteAssessment and DeleteAssessmentGroup, review comments on 3 actions

## Decisions Made
- `UserPackageAssignments` do not need manual deletion — the DB is configured with `OnDelete(DeleteBehavior.Cascade)` from `AssessmentSession`, so EF Core handles this automatically
- `PackageUserResponses` have `OnDelete(DeleteBehavior.Restrict)` — must be manually removed before deleting the session to avoid FK violation
- `AssessmentAttemptHistory.SessionId` is a plain int (not a FK navigation property in EF) — must be manually removed to avoid orphaned rows
- ForceCloseAssessment's no-archive design is acceptable: it is an admin override, not a real attempt completion

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed missing PackageUserResponses deletion in DeleteAssessment**
- **Found during:** Task 3 (code review of DeleteAssessment)
- **Issue:** `PackageUserResponses` has a Restrict FK to `AssessmentSession` — deleting a session without first removing its `PackageUserResponses` would throw an FK constraint violation at runtime
- **Fix:** Added manual deletion of `PackageUserResponses` before removing the session, in both `DeleteAssessment` and `DeleteAssessmentGroup`
- **Files modified:** Controllers/AdminController.cs
- **Verification:** Build passes with no CS errors
- **Committed in:** `7b1be2c` (Task 3 commit)

**2. [Rule 1 - Bug] Fixed orphaned AssessmentAttemptHistory rows in DeleteAssessment**
- **Found during:** Task 3 (code review of DeleteAssessment)
- **Issue:** `AssessmentAttemptHistory.SessionId` is a plain int field (no FK), so deleting a session leaves orphaned history rows that reference a non-existent session ID
- **Fix:** Added manual deletion of `AssessmentAttemptHistory` rows by SessionId before removing the session, in both `DeleteAssessment` and `DeleteAssessmentGroup`
- **Files modified:** Controllers/AdminController.cs
- **Verification:** Build passes with no CS errors
- **Committed in:** `7b1be2c` (Task 3 commit)

---

**Total deviations:** 2 auto-fixed (both Rule 1 - Bug, found during Task 3 code review)
**Impact on plan:** Both fixes essential for correctness — cascade gap in Delete actions was a latent FK violation bug. Plan's review checklist correctly identified both gaps.

## Issues Encountered
- App process was running during build, causing file-lock MSB3021 copy errors on the exe. Build compilation succeeded with 0 CS errors — only the exe-copy step failed due to the locked file. Not a code issue.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- AdminController assessment actions are now clean: IsActive-filtered pickers, correct group token sync, correct cascade deletes
- Phase 91 (CMP Assessment audit) can reference these same patterns for CMPController

---
*Phase: 90-audit-fix-admin-assessment-pages-manageassessment-assessmentmonitoring*
*Completed: 2026-03-04*
