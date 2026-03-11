---
phase: 151-homepage-progress-overview-and-upcoming-events-fix
plan: 01
subsystem: ui
tags: [dashboard, homepage, progress-bar, upcoming-events, coaching]

# Dependency graph
requires: []
provides:
  - Upcoming Events restricted to today+tomorrow for both coaching and assessment
  - CoachingProgress percentage property on ProgressViewModel
  - Coaching Sessions progress bar (yellow/warning) on homepage Progress Overview
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns: [DateTime.Today + AddDays(2).AddTicks(-1) for end-of-tomorrow boundary]

key-files:
  created: []
  modified:
    - Controllers/HomeController.cs
    - Models/DashboardHomeViewModel.cs
    - Views/Home/Index.cshtml

key-decisions:
  - "Use DateTime.Today.AddDays(2).AddTicks(-1) as end-of-tomorrow to capture the full tomorrow day"
  - "Remove .Take(3) on individual queries since date window already limits results"
  - "Coaching progress bar uses bg-warning (yellow) to differentiate from CDP (blue) and Assessment (green)"

patterns-established:
  - "Progress bar pattern: d-flex justify-content-between header + div.progress + small fraction text"

requirements-completed: []

# Metrics
duration: 10min
completed: 2026-03-11
---

# Phase 151 Plan 01: Homepage Progress Overview and Upcoming Events Fix Summary

**Restricted Upcoming Events to today/tomorrow only and added a yellow Coaching Sessions progress bar consistent with CDP and Assessment sections**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-03-11T~
- **Completed:** 2026-03-11
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments

- GetUpcomingEvents now filters coaching sessions to today and tomorrow only (was: all future dates)
- GetUpcomingEvents now filters assessments to today and tomorrow only (was: all open/upcoming with no date cap)
- ProgressViewModel gained CoachingProgress property; HomeController computes it after coaching query
- Coaching Sessions section in Progress Overview now shows a yellow progress bar + percentage + fraction, matching CDP and Assessment layout

## Task Commits

1. **Task 1: Fix Upcoming Events date filter and add CoachingProgress to model** - `9cefd85` (fix)
2. **Task 2: Update homepage view to show Coaching progress bar** - `7614f03` (feat)

## Files Created/Modified

- `Controllers/HomeController.cs` - Fixed GetUpcomingEvents date filter; added CoachingProgress computation in GetProgress
- `Models/DashboardHomeViewModel.cs` - Added CoachingProgress property to ProgressViewModel
- `Views/Home/Index.cshtml` - Replaced Coaching badge display with progress bar matching CDP/Assessment layout

## Decisions Made

- Used `DateTime.Today.AddDays(2).AddTicks(-1)` as the end-of-tomorrow boundary to include the entire tomorrow day
- Removed `.Take(3)` on individual queries since the narrow date window naturally limits results; kept `.Take(5)` on the final ordered list
- No badge kept — progress bar + percentage + fraction text is sufficient and more informative

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Homepage dashboard now shows three consistent progress bars and an event list filtered to immediately relevant dates
- No blockers for subsequent phases

---
*Phase: 151-homepage-progress-overview-and-upcoming-events-fix*
*Completed: 2026-03-11*
