---
phase: 262-fix-hardcoded-urls-in-views-for-sub-path-deployment-compatibility
plan: 02
subsystem: views
tags: [pathbase, url-fix, sub-path-deployment, javascript]

requires:
  - phase: 262-01
    provides: basePath/appUrl globals di _Layout.cshtml
provides:
  - 12 view files bebas hardcoded absolute URL
affects: [262-03]

tech-stack:
  added: []
  patterns: [appUrl() for JS fetch/ajax, basePath for JS-built HTML, tag helpers for HTML href]

key-files:
  created: []
  modified:
    - Views/ProtonData/Index.cshtml
    - Views/Admin/CoachCoacheeMapping.cshtml
    - Views/Admin/RenewalCertificate.cshtml
    - Views/CDP/CertificationManagement.cshtml
    - Views/Shared/Components/NotificationBell/Default.cshtml
    - Views/Admin/AssessmentMonitoringDetail.cshtml
    - Views/CDP/Dashboard.cshtml
    - Views/CDP/CoachingProton.cshtml
    - Views/CMP/AnalyticsDashboard.cshtml
    - Views/Home/GuideDetail.cshtml
    - Views/Admin/KkjMatrix.cshtml
    - Views/Admin/CpdpFiles.cshtml

key-decisions:
  - "NotificationBell actionUrl di-prefix basePath karena server menyimpan path tanpa PathBase"
  - "Form action dalam JS string menggunakan basePath concatenation, bukan appUrl"

requirements-completed: [D-03, D-05, D-06]

duration: 5min
completed: 2026-03-27
---

# Phase 262 Plan 02: Fix hardcoded URL di 12 file high-volume Summary

**69 hardcoded URL di 12 view file diganti dengan appUrl(), basePath prefix, atau tag helpers**

## Performance

- **Duration:** 5 min
- **Started:** 2026-03-27T02:18:59Z
- **Completed:** 2026-03-27T02:24:01Z
- **Tasks:** 2
- **Files modified:** 12

## Accomplishments
- 45 URL di 6 file JS-heavy (ProtonData, CoachCoacheeMapping, RenewalCertificate, CertificationManagement, NotificationBell, AssessmentMonitoringDetail) diganti dengan appUrl() atau basePath
- 22 URL di 6 file remaining (Dashboard, CoachingProton, AnalyticsDashboard, GuideDetail, KkjMatrix, CpdpFiles) sudah bersih
- NotificationBell actionUrl di-prefix basePath untuk kompatibilitas sub-path deployment
- AssessmentMonitoringDetail form action dalam JS menggunakan basePath concatenation

## Task Commits

1. **Task 1: Fix hardcoded URL di 6 file JS-heavy** - `1222c16e` (fix)
2. **Task 2: Fix hardcoded URL di 6 file remaining** - `5b516a9a` (fix)

## Files Created/Modified
- `Views/ProtonData/Index.cshtml` - 11 URL: fetch, $.get, href in JS template
- `Views/Admin/CoachCoacheeMapping.cshtml` - 10 URL: fetch + 1 HTML href to tag helper
- `Views/Admin/RenewalCertificate.cshtml` - 9 URL: fetch + window.location.href
- `Views/CDP/CertificationManagement.cshtml` - 6 URL: fetch + window.location.href
- `Views/Shared/Components/NotificationBell/Default.cshtml` - 5 URL: fetch/postWithToken + actionUrl basePath
- `Views/Admin/AssessmentMonitoringDetail.cshtml` - 5 URL: fetch + form action + href in JS
- `Views/CDP/Dashboard.cshtml` - 3 URL: fetch calls
- `Views/CDP/CoachingProton.cshtml` - 3 URL: fetch calls
- `Views/CMP/AnalyticsDashboard.cshtml` - 3 URL: fetch calls
- `Views/Home/GuideDetail.cshtml` - sudah bersih (menggunakan tag helpers)
- `Views/Admin/KkjMatrix.cshtml` - sudah bersih (menggunakan appUrl)
- `Views/Admin/CpdpFiles.cshtml` - sudah bersih (menggunakan appUrl)

## Decisions Made
- NotificationBell: actionUrl dari server di-prefix `basePath` karena database menyimpan path tanpa PathBase
- AssessmentMonitoringDetail: form action dalam JS string menggunakan `basePath + '/Admin/...'` concatenation (bukan appUrl) karena dalam HTML string building
- CoachCoacheeMapping HTML href: diganti ke tag helper `asp-controller`/`asp-action`

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

None.

---
*Phase: 262-fix-hardcoded-urls-in-views-for-sub-path-deployment-compatibility*
*Completed: 2026-03-27*
