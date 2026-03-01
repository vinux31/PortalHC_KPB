---
phase: 64-functional-filters
plan: 01
subsystem: api
tags: [efcore, csharp, aspnetcore, role-scoping, filtering]

# Dependency graph
requires:
  - phase: 63-data-source-fix
    provides: ProtonProgress action with real ProtonDeliverableProgress data source

provides:
  - ProtonProgress action with 5 filter params (bagian, unit, trackType, tahun, coacheeId)
  - Role-scoped EF Core Where clause composition before ToListAsync materialization
  - TrackingItem model with CoacheeId and CoacheeName fields for multi-coachee results
  - ViewBag populated with AllBagian/AllUnits/AllTracks/AllTahun/Coachees and Selected* values
  - ViewBag.TotalCount and ViewBag.FilteredCount for "Menampilkan X dari Y" display

affects:
  - 64-02 (view rendering for filter dropdowns consumes these ViewBag values)
  - 65-actions (approval/reject actions use TrackingItem.CoacheeId for routing)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Role-scope-first: derive scopedCoacheeIds from role before applying any URL params"
    - "EF Where composition: build IQueryable, chain .Where() calls, single ToListAsync()"
    - "OrganizationStructure validation: bagian validated via GetAllSections(), unit via GetUnitsForSection()"
    - "ProtonTrack ThenInclude in deliverable query chain for TrackType/TahunKe filter access"

key-files:
  created: []
  modified:
    - Models/TrackingModels.cs
    - Controllers/CDPController.cs

key-decisions:
  - "ProtonTrack ThenInclude added to deliverable query chain — Phase 63 did not include it; required for Track/Tahun EF Where clauses to resolve"
  - "dataCoacheeIds derived from scopedCoacheeIds: if single coacheeId selected use that, else use full scoped list — data always loads"
  - "Coach Track cascade narrows scopedCoacheeIds via ProtonTrackAssignments before dropdown population — coachee dropdown shows only matching coachees"
  - "TrackLabel still computed for single-coachee case; omitted (empty string) for multi-coachee"
  - "NoTrackMessage / NoProgressMessage removed; replaced with generic EmptyMessage for filter-empty state"

patterns-established:
  - "ViewBag.AllBagian, AllUnits, AllTracks, AllTahun: populate once per request from OrganizationStructure static methods"
  - "Selected* ViewBag values: echoed back from validated (possibly nulled) param locals"

requirements-completed: [FILT-01, FILT-02, FILT-03, UI-03]

# Metrics
duration: 2min
completed: 2026-02-27
---

# Phase 64 Plan 01: Functional Filters — Server-Side Summary

**EF Core role-scoped filter pipeline: ProtonProgress accepts bagian/unit/trackType/tahun/coacheeId params, composes Where clauses before materialization, with TrackingItem extended for CoacheeId/CoacheeName multi-coachee identification**

## Performance

- **Duration:** 2 min
- **Started:** 2026-02-27T04:37:10Z
- **Completed:** 2026-02-27T04:39:04Z
- **Tasks:** 1
- **Files modified:** 2

## Accomplishments

- Extended TrackingItem with CoacheeId and CoacheeName fields so multi-coachee result sets identify which row belongs to whom
- Replaced single-coachee ProtonProgress action with a 5-param version that composes EF Where clauses (bagian, unit, trackType, tahun, coacheeId) before ToListAsync — no in-memory post-filtering
- Implemented server-enforced role scoping: Level 1-2 see all, Level 4 scoped to section, Level 5 via CoachCoacheeMapping, Level 6 own data only — URL params can narrow but never expand scope
- Added ProtonTrack ThenInclude to deliverable query chain, enabling Track/Tahun Where clauses to resolve via navigation properties
- Populated ViewBag with all filter option lists (AllBagian, AllUnits, AllTracks, AllTahun, Coachees) and Selected* echoes for Plan 02 view to render dropdown selected state

## Task Commits

1. **Task 1: Extend TrackingItem model and refactor ProtonProgress action** - `50fc0bc` (feat)

**Plan metadata:** (pending final commit)

## Files Created/Modified

- `Models/TrackingModels.cs` - Added CoacheeId and CoacheeName string properties to TrackingItem
- `Controllers/CDPController.cs` - Refactored ProtonProgress: new 5-param signature, role-scoped coachee derivation, EF filter composition, ViewBag population

## Decisions Made

- ProtonTrack ThenInclude added to deliverable query chain: Phase 63 did not include it; this is required for Track/Tahun EF Where clauses to traverse the navigation property chain
- dataCoacheeIds defaults to all scopedCoacheeIds (not just one) so data loads without selecting a specific coachee — multi-coachee aggregate view is the default
- Coach Track cascade narrows scopedCoacheeIds before coacheeList population so the coachee dropdown in the view only shows coachees on the filtered track
- TrackLabel computed only for single-coachee selection; empty string for multi-coachee aggregate
- NoTrackMessage / NoProgressMessage (single-coachee specific) removed; replaced with generic ViewBag.EmptyMessage for the filter-empty state

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- All ViewBag values (AllBagian, AllUnits, AllTracks, AllTahun, Coachees, Selected*) are ready for Plan 02 to render filter dropdowns in the view
- TotalCount and FilteredCount available for "Menampilkan X dari Y data" display
- TrackingItem.CoacheeId and CoacheeName ready for Plan 02 to show per-coachee identification in table rows
- Build passes with 0 errors

## Self-Check: PASSED

- FOUND: Models/TrackingModels.cs
- FOUND: Controllers/CDPController.cs
- FOUND: .planning/phases/64-functional-filters/64-01-SUMMARY.md
- FOUND commit: 50fc0bc (feat(64-01): extend TrackingItem + refactor ProtonProgress with filter params)

---
*Phase: 64-functional-filters*
*Completed: 2026-02-27*
