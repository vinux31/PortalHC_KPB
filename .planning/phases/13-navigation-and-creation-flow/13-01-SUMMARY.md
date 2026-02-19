---
phase: 13-navigation-and-creation-flow
plan: 01
subsystem: ui
tags: [razor, csharp, cmp, assessment, navigation]

# Dependency graph
requires:
  - phase: 12-dashboard-consolidation
    provides: clean manage view baseline with Create Assessment button already in place (CRT-01 pre-existing)
provides:
  - Clean CMP Index with Assessment Lobby card (all roles) and Manage Assessments card (HC/Admin only)
  - Sync Index() controller action with no data loading
  - CreateAssessment POST redirects to manage view on success
affects:
  - 13-02 and beyond — manage view is now the canonical post-creation landing point

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Role-gated navigation cards: @if (User.IsInRole("HC") || User.IsInRole("Admin")) wrapping separate cards rather than branching inside a single card
    - Clean controller actions: Index() returns View() with no data preloading when view needs none

key-files:
  created: []
  modified:
    - Views/CMP/Index.cshtml
    - Controllers/CMPController.cs

key-decisions:
  - "Assessment Lobby card is universal (all roles) — HC/Admin also see their personal lobby; Manage Assessments is a separate HC/Admin-only card"
  - "TempData[CreatedAssessment] serialization kept in CreateAssessment POST even though Index no longer reads it — harmless and may be useful in future manage view enhancement"

patterns-established:
  - "Separate cards per concern rather than branching button sets inside one card"

# Metrics
duration: 3min
completed: 2026-02-19
---

# Phase 13 Plan 01: Navigation and Creation Flow Summary

**CMP Index redesigned with clean role-gated navigation cards and CreateAssessment POST redirecting to manage view instead of Index**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-02-19T11:09:04Z
- **Completed:** 2026-02-19T11:12:06Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- Removed entire embedded Create Assessment form from CMP Index (form, success modal, 224 lines of JS, form-switch style, categories variable)
- Replaced single branching Assessments card with two separate cards: Assessment Lobby (all roles) and Manage Assessments (HC/Admin only) with Create New and Manage buttons
- Reverted Index() controller action to synchronous `public IActionResult Index()` with no data loading (removed async, Task, user/role lookup, ViewBag data)
- Fixed CreateAssessment POST success redirect from `Index` to `Assessment?view=manage` so HC lands on the manage view after creation

## Task Commits

Each task was committed atomically:

1. **Task 1: Remove embedded form from CMP Index and redesign Assessments card section** - `e72f49e` (feat)
2. **Task 2: Fix CreateAssessment POST redirect to manage view** - `96934a6` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified
- `Views/CMP/Index.cshtml` - Removed form/modal/JS/form-switch-style/categories var; added Assessment Lobby card (universal) and Manage Assessments card (HC/Admin role-gated)
- `Controllers/CMPController.cs` - Index() reverted to sync; CreateAssessment POST redirect changed from Index to Assessment?view=manage

## Decisions Made
- Assessment Lobby card is universal (all roles including HC/Admin) so HC/Admin also retain easy access to their personal assessment view. Manage Assessments is a separate card.
- TempData["CreatedAssessment"] serialization kept in CreateAssessment POST — it no longer gets consumed by Index.cshtml but causes no harm sitting in TempData. The manage view can potentially read it in a future enhancement.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- `dotnet build` reported MSB3027/MSB3021 file-lock errors because the dev server was running and locking `HcPortal.exe`. No C# compilation errors were present. This is expected behavior when building while the dev server is running.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- CMP Index is clean: 4 cards for workers (KKJ, CPDP, Assessment Lobby, Training Records), 5 cards for HC/Admin (+ Manage Assessments)
- Manage Assessments card has correct links: Create New → /CMP/CreateAssessment, Manage → /CMP/Assessment?view=manage
- CreateAssessment POST redirects to manage view on success
- Phase 14 (Bulk Assign via EditAssessment) and Phase 15 (Quick Edit) can proceed with clean baseline

---
*Phase: 13-navigation-and-creation-flow*
*Completed: 2026-02-19*
