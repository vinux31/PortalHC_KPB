---
phase: 92-admin-cpdp-file-management
plan: 02
subsystem: AdminController / CPDP File Management Views
tags: [cpdp, file-management, admin, razor-views, soft-delete]

requires:
  - phase: 92-01
    provides: [CpdpFiles GET, CpdpUpload GET/POST, CpdpFileDownload, CpdpFileArchive controller actions]
provides:
  - CpdpFiles.cshtml tabbed file management page
  - CpdpUpload.cshtml drag-drop upload form
  - CpdpFileHistory.cshtml archived file listing
  - Admin/Index hub card for CPDP File Management (Admin/HC only)
  - CpdpFileHistory GET controller action
  - KkjBagianDelete guards CpdpFiles (dual-check)
affects: [phase 93 (worker view), KKJ Matrix pages (shared bagian endpoints)]

tech-stack:
  added: []
  patterns: [KkjMatrix view mirror pattern, shared KkjBagian endpoints for CPDP]

key-files:
  created:
    - Views/Admin/CpdpFiles.cshtml
    - Views/Admin/CpdpUpload.cshtml
    - Views/Admin/CpdpFileHistory.cshtml
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/Index.cshtml

key-decisions:
  - "CpdpFiles.cshtml mirrors KkjMatrix.cshtml exactly — same Bootstrap structure, same tab/pane pattern, CPDP-specific action names"
  - "KkjBagianDelete now checks both KkjFiles and CpdpFiles counts — deletion blocked if either has files, with per-type counts in message"
  - "Bagian add/delete still uses shared KkjBagianAdd/KkjBagianDelete endpoints from CpdpFiles page"

patterns-established:
  - "CPDP view pattern: mirror KkjMatrix/KkjUpload/KkjFileHistory with substituted model types and action names"
  - "Dual-guard pattern: KkjBagianDelete checks all tables that reference KkjBagian before deletion"

requirements-completed: [CPDP-03]

duration: 5min
completed: "2026-03-03"
---

# Phase 92 Plan 02: CPDP File Management Views Summary

**Three Razor views (CpdpFiles, CpdpUpload, CpdpFileHistory) plus hub card wiring the full CPDP file lifecycle for Admin/HC roles.**

## Performance

- **Duration:** 5 min
- **Started:** 2026-03-03T02:56:03Z
- **Completed:** 2026-03-03T03:01:00Z
- **Tasks:** 2 (checkpoint at Task 3)
- **Files modified:** 5

## Accomplishments
- Created CpdpFiles.cshtml: tabbed Bootstrap page with per-bagian file lists, archive (soft-delete) AJAX, upload/history links, and shared bagian add/delete controls
- Created CpdpUpload.cshtml: drag-drop file upload form mirroring KkjUpload, CPDP-specific action and breadcrumbs
- Created CpdpFileHistory.cshtml: archived CPDP file listing with Download buttons, back-link to CpdpFiles with bagian param
- Added CPDP File Management hub card to Admin/Index.cshtml inside the Admin/HC conditional block
- Added CpdpFileHistory GET controller action to AdminController.cs
- Updated KkjBagianDelete to guard against both KkjFiles and CpdpFiles (dual-count check)

## Task Commits

Each task was committed atomically:

1. **Task 1: Add CpdpFileHistory action and update KkjBagianDelete** - `372ab22` (feat)
2. **Task 2: Create CPDP views and hub card** - `9b35b05` (feat)

## Files Created/Modified
- `Controllers/AdminController.cs` - CpdpFileHistory action added; KkjBagianDelete dual-guard updated
- `Views/Admin/CpdpFiles.cshtml` - Tabbed CPDP file management page (mirrors KkjMatrix.cshtml)
- `Views/Admin/CpdpUpload.cshtml` - Drag-drop upload form for CPDP files (mirrors KkjUpload.cshtml)
- `Views/Admin/CpdpFileHistory.cshtml` - Archived CPDP files listing (mirrors KkjFileHistory.cshtml)
- `Views/Admin/Index.cshtml` - Added CPDP File Management hub card (Admin/HC only)

## Decisions Made
- KkjBagianDelete dual-guard: message now shows "(KKJ: X, CPDP: Y)" breakdown so admin knows which table has files
- CPDP hub card placed between KkjMatrix card and KKJ-IDP Mapping card in the same @if (Admin/HC) block

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Full CPDP file management UI is ready for browser verification (checkpoint Task 3)
- After checkpoint approval, Phase 93 can proceed with worker view rewrite and CpdpItems cleanup

---
*Phase: 92-admin-cpdp-file-management*
*Completed: 2026-03-03*
