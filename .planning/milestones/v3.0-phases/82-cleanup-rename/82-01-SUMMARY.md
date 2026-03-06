---
phase: 82-cleanup-rename
plan: 01
subsystem: ui
tags: [rename, cdp, coaching-proton, asp-net-mvc, cshtml]

# Dependency graph
requires: []
provides:
  - "CDPController.CoachingProton action replaces ProtonProgress (route /CDP/CoachingProton is live)"
  - "Views/CDP/CoachingProton.cshtml (renamed from ProtonProgress.cshtml)"
  - "Views/CDP/Shared/_CoachingProtonPartial.cshtml (renamed from _ProtonProgressPartial.cshtml)"
  - "Dashboard tab label says 'Coaching Proton'"
  - "CDP hub card heading and button text say 'Coaching Proton'"
  - "Excel worksheet and PDF header export strings say 'Coaching Proton'"
affects: [85-coaching-proton-flow-qa, 87-dashboard-navigation-qa]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Rename display-facing text only; leave internal class/method names unchanged (ProtonProgressSubModel, BuildProtonProgressSubModelAsync, ProtonProgressData)"

key-files:
  created:
    - Views/CDP/CoachingProton.cshtml
    - Views/CDP/Shared/_CoachingProtonPartial.cshtml
  modified:
    - Controllers/CDPController.cs
    - Views/CDP/Dashboard.cshtml
    - Views/CDP/Index.cshtml

key-decisions:
  - "Internal identifiers (ProtonProgressSubModel, BuildProtonProgressSubModelAsync, ProtonProgressData) left unchanged — display-facing strings only renamed"
  - "Old files (ProtonProgress.cshtml, _ProtonProgressPartial.cshtml) deleted, not kept as aliases — /CDP/ProtonProgress now returns 404"

patterns-established:
  - "Cleanup renames: update controller action + all Url.Action calls + view file name + partial file name + display text in one atomic set of commits"

requirements-completed: [CLN-01]

# Metrics
duration: 2min
completed: 2026-03-02
---

# Phase 82 Plan 01: Proton Progress to Coaching Proton Rename Summary

**Renamed "Proton Progress" to "Coaching Proton" across CDPController action, view files, partial files, all display text, form targets, pagination links, Excel worksheet name, and PDF header — /CDP/CoachingProton is live, /CDP/ProtonProgress returns 404.**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-02T06:25:34Z
- **Completed:** 2026-03-02T06:27:34Z
- **Tasks:** 2
- **Files modified:** 5 (CDPController.cs, CoachingProton.cshtml new, _CoachingProtonPartial.cshtml new, Dashboard.cshtml, Index.cshtml) + 2 deleted (ProtonProgress.cshtml, _ProtonProgressPartial.cshtml)

## Accomplishments
- CDPController.CoachingProton action is live; old ProtonProgress action removed (old URL returns 404)
- Excel export worksheet name changed from "Proton Progress" to "Coaching Proton"
- PDF export header changed from "Proton Progress — {name}" to "Coaching Proton — {name}"
- CDP hub card and Dashboard tab both display "Coaching Proton"
- All pagination Url.Action links and filter form asp-action point to CoachingProton

## Task Commits

Each task was committed atomically:

1. **Task 1: Rename CDPController action and update all CDPController references** - `aeac468` (feat)
2. **Task 2: Rename view files and update all display text in views** - `9889284` (feat)

**Plan metadata:** (created next)

## Files Created/Modified
- `Controllers/CDPController.cs` - Action renamed ProtonProgress->CoachingProton; 2 RedirectToAction calls updated; Excel and PDF strings updated
- `Views/CDP/CoachingProton.cshtml` - Renamed from ProtonProgress.cshtml; ViewData title, h2, form action, all Url.Action pagination links updated
- `Views/CDP/Shared/_CoachingProtonPartial.cshtml` - Renamed from _ProtonProgressPartial.cshtml; content unchanged (model class name ProtonProgressSubModel kept per decision)
- `Views/CDP/Dashboard.cshtml` - Tab label text and partial reference updated
- `Views/CDP/Index.cshtml` - Card heading, Url.Action href, and button text updated
- `Views/CDP/ProtonProgress.cshtml` - DELETED
- `Views/CDP/Shared/_ProtonProgressPartial.cshtml` - DELETED

## Decisions Made
- Internal identifiers (ProtonProgressSubModel, BuildProtonProgressSubModelAsync, ProtonProgressData) left unchanged — only display-facing strings renamed
- Old view files deleted (not kept as stubs or redirects) — /CDP/ProtonProgress now returns 404 as intended by requirements

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- The Coaching Proton feature is correctly named; QA testing in Phase 85 (Coaching Proton Flow QA) can proceed with the correct URL /CDP/CoachingProton
- Plans 82-02 and 82-03 are independent and can proceed

---
*Phase: 82-cleanup-rename*
*Completed: 2026-03-02*
