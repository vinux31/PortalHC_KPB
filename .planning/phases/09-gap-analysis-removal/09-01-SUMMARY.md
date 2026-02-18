---
phase: 09-gap-analysis-removal
plan: 01
subsystem: ui
tags: [razor, cshtml, cmp, competency, cleanup]

# Dependency graph
requires: []
provides:
  - CMP Index hub without Gap Analysis card
  - CPDP Progress page without Gap Analysis nav link
  - CMPController without CompetencyGap action or GenerateIdpSuggestion helper
  - Zero orphaned files (CompetencyGap.cshtml and CompetencyGapViewModel.cs deleted)
affects: [10-training-records-consolidation, 12-dashboard-consolidation]

# Tech tracking
tech-stack:
  added: []
  patterns: []

key-files:
  created: []
  modified:
    - Views/CMP/Index.cshtml
    - Views/CMP/CpdpProgress.cshtml
    - Controllers/CMPController.cs
  deleted:
    - Views/CMP/CompetencyGap.cshtml
    - Models/Competency/CompetencyGapViewModel.cs

key-decisions:
  - "Feature deleted outright — no redirect stub added (STATE.md decision noted RedirectToAction stub for 'one release cycle' but plan specified hard deletion; executed as hard deletion per plan)"

patterns-established:
  - "Dead nav removal: delete card block, cross-link, action, view, and ViewModel atomically in same phase"

# Metrics
duration: 8min
completed: 2026-02-18
---

# Phase 9 Plan 01: Gap Analysis Removal Summary

**Complete removal of Gap Analysis feature — nav card deleted from CMP Index, cross-link removed from CPDP Progress, CompetencyGap action and GenerateIdpSuggestion helper deleted from CMPController, orphaned view and ViewModel files removed, build clean with zero errors.**

## Performance

- **Duration:** ~8 min
- **Started:** 2026-02-18
- **Completed:** 2026-02-18
- **Tasks:** 2
- **Files modified/deleted:** 5

## Accomplishments
- Removed Gap Analysis card block (20 lines) from CMP Index hub — grid reflows cleanly to remaining 5 cards
- Removed Gap Analysis nav link from CPDP Progress navigation tabs — self-link remains as sole tab
- Deleted CompetencyGap() action (~100 lines) and GenerateIdpSuggestion() helper (~23 lines) from CMPController.cs
- Deleted orphaned Views/CMP/CompetencyGap.cshtml and Models/Competency/CompetencyGapViewModel.cs
- dotnet build: 0 errors, 37 pre-existing warnings (all from unrelated code)

## Task Commits

Each task was committed atomically:

1. **Task 1: Remove nav links and delete controller action** - `4b8981d` (feat)
2. **Task 2: Delete orphaned files and verify clean build** - `40f81e3` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified

- `Views/CMP/Index.cshtml` - Removed Gap Analysis card block (lines 58-77 removed)
- `Views/CMP/CpdpProgress.cshtml` - Removed Gap Analysis nav link from navigation tabs
- `Controllers/CMPController.cs` - Deleted CompetencyGap() action and GenerateIdpSuggestion() private helper
- `Views/CMP/CompetencyGap.cshtml` - Deleted (orphaned view)
- `Models/Competency/CompetencyGapViewModel.cs` - Deleted (orphaned model with CompetencyGapViewModel and CompetencyGapItem classes)

## Decisions Made

- Executed as hard deletion with no redirect stub. STATE.md had noted a potential RedirectToAction("CpdpProgress") stub "for one release cycle" as a v1.2 roadmap decision, but the 09-01-PLAN.md specified complete removal with zero CompetencyGap strings remaining. Plan took precedence — stub would violate the must_haves contract.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None. The `using HcPortal.Models.Competency;` directive was correctly preserved as required — it is still consumed by CpdpProgressViewModel, UserCompetencyLevel, and AssessmentCompetencyMap in the remaining controller actions.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 9 complete. CMP Index and CPDP Progress are clean of Gap Analysis references.
- Phase 10 (Training Records Consolidation) can proceed — no dependencies on removed code.
- Phase 12 (Dashboard Consolidation) should still grep for `"ReportsIndex"` implicit-controller calls before execution (STATE.md blocker unchanged).

---
*Phase: 09-gap-analysis-removal*
*Completed: 2026-02-18*

## Self-Check: PASSED

- FOUND: 4b8981d (Task 1 commit)
- FOUND: 40f81e3 (Task 2 commit)
- CONFIRMED ABSENT: Views/CMP/CompetencyGap.cshtml
- CONFIRMED ABSENT: Models/Competency/CompetencyGapViewModel.cs
- CONFIRMED ABSENT: Zero CompetencyGap in *.cs files
- CONFIRMED ABSENT: Zero CompetencyGap in *.cshtml files
- CONFIRMED PRESENT: using HcPortal.Models.Competency; in CMPController.cs
- BUILD: dotnet build succeeded, 0 errors
