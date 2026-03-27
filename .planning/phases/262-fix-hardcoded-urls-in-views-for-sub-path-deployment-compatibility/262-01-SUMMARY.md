---
phase: 262-fix-hardcoded-urls-in-views-for-sub-path-deployment-compatibility
plan: 01
subsystem: infra
tags: [pathbase, middleware, asp-net-core, sub-path-deployment]

requires:
  - phase: 261
    provides: validated org data
provides:
  - UsePathBase middleware aktif dari appsettings.json config
  - Global basePath JS variable dan appUrl() helper di semua halaman
affects: [262-02, 262-03]

tech-stack:
  added: []
  patterns: [UsePathBase middleware, basePath/appUrl JS globals]

key-files:
  created: []
  modified: [Program.cs, appsettings.json, Views/Shared/_Layout.cshtml]

key-decisions:
  - "PathBase di appsettings.json agar mudah diubah tanpa recompile"
  - "basePath variable dari Url.Content(~/) dengan trim trailing slash untuk konsisten concatenation"

patterns-established:
  - "appUrl(path): JS helper untuk URL building yang PathBase-aware"
  - "basePath global variable tersedia di semua halaman via _Layout.cshtml"

requirements-completed: [D-01, D-02, D-04]

duration: 2min
completed: 2026-03-27
---

# Phase 262 Plan 01: Setup UsePathBase + basePath/appUrl Summary

**UsePathBase middleware dari appsettings.json config + global basePath variable dan appUrl() JS helper di _Layout.cshtml**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-27T02:15:38Z
- **Completed:** 2026-03-27T02:17:40Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- UsePathBase middleware aktif di Program.cs, membaca PathBase dari appsettings.json
- Global `basePath` JS variable dan `appUrl()` helper function tersedia di semua halaman
- Foundation siap untuk Plan 02 dan 03 yang akan fix hardcoded URLs

## Task Commits

1. **Task 1: Tambah PathBase config dan UsePathBase middleware** - `0b47fddb` (feat)
2. **Task 2: Inject global basePath dan appUrl helper di _Layout.cshtml** - `a6eb2c1e` (feat)

## Files Created/Modified
- `appsettings.json` - Tambah key PathBase: "/KPB-PortalHC"
- `Program.cs` - UsePathBase middleware sebelum UseStaticFiles
- `Views/Shared/_Layout.cshtml` - basePath variable dan appUrl() function di head

## Decisions Made
- PathBase disimpan di appsettings.json (bukan hardcode) agar mudah diubah per environment
- basePath dari Url.Content("~/") dengan trim trailing slash untuk mencegah double-slash

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Build error MSB3021/MSB3027 karena HcPortal.exe sedang running (file lock) - bukan compilation error, kode valid

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Foundation PathBase siap, Plan 02 bisa mulai fix hardcoded URLs di 12 high-volume files
- basePath dan appUrl() tersedia untuk digunakan di JavaScript

---
*Phase: 262-fix-hardcoded-urls-in-views-for-sub-path-deployment-compatibility*
*Completed: 2026-03-27*
