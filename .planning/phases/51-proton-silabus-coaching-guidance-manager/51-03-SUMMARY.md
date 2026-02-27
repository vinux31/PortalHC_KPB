---
phase: 51-proton-silabus-coaching-guidance-manager
plan: 03
subsystem: ui
tags: [aspnet, mvc, file-upload, coaching-guidance, proton-silabus, ajax]

# Dependency graph
requires:
  - phase: 51-proton-silabus-coaching-guidance-manager-01
    provides: CoachingGuidanceFile model, EF migration, ProtonDataController scaffold, two-tab view skeleton
  - phase: 51-proton-silabus-coaching-guidance-manager-02
    provides: Silabus CRUD tab fully functional, guidanceTableContainer placeholder div
provides:
  - GuidanceList AJAX endpoint (GET /ProtonData/GuidanceList)
  - GuidanceUpload POST endpoint with extension/size validation
  - GuidanceDownload GET endpoint serving PhysicalFile with original filename
  - GuidanceReplace POST endpoint replacing physical file in-place
  - GuidanceDelete POST endpoint with DB + physical file cleanup
  - Coaching Guidance tab fully functional with AJAX file table, upload form, replace/delete modals
  - ProtonCatalog all actions redirect to /ProtonData
  - wwwroot/uploads/guidance/ upload directory
affects: [proton-data, coaching-guidance, proton-catalog]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "File upload: IFormFile validation (extension whitelist + size limit), safe filename with timestamp+GUID, PhysicalFile download"
    - "Coaching Guidance CRUD: AJAX table refresh after each mutation (no page reload)"
    - "GuidanceDeleteRequest [FromBody] JSON DTO for delete endpoint"
    - "Controller redirect: All ProtonCatalog actions use simple expression-bodied RedirectToAction"

key-files:
  created:
    - wwwroot/uploads/guidance/.gitkeep
  modified:
    - Controllers/ProtonDataController.cs
    - Views/ProtonData/Index.cshtml
    - Controllers/ProtonCatalogController.cs

key-decisions:
  - "GuidanceDelete uses [FromBody] JSON DTO (GuidanceDeleteRequest) to match Plan 02 SilabusDeleteRequest pattern"
  - "Upload file named as {timestamp}_{GUID}{ext} to prevent collisions; original filename preserved in DB for download"
  - "GuidanceReplace deletes old physical file before saving new one; DB record updated in-place (same Id)"
  - "Coaching Guidance tab auto-loads on Unit/Track change; also has explicit Muat Data button for manual trigger"
  - "ProtonCatalogController stripped to redirect-only — no DI constructor needed, no DB dependencies"
  - "escapeJs() escapes backslash, single quote, double quote for inline onclick attribute safety"

patterns-established:
  - "File upload pattern: validate extension from whitelist, check size <= 10MB, save with safe filename, store web-relative path in DB"
  - "AJAX CRUD: loadGuidanceFiles() is the single reload function called after every mutation (upload, replace, delete)"

requirements-completed: [OPER-02]

# Metrics
duration: 3min
completed: 2026-02-27
---

# Phase 51 Plan 03: Proton Silabus & Coaching Guidance Manager Summary

**Coaching Guidance file management (upload/download/replace/delete with AJAX table refresh) + ProtonCatalog URL redirect, backed by 5 controller endpoints with AuditLog integration**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-02-27T06:52:41Z
- **Completed:** 2026-02-27T06:55:04Z
- **Tasks:** 2
- **Files modified:** 3 + 1 created

## Accomplishments
- 5 Coaching Guidance endpoints added to ProtonDataController: GuidanceList (AJAX), GuidanceUpload (multipart/form-data with extension+size validation), GuidanceDownload (PhysicalFile with original filename), GuidanceReplace (in-place file swap), GuidanceDelete ([FromBody] JSON with DB+physical cleanup)
- Coaching Guidance tab in ProtonData/Index.cshtml now fully functional: AJAX file table with Download/Replace/Delete actions, Bootstrap modals for Replace and Delete confirmation, upload form with error feedback
- ProtonCatalogController replaced with redirect-only controller (all 11 actions redirect to /ProtonData/Index)
- wwwroot/uploads/guidance/ directory created with .gitkeep placeholder

## Task Commits

Each task was committed atomically:

1. **Task 1: Add Coaching Guidance endpoints to ProtonDataController and create upload directory** - `127b184` (feat)
2. **Task 2: Implement Coaching Guidance tab JavaScript and redirect ProtonCatalog** - `2988d87` (feat)

**Plan metadata:** (docs commit — see final commit)

## Files Created/Modified
- `Controllers/ProtonDataController.cs` - Added GuidanceDeleteRequest DTO, GuidanceList/Upload/Download/Replace/Delete endpoints, GetContentType/FormatFileSize helpers
- `Views/ProtonData/Index.cshtml` - Added Coaching Guidance IIFE with loadGuidanceFiles, renderGuidanceTable, upload form wiring, Replace/Delete modals and their Bootstrap modal HTML
- `Controllers/ProtonCatalogController.cs` - Replaced all content with redirect-only controller (11 actions → RedirectToAction to ProtonData/Index)
- `wwwroot/uploads/guidance/.gitkeep` - Upload directory placeholder

## Decisions Made
- GuidanceDelete uses `[FromBody] GuidanceDeleteRequest` JSON DTO matching the SilabusDeleteRequest pattern from Plan 02
- Upload files named `{yyyyMMddHHmmss}_{GuidN}{ext}` to prevent filename collisions; original `file.FileName` stored in DB for download
- GuidanceReplace deletes old physical file then saves new one, updating DB record in-place (preserves same Id)
- Coaching Guidance tab auto-reloads on Unit/Track filter change; Muat Data button also triggers load for explicit control
- ProtonCatalogController stripped to redirect-only with no DI constructor — no DB/UserManager dependencies needed
- `escapeJs()` helper escapes backslash + single/double quotes for safe inline `onclick` attribute string literals

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Phase 51 (3/3 plans) complete — Proton Silabus & Coaching Guidance Manager fully shipped
- Coaching Guidance file management ready for use at /ProtonData (Coaching Guidance tab)
- ProtonCatalog bookmarks redirect safely to /ProtonData
- No blockers for next phase

---
*Phase: 51-proton-silabus-coaching-guidance-manager*
*Completed: 2026-02-27*
