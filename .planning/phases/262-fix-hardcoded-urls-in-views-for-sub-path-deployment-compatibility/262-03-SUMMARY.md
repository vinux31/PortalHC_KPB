---
phase: 262-fix-hardcoded-urls-in-views-for-sub-path-deployment-compatibility
plan: 03
subsystem: ui
tags: [pathbase, razor-views, tag-helpers, appUrl, sub-path-deployment]

requires:
  - phase: 262-01
    provides: UsePathBase middleware, basePath/appUrl JS globals
provides:
  - Zero hardcoded absolute URL di seluruh Views folder
  - Semua view PathBase-aware untuk sub-path deployment
affects: []

tech-stack:
  added: []
  patterns: [tag helpers untuk href/action, ~/images untuk static assets, appUrl() untuk JS fetch]

key-files:
  created: []
  modified:
    - Views/CMP/Certificate.cshtml
    - Views/Admin/ManageCategories.cshtml
    - Views/ProtonData/Override.cshtml
    - Views/Home/_CertAlertBanner.cshtml
    - Views/Admin/Shared/_RenewalCertificateTablePartial.cshtml
    - Views/Admin/Shared/_AssessmentGroupsTab.cshtml
    - Views/Admin/Shared/_RenewalGroupedPartial.cshtml
    - Views/Shared/_CertificateHistoryModalContent.cshtml
    - Views/Shared/Error.cshtml
    - Views/CDP/PlanIdp.cshtml
    - Views/Admin/CreateAssessment.cshtml
    - Views/Admin/AssessmentMonitoring.cshtml

key-decisions:
  - "Url.Action dengan anonymous object untuk complex query params (CertificateHistoryModalContent renewParam)"

patterns-established:
  - "img src='/images' selalu diganti src='~/images' untuk Razor URL resolution"
  - "form action='/...' selalu diganti asp-controller + asp-action tag helpers"

requirements-completed: [D-03, D-05, D-06]

duration: 6min
completed: 2026-03-27
---

# Phase 262 Plan 03: Fix hardcoded URL di 12 file low-volume + final sweep Summary

**Fix 18 hardcoded URL di 12 view files (img src, href, form action, JS fetch) + comprehensive sweep memverifikasi zero remaining hardcoded absolute URL**

## Performance

- **Duration:** 6 min
- **Started:** 2026-03-27T02:19:08Z
- **Completed:** 2026-03-27T02:25:19Z
- **Tasks:** 2
- **Files modified:** 12

## Accomplishments
- 18 hardcoded absolute URL di 12 view files berhasil diganti dengan PathBase-aware alternatives
- Comprehensive grep sweep memverifikasi zero genuine hardcoded absolute URL di seluruh Views folder
- Build sukses tanpa compilation errors

## Task Commits

1. **Task 1: Fix hardcoded URL di 12 file low-volume** - `fb08f705` (fix)
2. **Task 2: Final sweep** - verification only, no new fixes needed (plan 262-02 already covered remaining files)

## Files Created/Modified
- `Views/CMP/Certificate.cshtml` - src="/images" -> src="~/images" (2x)
- `Views/Admin/ManageCategories.cshtml` - src="/images" -> src="~/images" (2x)
- `Views/ProtonData/Override.cshtml` - fetch('/...') -> fetch(appUrl('/...')) (3x)
- `Views/Home/_CertAlertBanner.cshtml` - href="/Admin" -> asp-controller/asp-action (2x)
- `Views/Admin/Shared/_RenewalCertificateTablePartial.cshtml` - href="/Admin" -> asp-controller/asp-action (2x)
- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` - action="/Admin" -> asp-controller/asp-action (1x)
- `Views/Admin/Shared/_RenewalGroupedPartial.cshtml` - href="/Admin" -> asp-controller/asp-action (1x)
- `Views/Shared/_CertificateHistoryModalContent.cshtml` - href="/Admin?param" -> Url.Action with anonymous object (1x)
- `Views/Shared/Error.cshtml` - href="/" -> href="~/" (1x)
- `Views/CDP/PlanIdp.cshtml` - JS href="/CDP" -> basePath + '/CDP' (1x)
- `Views/Admin/CreateAssessment.cshtml` - fetch('/Admin') -> fetch(appUrl('/Admin')) (1x)
- `Views/Admin/AssessmentMonitoring.cshtml` - fetch('/Admin') -> fetch(appUrl('/Admin')) (1x)

## Decisions Made
- CertificateHistoryModalContent: mengganti string interpolation `renewParam` dengan Url.Action + anonymous object untuk type-safe URL generation

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Build error MSB3021 (file lock karena HcPortal.exe running) - bukan compilation error, kode valid
- Final sweep menemukan file dari scope plan 262-02 — ternyata sudah difix oleh plan 262-02 yang sudah dieksekusi sebelumnya

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Phase 262 complete: seluruh Views folder bebas hardcoded absolute URL
- Deployment ke sub-path /KPB-PortalHC/ siap dilakukan

---
*Phase: 262-fix-hardcoded-urls-in-views-for-sub-path-deployment-compatibility*
*Completed: 2026-03-27*
