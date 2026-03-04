---
phase: 91-audit-fix-cmp-assessment-pages
plan: 01
subsystem: api
tags: [csharp, dotnet, cmp, assessment, csrf, auth, shuffle]

# Dependency graph
requires:
  - phase: 90-audit-fix-admin-assessment-pages-manageassessment-assessmentmonitoring
    provides: Admin assessment fixes; context for CMP assessment audit
provides:
  - ValidateAntiForgeryToken on all 9 CMPController POST actions
  - SubmitExam HC auth fix (HC can submit on behalf of workers)
  - Single-package question shuffle (each worker sees unique order)
  - Option shuffle populated per worker per question (not "{}")
  - UnifiedTrainingRecord.AssessmentSessionId for Records Results link
affects: [91-02-view-fixes, Records.cshtml, StartExam.cshtml, Assessment.cshtml]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Fisher-Yates shuffle applied to both question order and option order at session creation"
    - "optionShuffleDict built from packages.SelectMany(p => p.Questions) at StartExam"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Models/UnifiedTrainingRecord.cs

key-decisions:
  - "[91-01]: VerifyToken [ValidateAntiForgeryToken] added — plan 91-02 must add CSRF token to JS fetch call"
  - "[91-01]: SubmitExam auth now includes HC role — consistent with all other assessment actions"
  - "[91-01]: Single-package shuffle enabled — reversal of prior 'no shuffle' decision in BuildCrossPackageAssignment"
  - "[91-01]: Option shuffle stored as JSON dict in ShuffledOptionIdsPerQuestion — view rendering deferred to 91-02"
  - "[91-01]: Variable renamed questionsForOptionShuffle (was allPackageQuestions — CS0136 scope conflict)"

patterns-established:
  - "All CMPController [HttpPost] actions have [ValidateAntiForgeryToken] — maintain this invariant"

requirements-completed: []

# Metrics
duration: 12min
completed: 2026-03-04
---

# Phase 91 Plan 01: CMP Assessment Backend Security and Shuffle Fixes Summary

**CMPController hardened: CSRF on all 9 POSTs, HC auth on SubmitExam, Fisher-Yates shuffle for both question order and option order per worker, AssessmentSessionId added to UnifiedTrainingRecord**

## Performance

- **Duration:** 12 min
- **Started:** 2026-03-04T00:40:00Z
- **Completed:** 2026-03-04T00:52:00Z
- **Tasks:** 3
- **Files modified:** 2

## Accomplishments
- Added `[ValidateAntiForgeryToken]` to VerifyToken POST (was the only POST missing it); all 9 CMPController POST actions are now protected
- Fixed SubmitExam authorization to include `!User.IsInRole("HC")` — HC users were incorrectly forbidden from submitting on behalf of workers
- Fixed BuildCrossPackageAssignment single-package path to call `Shuffle()` before returning — each worker now gets a unique question sequence
- Populated `ShuffledOptionIdsPerQuestion` with a per-question randomized option ID list (was hardcoded `"{}"`) — A/B/C/D order now varies per worker
- Added `public int? AssessmentSessionId { get; set; }` to `UnifiedTrainingRecord` and mapped `AssessmentSessionId = a.Id` in `GetUnifiedRecords` — enables Records.cshtml to build Results link

## Task Commits

Each task was committed atomically:

1. **Task 1: Fix VerifyToken CSRF, SubmitExam HC auth** - `941e74f` (fix)
2. **Task 2: Fix single-package question shuffle and populate option shuffle** - `e6ddffd` (fix)
3. **Task 3: Add AssessmentSessionId to UnifiedTrainingRecord** - `37d1f14` (feat)

## Files Created/Modified
- `Controllers/CMPController.cs` - CSRF fix, HC auth fix, question shuffle, option shuffle, AssessmentSessionId mapping
- `Models/UnifiedTrainingRecord.cs` - Added nullable AssessmentSessionId property

## Decisions Made
- VerifyToken CSRF fix requires plan 91-02 to add `__RequestVerificationToken` to the JS fetch call (noted in plan's key_links)
- Single-package shuffle: reversed prior "no shuffle" decision — workers should get different question orders even for single-package exams
- Option shuffle variable renamed from `allPackageQuestions` to `questionsForOptionShuffle` to avoid CS0136 scope conflict with existing outer-scope variable

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Renamed variable to fix CS0136 compile error**
- **Found during:** Task 2 (option shuffle implementation)
- **Issue:** New code used `allPackageQuestions` but an outer scope in StartExam already declared that name; dotnet build failed with CS0136
- **Fix:** Renamed inner variable to `questionsForOptionShuffle` — functionally identical
- **Files modified:** Controllers/CMPController.cs
- **Verification:** dotnet build passes with 0 CS errors (only MSB file-lock warnings from running app)
- **Committed in:** 37d1f14 (Task 3 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - bug/compile error)
**Impact on plan:** Necessary for build to pass. No scope creep.

## Issues Encountered
- `dotnet build` returned 2 MSB errors (file locked by running HcPortal process) — these are deployment errors, not compilation errors; 0 CS errors confirmed

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Plan 91-01 backend changes are complete and build-clean
- Plan 91-02 can now proceed: add CSRF token to VerifyToken JS call; use ShuffledOptionIdsPerQuestion to render options in shuffled order in StartExam.cshtml; render Results link in Records.cshtml using AssessmentSessionId
- No blockers

---
*Phase: 91-audit-fix-cmp-assessment-pages*
*Completed: 2026-03-04*
