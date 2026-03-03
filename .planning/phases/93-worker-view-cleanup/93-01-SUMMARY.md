---
phase: 93-worker-view-cleanup
plan: 01
subsystem: ui
tags: [cpdp, cmp, mapping, tabbed-view, role-based-filtering, file-download]

# Dependency graph
requires:
  - phase: 92-admin-cpdp-file-management
    provides: CpdpFiles model + AdminController.CpdpFileDownload endpoint + KkjBagian shared entity
provides:
  - Read-only tabbed CPDP file-download page for workers at /CMP/Mapping
  - Role-based tab filtering: L5-L6 sees own section only, L1-L4/Admin/HC see all
  - CMPController.Mapping action querying CpdpFiles + KkjBagians
affects: [93-02-cleanup, future-cpdp-features]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Tabbed Bootstrap nav-tabs layout with ViewBag data (no @model) — same pattern as Admin/CpdpFiles"
    - "Role-based tab filtering via RoleLevel >= 5 and user.Section matching KkjBagian.Name"
    - "Safe fallback: show all tabs if user.Section doesn't match any bagian"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/Mapping.cshtml

key-decisions:
  - "Worker Mapping view mirrors Admin CpdpFiles tabbed layout but is read-only — no Upload, Archive, or Bagian management controls"
  - "RoleLevel >= 5 triggers section-specific filtering; fallback to all bagians if Section has no matching KkjBagian"
  - "Download links reuse Admin/CpdpFileDownload endpoint (already [Authorize] without role restriction)"

patterns-established:
  - "Worker read-only views strip admin controls while reusing same ViewBag data structure"

requirements-completed: [CPDP-04, CPDP-05]

# Metrics
duration: 10min
completed: 2026-03-03
---

# Phase 93 Plan 01: Worker View Cleanup Summary

**CMPController.Mapping rewritten to query CpdpFiles + KkjBagians with RoleLevel-based section filtering; Mapping.cshtml rebuilt as read-only tabbed download page mirroring Admin/CpdpFiles layout**

## Performance

- **Duration:** 10 min
- **Started:** 2026-03-03T04:27:48Z
- **Completed:** 2026-03-03T04:38:00Z
- **Tasks:** 2 (of 3 — paused at checkpoint:human-verify)
- **Files modified:** 2

## Accomplishments
- Replaced CpdpItem spreadsheet query with KkjBagians + CpdpFiles (IsArchived==false) query in CMPController.Mapping
- Implemented role-based tab filtering: L5-L6 users see only their own Section tab with safe fallback
- Rewrote Mapping.cshtml from old @model CpdpItem table to tabbed Bootstrap layout with download-only controls
- Added empty-state message "Belum ada dokumen CPDP untuk bagian ini." per empty tab pane
- Breadcrumb: Beranda > CMP > Mapping KKJ-IDP (CPDP)

## Task Commits

Each task was committed atomically:

1. **Task 1: Rewrite CMPController.Mapping action** - `3460a1b` (feat)
2. **Task 2: Rewrite Views/CMP/Mapping.cshtml** - `b4f370c` (feat)

## Files Created/Modified
- `Controllers/CMPController.cs` - Mapping action rewritten: queries KkjBagians + CpdpFiles, applies RoleLevel >= 5 section filter, passes ViewBag.Bagians / ViewBag.FilesByBagian / ViewBag.SelectedBagianId
- `Views/CMP/Mapping.cshtml` - Fully replaced: no @model, uses ViewBag, Bootstrap nav-tabs, download-only file table, empty state, breadcrumb

## Decisions Made
- Followed plan as specified — no architectural decisions required during execution.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Tasks 1 and 2 complete; awaiting human verification at Task 3 (checkpoint:human-verify)
- After verification approved, Phase 93 Plan 02 (CpdpItem cleanup) can proceed
- Build passes with 0 errors; all verification criteria met

---
*Phase: 93-worker-view-cleanup*
*Completed: 2026-03-03*
