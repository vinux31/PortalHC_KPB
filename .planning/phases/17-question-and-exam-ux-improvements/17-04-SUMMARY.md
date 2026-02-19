---
phase: 17-question-and-exam-ux-improvements
plan: "04"
subsystem: ui
tags: [razor, viewmodel, fisher-yates, shuffle, json, package-assignment]

# Dependency graph
requires:
  - phase: 17-question-and-exam-ux-improvements
    provides: AssessmentPackage, PackageQuestion, PackageOption, UserPackageAssignment models and DB tables (Plans 17-01 through 17-03)
provides:
  - PackageExamViewModel, ExamQuestionItem, ExamOptionItem classes in Models/PackageExamViewModel.cs
  - StartExam GET with per-user package assignment (random on first visit, resume on revisit)
  - Fisher-Yates shuffle for question and option order, persisted as JSON in UserPackageAssignment
  - Legacy fallback path for assessments with no packages
affects: [17-05-exam-view-redesign, any future exam submission/grading logic]

# Tech tracking
tech-stack:
  added: [System.Text.Json (now imported in CMPController)]
  patterns:
    - ViewModel pattern: PackageExamViewModel decouples controller data from view rendering
    - Idempotent assignment: query before insert, skip creation if assignment already exists
    - JSON string keys: option IDs serialized with .ToString() key for JSON object key compatibility

key-files:
  created:
    - Models/PackageExamViewModel.cs
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/StartExam.cshtml

key-decisions:
  - "ShuffledOptionIdsPerQuestion serialized with string keys using .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value) — JSON object keys must be strings; GetShuffledOptionIds() already handles re-parsing string keys back to int"
  - "StartExam view @model updated to PackageExamViewModel immediately (not deferred to 17-05) — required for project to compile"
  - "Package path queries AssessmentPackages directly; legacy path re-queries AssessmentSessions with Include for backward compatibility"

patterns-established:
  - "Exam ViewModel pattern: always pass PackageExamViewModel to StartExam view; grading uses IDs not display letters"
  - "Shuffle helper: private static Shuffle<T>(List<T>, Random) inside controller for reuse"

# Metrics
duration: 12min
completed: 2026-02-19
---

# Phase 17 Plan 04: PackageExamViewModel and StartExam Package Assignment Summary

**Per-user package assignment with Fisher-Yates shuffle persisted in UserPackageAssignment JSON fields, with legacy fallback for assessments without packages**

## Performance

- **Duration:** ~12 min
- **Started:** 2026-02-19T00:00:00Z
- **Completed:** 2026-02-19T00:12:00Z
- **Tasks:** 2
- **Files modified:** 3 (1 created, 2 modified)

## Accomplishments
- Created `PackageExamViewModel` with `ExamQuestionItem` and `ExamOptionItem` — the typed data shape for the exam view going forward
- Replaced the legacy `StartExam GET` with a two-branch implementation: package path (random assignment + Fisher-Yates shuffle + UserPackageAssignment persistence) and legacy path (old AssessmentQuestion/Option, no shuffle)
- Fisher-Yates shuffle helper added to CMPController as `private static void Shuffle<T>(List<T>, Random)`
- Shuffled question/option order persisted as JSON on first visit; resuming re-uses existing assignment without re-randomizing

## Task Commits

Each task was committed atomically:

1. **Task 1: Create PackageExamViewModel** - `003e8b2` (feat)
2. **Task 2: Extend StartExam GET with package assignment + randomization** - `003e8b2` (feat — committed together with Task 1)

**Plan metadata:** (see final commit below)

## Files Created/Modified
- `Models/PackageExamViewModel.cs` - PackageExamViewModel, ExamQuestionItem, ExamOptionItem classes
- `Controllers/CMPController.cs` - StartExam GET replaced; Shuffle<T> helper added; using System.Text.Json added
- `Views/CMP/StartExam.cshtml` - @model updated from AssessmentSession to PackageExamViewModel (blocking fix)

## Decisions Made
- Serialized option ID dictionary with string keys (`.ToDictionary(kv => kv.Key.ToString(), ...)`) — JSON spec requires string keys; the existing `GetShuffledOptionIds()` method in `UserPackageAssignment` already handles deserialization by parsing string keys back to int.
- Updated `StartExam.cshtml` @model directive immediately in this plan rather than waiting for Plan 17-05 — the view would not compile with the old `AssessmentSession` model after the controller change.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Updated StartExam.cshtml @model to PackageExamViewModel**
- **Found during:** Task 2 (replacing StartExam GET)
- **Issue:** View was strongly typed to `@model HcPortal.Models.AssessmentSession`; controller now passes `PackageExamViewModel`. Razor views are compiled — mismatched @model causes build failure.
- **Fix:** Updated `@model` to `HcPortal.Models.PackageExamViewModel`; updated all model property references (`@Model.Id` → `@Model.AssessmentSessionId`, `@Model.Questions.Count` → `@Model.TotalQuestions`, removed `.OrderBy(q => q.Order)` since questions are already in shuffled order, used `question.DisplayNumber` instead of `index`, used `question.QuestionId` / `opt.OptionId` for radio name/value).
- **Files modified:** `Views/CMP/StartExam.cshtml`
- **Verification:** `dotnet build` exits 0 with 0 errors.
- **Committed in:** `003e8b2` (part of task commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Necessary for project to build. The plan note said "for now just make the controller compile" — updating the view @model was the minimum required to achieve that. No scope creep; the view body now uses PackageExamViewModel correctly.

## Issues Encountered
- None beyond the blocking view @model issue (documented above).

## User Setup Required
None — no external service configuration required.

## Self-Check: PASSED

Files verified:
- `Models/PackageExamViewModel.cs` — FOUND
- `Controllers/CMPController.cs` — modified (StartExam GET replaced, Shuffle helper present)
- `Views/CMP/StartExam.cshtml` — modified (@model updated)
- Commit `003e8b2` — FOUND

Build result: `Build succeeded. 0 Error(s).`

## Next Phase Readiness
- `PackageExamViewModel` is ready for the full exam view redesign in Plan 17-05
- `StartExam GET` now delivers per-user shuffled question/option order to the view
- `UserPackageAssignment` rows are created on first visit; grading in future plans will use stable option IDs from these records
- The view currently renders a minimal functional layout; Plan 17-05 will implement the full paged/flagged exam UI

---
*Phase: 17-question-and-exam-ux-improvements*
*Completed: 2026-02-19*
