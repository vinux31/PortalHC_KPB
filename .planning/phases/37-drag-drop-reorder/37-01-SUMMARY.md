---
phase: 37-drag-drop-reorder
plan: "01"
subsystem: api
tags: [aspnet, csharp, ajax, reorder]

# Dependency graph
requires:
  - phase: 36-delete-guards
    provides: ProtonCatalogController with DeleteCatalogItem — confirms AJAX JSON contract and DbSet names

provides:
  - ReorderKompetensi POST action on ProtonCatalogController
  - ReorderSubKompetensi POST action on ProtonCatalogController
  - ReorderDeliverable POST action on ProtonCatalogController

affects: [37-02-frontend — will call /ProtonCatalog/Reorder* endpoints on each SortableJS drop event]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Reorder pattern: load items by orderedIds.Contains, loop with i+1 Urutan assignment, single SaveChangesAsync"

key-files:
  created: []
  modified:
    - Controllers/ProtonCatalogController.cs

key-decisions:
  - "[37-01]: Reorder endpoints follow identical AJAX JSON contract as existing actions — RoleLevel > 2 returns Json({success:false,error:'Unauthorized'}), not Forbid()"
  - "[37-01]: Single SaveChangesAsync at end of Urutan reassignment loop — EF Core batches all UPDATE statements in one round-trip"

patterns-established:
  - "Reorder POST: orderedIds int[] param → Where(Contains) → for loop i+1 → SaveChangesAsync → Json({success:true})"

# Metrics
duration: 1min
completed: 2026-02-24
---

# Phase 37 Plan 01: Reorder Backend Endpoints Summary

**Three POST actions added to ProtonCatalogController that accept an ordered int[] and reassign Urutan 1..N for Kompetensi, SubKompetensi, and Deliverable — ready for SortableJS frontend to call**

## Performance

- **Duration:** 1 min
- **Started:** 2026-02-24T03:00:29Z
- **Completed:** 2026-02-24T03:01:45Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments

- ReorderKompetensi POST action: auth guard + input guard + Urutan i+1 loop over ProtonKompetensiList + SaveChangesAsync
- ReorderSubKompetensi POST action: same pattern over ProtonSubKompetensiList
- ReorderDeliverable POST action: same pattern over ProtonDeliverableList
- All three return `Json({success:true})` on success and `Json({success:false, error:string})` on guard failures
- `dotnet build` passes with 0 errors

## Task Commits

Each task was committed atomically:

1. **Task 1: Add ReorderKompetensi, ReorderSubKompetensi, ReorderDeliverable to ProtonCatalogController** - `1bd199e` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified

- `Controllers/ProtonCatalogController.cs` - Added 78 lines: three Reorder POST actions appended after DeleteCatalogItem

## Decisions Made

- Reorder endpoints follow the same AJAX JSON contract as all other actions in this controller — RoleLevel > 2 guard returns `Json({success:false,error:"Unauthorized"})` not `Forbid()`, preserving the JSON shape the frontend expects
- Single `SaveChangesAsync` after the full Urutan reassignment loop — EF Core batches all UPDATEs in one round-trip

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- All three Reorder endpoints are live: `/ProtonCatalog/ReorderKompetensi`, `/ProtonCatalog/ReorderSubKompetensi`, `/ProtonCatalog/ReorderDeliverable`
- Phase 37-02 (frontend) can now add SortableJS drag handles and wire `onEnd` callbacks to these endpoints
- No blockers

---
*Phase: 37-drag-drop-reorder*
*Completed: 2026-02-24*

## Self-Check: PASSED

- Controllers/ProtonCatalogController.cs: FOUND
- .planning/phases/37-drag-drop-reorder/37-01-SUMMARY.md: FOUND
- commit 1bd199e: FOUND
