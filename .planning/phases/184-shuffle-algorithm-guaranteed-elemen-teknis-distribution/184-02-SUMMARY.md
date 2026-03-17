---
phase: 184-shuffle-algorithm-guaranteed-elemen-teknis-distribution
plan: 02
subsystem: ui
tags: [assessment, elemen-teknis, coverage-table, import-warning, razor]

# Dependency graph
requires:
  - phase: 184-01
    provides: ET-aware shuffle algorithm and ElemenTeknis property on PackageQuestion
provides:
  - ET coverage table on ManagePackages page showing per-package question distribution
  - Enhanced import warning naming specific packages missing specific ET groups
affects: [assessment-management, hc-workflow]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "ViewBag dictionary (string -> Dictionary<int,int>) for cross-tabulation data to Razor views"
    - "Per-package missing-group analysis using LINQ Except() for actionable warnings"

key-files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/ManagePackages.cshtml

key-decisions:
  - "(Tanpa ET) row sorted last using OrderBy sentinel 'zzz' string comparison"
  - "No warning styling on (Tanpa ET) row — null ET is expected data, not a coverage gap"
  - "Import warning uses full DB reload of all packages (not just in-memory rows) for accurate cross-package comparison"

patterns-established:
  - "ET coverage table: rows=ET groups, columns=packages+total, warning icon for 0-count cells (excluding Tanpa ET)"

requirements-completed: [SHUF-01, SHUF-02]

# Metrics
duration: 15min
completed: 2026-03-17
---

# Phase 184 Plan 02: ET Coverage UI Summary

**ET coverage table on ManagePackages with per-package question counts and missing-group warnings; import upload warning now names specific packages and ET groups with incomplete distribution**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-03-17T07:30:00Z
- **Completed:** 2026-03-17T07:45:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- ManagePackages controller computes ET group coverage across all packages and passes it via ViewBag.EtCoverage + ViewBag.EtGroups
- "Distribusi Elemen Teknis" table renders with warning icon (bi-exclamation-triangle) and red text for packages missing any ET group
- (Tanpa ET) row displayed without warning styling and sorted last
- Import warning replaced: instead of generic "ET groups differ", now lists each package name and the specific ET groups it is missing

## Task Commits

1. **Task 1: Compute ET coverage data and render coverage table** - `b72c9a8` (feat)
2. **Task 2: Enhance import upload ET distribution warning** - `9fd814e` (feat)

## Files Created/Modified

- `Controllers/AdminController.cs` - Added ET coverage computation in ManagePackages GET; replaced import warning block with per-package missing-group analysis
- `Views/Admin/ManagePackages.cshtml` - Added Distribusi Elemen Teknis card with responsive table after summary card

## Decisions Made

- (Tanpa ET) row excluded from warning styling — null ET on questions is valid data, not a missing-coverage problem
- Import warning performs fresh DB Include query (not in-memory row scan) so it captures the full post-import state of all sibling packages

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

Build showed file-lock error (HcPortal.exe locked by running app process) — not a compile error. All C# compile checks passed with 0 errors.

## Next Phase Readiness

- Both 184 plans complete; v7.3 milestone deliverables shipped
- ManagePackages now gives HC full visibility into ET distribution before workers start exams
- Ready for `/gsd:new-milestone`

---
*Phase: 184-shuffle-algorithm-guaranteed-elemen-teknis-distribution*
*Completed: 2026-03-17*
