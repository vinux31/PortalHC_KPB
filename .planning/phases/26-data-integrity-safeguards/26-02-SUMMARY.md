---
phase: 26-data-integrity-safeguards
plan: 02
subsystem: ui
tags: [razor, javascript, confirm-dialog, assessment, packages, schedule]

# Dependency graph
requires:
  - phase: 26-data-integrity-safeguards-01
    provides: DATA-01 context (sibling session pattern, package attachment model)
provides:
  - JS schedule-change confirmation guard on EditAssessment form when packages are attached
  - EditAssessment GET passes PackageCount and OriginalSchedule to view via ViewBag
affects: [EditAssessment flow, package-based assessments, CMPController]

# Tech tracking
tech-stack:
  added: []
  patterns: [ViewBag-to-JS Razor emission pattern for server-side values in client guards]

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/EditAssessment.cshtml

key-decisions:
  - "Client-side only guard (no server-side confirm page) — JS confirm() on form submit matches existing pattern"
  - "IIFE placed before Bootstrap validation block — fires before validation, confirm cancels immediately via e.preventDefault()"
  - "Sibling-group package count uses same siblings variable already computed in EditAssessment GET — no extra query overhead"
  - "OriginalSchedule passed as yyyy-MM-dd string matching HTML date input value format for direct string comparison"

patterns-established:
  - "ViewBag int/string -> Razor @variable -> JS IIFE pattern for embedding server-side data in client guards"

# Metrics
duration: 4min
completed: 2026-02-21
---

# Phase 26 Plan 02: Schedule-Change Warning on EditAssessment Summary

**JS confirm guard on EditAssessment form warns HC when changing schedule date on an assessment with attached packages, using server-side package count passed to view via ViewBag**

## Performance

- **Duration:** 4 min
- **Started:** 2026-02-21T04:32:14Z
- **Completed:** 2026-02-21T04:36:00Z
- **Tasks:** 1
- **Files modified:** 2

## Accomplishments
- EditAssessment GET now queries sibling-group package count via `AssessmentPackages.CountAsync` and passes it plus original schedule date to the view
- EditAssessment form has `id="editAssessmentForm"` for reliable JS targeting
- JS IIFE in the Scripts section detects date changes and calls `confirm()` with a bilingual warning message before allowing submission
- When no packages exist (packageCount = 0) the IIFE exits immediately — zero overhead for the common case

## Task Commits

Each task was committed atomically:

1. **Task 1: Add package count to EditAssessment GET and schedule-change confirm to view** - `51d4323` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified
- `Controllers/CMPController.cs` - Added sibling-group package count query and ViewBag.PackageCount + ViewBag.OriginalSchedule after ViewBag.Sections assignment
- `Views/CMP/EditAssessment.cshtml` - Added Razor variable extraction from ViewBag, id on form tag, and JS IIFE schedule-change guard in @section Scripts

## Decisions Made
- Client-side guard only (no second POST or server-side confirmation page) — `confirm()` pattern is already established throughout the project and is sufficient for this use case
- IIFE inserted before the existing Bootstrap validation block so the schedule warning fires first; if HC cancels, Bootstrap validation is never reached
- Package count uses the `siblings` variable (sibling sessions with same Title+Category+Date) already computed earlier in the GET action — consistent with how packages are attached to a representative session in the sibling group
- `OriginalSchedule` emitted as `yyyy-MM-dd` string matching the HTML `<input type="date">` `.value` format, enabling direct `===` comparison in JS without date parsing

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- DATA-02 satisfied: schedule-change warning is live
- Phase 26 plans 01 and 02 both complete — phase ready for overall verification
- No blockers

---
*Phase: 26-data-integrity-safeguards*
*Completed: 2026-02-21*

## Self-Check: PASSED

- FOUND: Controllers/CMPController.cs
- FOUND: Views/CMP/EditAssessment.cshtml
- FOUND: .planning/phases/26-data-integrity-safeguards/26-02-SUMMARY.md
- FOUND commit: 51d4323
