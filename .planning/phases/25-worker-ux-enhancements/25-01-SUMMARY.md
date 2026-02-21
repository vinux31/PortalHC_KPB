---
phase: 25-worker-ux-enhancements
plan: 01
subsystem: ui
tags: [razor, viewbag, assessment, worker-ux]

# Dependency graph
requires:
  - phase: 22-exam-lifecycle-actions
    provides: AssessmentSession Status="Completed" and CompletedAt fields
  - phase: 23-package-answer-integrity
    provides: Results action for completed session detail view
provides:
  - Completed assessment history visible on worker Assessment page via ViewBag.CompletedHistory
affects: [worker-ux, assessment-page, cmp-controller]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Direct C# statements at top level of Razor else-block (no @{} wrapping needed after last HTML element)
    - Razor @* *@ comment syntax for code-context comments inside if/else blocks

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/Assessment.cshtml

key-decisions:
  - "Riwayat Ujian query placed in worker branch only — HC/Admin branch returns early at line 197 and never executes it"
  - "Razor @{} wrapping fails after last HTML element in else-block; direct var/if C# statements work correctly at that nesting level"
  - "HTML comment inside Razor C# context replaced with @* *@ Razor comment to avoid parse ambiguity"

patterns-established:
  - "After the last HTML element at the top level of a Razor @if/@else block, subsequent C# code does not need @{} wrapping — use direct statements"

# Metrics
duration: 4min
completed: 2026-02-21
---

# Phase 25 Plan 01: Worker UX Enhancements — Riwayat Ujian Summary

**Riwayat Ujian (exam history) table added to worker Assessment page, showing completed assessments with title, category badge, completion date, score%, and Lulus/Tidak Lulus status with Detail link per row**

## Performance

- **Duration:** ~4 min
- **Started:** 2026-02-21T04:13:36Z
- **Completed:** 2026-02-21T04:17:16Z
- **Tasks:** 1
- **Files modified:** 2

## Accomplishments
- Worker's Assessment page now shows all their completed assessments in a "Riwayat Ujian" table below the Open/Upcoming card grid
- Controller query scoped to current user + Status=="Completed", sorted by CompletedAt DESC
- Table shows title, category badge, completion date (dd MMM yyyy), score%, and Lulus/Tidak Lulus badge; each row links to Results page
- HC/Admin manage view structurally unaffected — completedHistory query is inside the worker-only else branch

## Task Commits

Each task was committed atomically:

1. **Task 1: Query completed assessments and render Riwayat Ujian table** - `65efba8` (feat)

**Plan metadata:** _(pending)_

## Files Created/Modified
- `Controllers/CMPController.cs` - Added completedHistory query and ViewBag.CompletedHistory assignment in worker personal branch before return View(exams)
- `Views/CMP/Assessment.cshtml` - Added Riwayat Ujian table in worker else-branch using direct C# var/if statements (no @{} wrapper)

## Decisions Made
- Placed the completedHistory query AFTER all ViewBag assignments and just before `return View(exams)` — logically grouped with the worker path
- Used direct C# variable declaration (`var completedHistory = ...`) without `@{}` because at that nesting level in the Razor else-block we are back in C# context (after the last HTML element), not HTML context
- Replaced HTML comment `<!-- Riwayat Ujian -->` with Razor comment `@* Riwayat Ujian *@` to avoid Razor parser ambiguity when the comment appears in C# code context

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Razor syntax fix: HTML comment and @{} inside C# context**
- **Found during:** Task 1 (build verification)
- **Issue:** Plan specified using `@{ var completedHistory = ... }` and `<!-- HTML comment -->` inside the else-block. After the last HTML element at top level of the else-block, the Razor parser is in C# mode — `@{}` is invalid and HTML comments cause parse errors (RZ1010)
- **Fix:** Removed `@{}` wrapping from the var declaration (direct C# statement), removed `@` prefix from the if statement, replaced `<!-- -->` HTML comment with `@* *@` Razor comment
- **Files modified:** Views/CMP/Assessment.cshtml
- **Verification:** dotnet build succeeded with 0 errors
- **Committed in:** 65efba8 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - Razor syntax bug)
**Impact on plan:** Syntax fix only — functionally identical to plan spec. No scope change.

## Issues Encountered
- First build attempt failed with RZ1010 (Unexpected "{" after "@"). Root cause: Razor parser is in C# mode after the last top-level HTML element in an else-block. The linter auto-stripped `@{}` and `@` from the if, but left the HTML comment causing further confusion. Fixed by using direct C# statements and Razor `@*...*@` comments.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Worker Assessment page now closes the feedback loop: workers see Open/Upcoming cards AND their completed exam history on one page
- Ready for Phase 25-02 (if any additional worker UX enhancements planned)

## Self-Check: PASSED

- Controllers/CMPController.cs: FOUND
- Views/CMP/Assessment.cshtml: FOUND
- .planning/phases/25-worker-ux-enhancements/25-01-SUMMARY.md: FOUND
- Commit 65efba8: FOUND
- ViewBag.CompletedHistory in controller: FOUND (line 261)
- "Riwayat Ujian" in view: FOUND (lines 528, 534)

---
*Phase: 25-worker-ux-enhancements*
*Completed: 2026-02-21*
