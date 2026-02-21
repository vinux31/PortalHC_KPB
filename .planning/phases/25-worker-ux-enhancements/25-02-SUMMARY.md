---
phase: 25-worker-ux-enhancements
plan: 02
subsystem: ui
tags: [razor, viewmodel, competency, assessment-results]

# Dependency graph
requires:
  - phase: 25-01
    provides: competency mapping infrastructure (AssessmentCompetencyMap, UserCompetencyLevel)
provides:
  - CompetencyGainItem class on AssessmentResultsViewModel
  - CompetencyGains list property on AssessmentResultsViewModel
  - Kompetensi Diperoleh card on Results page showing earned competencies
affects: [CMP Results view, worker assessment post-completion UX]

# Tech tracking
tech-stack:
  added: []
  patterns: [shared post-branch query block in Results action using pre-declared viewModel variable]

key-files:
  created: []
  modified:
    - Models/AssessmentResultsViewModel.cs
    - Controllers/CMPController.cs
    - Views/CMP/Results.cshtml
    - Views/CMP/Assessment.cshtml

key-decisions:
  - "viewModel declared outside if/else branches in Results action — enables shared competency lookup block after both package and legacy paths"
  - "CompetencyGains only populated when IsPassed=true — failed assessments never show the competency section"
  - "Double null guard in view: Model.CompetencyGains != null && Model.CompetencyGains.Any() — card invisible when no mappings exist"
  - "Competency query is READ-ONLY — no DB mutations, mirrors pattern from SubmitExam but without UserCompetencyLevel updates"

patterns-established:
  - "Post-branch shared block pattern: declare variable before if/else, assign inside each branch, add shared logic after closing brace"

# Metrics
duration: 4min
completed: 2026-02-21
---

# Phase 25 Plan 02: Kompetensi Diperoleh on Results Page Summary

**Assessment Results page now shows a "Kompetensi Diperoleh" card listing earned competency names and levels when a worker passes an assessment with category-to-competency mappings**

## Performance

- **Duration:** 4 min
- **Started:** 2026-02-21T04:13:47Z
- **Completed:** 2026-02-21T04:18:02Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- Added `CompetencyGainItem` class and `CompetencyGains` list property to `AssessmentResultsViewModel`
- Refactored Results action to declare viewModel before branches, enabling shared post-branch competency lookup
- Results action queries `AssessmentCompetencyMaps` (with `KkjMatrixItem` include) after both package and legacy paths when `IsPassed=true`
- Results.cshtml renders "Kompetensi Diperoleh" card between motivational message and Answer Review sections — invisible when no competencies earned

## Task Commits

Each task was committed atomically:

1. **Task 1: Add CompetencyGainItem to ViewModel** - `c2d2350` (feat)
2. **Task 2: Populate CompetencyGains in Results action and render Kompetensi Diperoleh card** - `9631939` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified
- `Models/AssessmentResultsViewModel.cs` - Added `CompetencyGains` property and `CompetencyGainItem` class
- `Controllers/CMPController.cs` - Refactored Results action for shared competency lookup + added query
- `Views/CMP/Results.cshtml` - Added Kompetensi Diperoleh card between motivational alert and Answer Review
- `Views/CMP/Assessment.cshtml` - Fixed pre-existing Razor syntax error (var declarations in else-if code block)

## Decisions Made
- viewModel declared outside if/else branches in Results action so shared competency block can run after either path
- CompetencyGains only populated when IsPassed=true — aligns with semantics (earning competencies requires passing)
- Card uses double guard (`!= null && .Any()`) to handle both failed assessments (null) and passed with no mappings (also null)
- Used `?? "Unknown"` null guard on `KkjMatrixItem?.Kompetensi` to prevent NRE if navigation property not loaded

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Fixed pre-existing Razor syntax error in Assessment.cshtml**
- **Found during:** Task 1 (build verification after ViewModel changes)
- **Issue:** `var daysUntil` and `var daysText` declarations inside `@if` code block without `@{}` wrapper, followed by a later `@{` block that Razor couldn't parse — caused `RZ1010: Unexpected "{" after "@"` build error
- **Fix:** Replaced multi-line variable declarations with inline `@()` Razor expression in the button text; removed orphaned `@{` wrapping the `completedHistory` variable (which is inside the outer `else` C# code context and doesn't need `@`)
- **Files modified:** `Views/CMP/Assessment.cshtml`
- **Verification:** `dotnet build` succeeds with 0 errors
- **Committed in:** `c2d2350` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Pre-existing Razor syntax error from a prior session blocked the build. Fix was required to verify Task 1. No scope creep — no behavior changed in Assessment.cshtml.

## Issues Encountered
None — implementation matched plan exactly.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Phase 25 Plan 02 complete. Both plans in Phase 25 are now done.
- Ready for Phase 25 completion or next phase.

---
*Phase: 25-worker-ux-enhancements*
*Completed: 2026-02-21*

## Self-Check: PASSED

- FOUND: Models/AssessmentResultsViewModel.cs (CompetencyGains property + CompetencyGainItem class)
- FOUND: Controllers/CMPController.cs (CompetencyGains population logic)
- FOUND: Views/CMP/Results.cshtml (Kompetensi Diperoleh card)
- FOUND: .planning/phases/25-worker-ux-enhancements/25-02-SUMMARY.md
- FOUND: c2d2350 — feat(25-02): add CompetencyGainItem class and CompetencyGains property to ViewModel
- FOUND: 9631939 — feat(25-02): populate CompetencyGains in Results action and render Kompetensi Diperoleh card
