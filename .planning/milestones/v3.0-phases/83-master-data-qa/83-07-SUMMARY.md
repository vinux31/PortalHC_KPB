---
phase: 83-master-data-qa
plan: 07
subsystem: api
tags: [silabus, soft-delete, proton-kompetensi, cdp-controller, active-filter]

# Dependency graph
requires:
  - phase: 83-05
    provides: IsActive property on ProtonKompetensi model and migration applied
provides:
  - SilabusDeactivate POST action (sets ProtonKompetensi.IsActive=false)
  - SilabusReactivate POST action (sets ProtonKompetensi.IsActive=true)
  - showInactive filter on ProtonData Index GET (default hides inactive)
  - CDPController ProtonKompetensiList query filtered by IsActive=true
affects: [83-08, 83-09, coaching-proton, plan-idp]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Soft-delete via IsActive flag with deactivate/reactivate POST pair"
    - "showInactive query parameter to toggle visibility of soft-deleted items in admin view"
    - "SilabusKompetensiRequest DTO separate from SilabusDeleteRequest (different target: Kompetensi vs Deliverable)"

key-files:
  created: []
  modified:
    - Controllers/ProtonDataController.cs
    - Controllers/CDPController.cs

key-decisions:
  - "SilabusKompetensiRequest created as separate class (not SilabusDeleteRequest) because existing SilabusDeleteRequest targets DeliverableId not KompetensiId"
  - "Deactivate/Reactivate actions include AuditLog entries for traceability"
  - "CDPController has exactly one direct _context.ProtonKompetensiList query (line 65) — all others navigate through deliverable progress relations and don't need IsActive filter"

patterns-established:
  - "Soft-delete pattern for silabus: SilabusDeactivate sets IsActive=false, SilabusReactivate sets IsActive=true"
  - "Admin silabus GET: showInactive=false (default) hides inactive; showInactive=true shows all for admin management"

requirements-completed: [DATA-03]

# Metrics
duration: 6min
completed: 2026-03-03
---

# Phase 83 Plan 07: Silabus Soft-Delete Backend Summary

**SilabusDeactivate/Reactivate POST actions with audit log added to ProtonDataController; ProtonData Index GET gains showInactive filter; CDPController ProtonKompetensiList query filtered by IsActive=true so inactive silabus are hidden from coachee views.**

## Performance

- **Duration:** 6 min
- **Started:** 2026-03-03T07:41:21Z
- **Completed:** 2026-03-03T07:47:37Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- SilabusDeactivate and SilabusReactivate POST actions in ProtonDataController with AuditLog entries
- showInactive query parameter on ProtonData Index GET filters inactive kompetensi from SilabusRowsJson by default
- ViewBag.ShowInactive set for view consumption in Plan 83-09
- CDPController ProtonKompetensiList query (coachee coaching view) now filters by k.IsActive — inactive silabus invisible to coachees

## Task Commits

Each task was committed atomically:

1. **Task 1: SilabusDeactivate/Reactivate + showInactive filter in ProtonDataController** - `c7f621d` (feat)
2. **Task 2: CDPController IsActive filter on ProtonKompetensiList query** - `a6794e7` (feat)

**Plan metadata:** (final commit hash — see below)

## Files Created/Modified

- `Controllers/ProtonDataController.cs` - Added SilabusKompetensiRequest class, SilabusDeactivate POST, SilabusReactivate POST; updated Index to accept showInactive param and filter accordingly
- `Controllers/CDPController.cs` - Added `&& k.IsActive` to the ProtonKompetensiList Where clause in coachee coaching view action

## Decisions Made

- Used a separate `SilabusKompetensiRequest` class rather than reusing `SilabusDeleteRequest` because the existing class targets `DeliverableId` (for row-level delete) while deactivate/reactivate operate on `KompetensiId` (kompetensi-level soft delete).
- Audit log entries added to deactivate/reactivate for traceability — consistent with other admin actions in the controller.
- Only the direct `_context.ProtonKompetensiList` query in CDPController needed the IsActive filter. All other ProtonKompetensi references in CDPController navigate via deliverable progress navigation properties (ThenInclude chains) and do not select from ProtonKompetensiList directly.

## Deviations from Plan

None - plan executed exactly as written. The `SilabusKompetensiRequest` class name differs from the plan's suggested `SilabusDeleteRequest` reuse, but this was required because the existing `SilabusDeleteRequest` has `DeliverableId` not `KompetensiId` — handled inline without scope creep.

## Issues Encountered

Build showed MSBuild file-lock warnings (running app had exe locked) but no CS compiler errors. All code compiled correctly.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Silabus soft-delete backend complete
- Plan 83-08 can now add IsActive filter to ApplicationUser admin queries (user soft-delete backend)
- Plan 83-09 can wire the ProtonData silabus view to call SilabusDeactivate instead of SilabusDelete, and display showInactive toggle
- CDPController already filters inactive silabus — coachee coaching pages are safe

---
*Phase: 83-master-data-qa*
*Completed: 2026-03-03*
