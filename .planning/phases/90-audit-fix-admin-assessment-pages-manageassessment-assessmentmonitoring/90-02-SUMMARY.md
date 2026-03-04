---
phase: 90-audit-fix-admin-assessment-pages-manageassessment-assessmentmonitoring
plan: 02
subsystem: ui
tags: [razor, views, assessment, admin, bootstrap]

# Dependency graph
requires:
  - phase: 90-01
    provides: context on Admin assessment page role guards and TCP connection fix
provides:
  - ManageAssessment header buttons always in DOM with JS show/hide
  - Monitoring cross-link button from ManageAssessment header
  - InProgress and Abandoned status badge classes in ManageAssessment
  - AssessmentMonitoring group titles link to AssessmentMonitoringDetail
  - AssessmentMonitoringDetail breadcrumb verified correct
  - Interview aspect form names verified match controller parsing
  - CreateAssessment and EditAssessment views audited and correct
affects: [assessment monitoring navigation, tab switching UX]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Always-render-in-DOM pattern: conditional elements use inline style for initial visibility, never conditional rendering that would remove from DOM"

key-files:
  created: []
  modified:
    - Views/Admin/ManageAssessment.cshtml
    - Views/Admin/AssessmentMonitoring.cshtml
    - Views/Admin/AssessmentMonitoringDetail.cshtml
    - Views/Admin/CreateAssessment.cshtml
    - Views/Admin/EditAssessment.cshtml

key-decisions:
  - "[90-02] header-assessment-btns always rendered in DOM; initial visibility via inline style; JS shown.bs.tab handler can always find element"
  - "[90-02] Monitoring cross-link added to ManageAssessment header as btn-outline-success"
  - "[90-02] AssessmentMonitoring title column uses reuses computed detailUrl variable for clickable anchor"

patterns-established:
  - "Always-render pattern: use style=display:none rather than @if conditional to keep elements accessible to JS event handlers"

requirements-completed: []

# Metrics
duration: 10min
completed: 2026-03-04
---

# Phase 90 Plan 02: ManageAssessment & Monitoring View Audit Summary

**Fixed header-assessment-btns conditional rendering bug and added Monitoring cross-link; made AssessmentMonitoring group titles clickable links to detail page**

## Performance

- **Duration:** 10 min
- **Started:** 2026-03-04T00:00:00Z
- **Completed:** 2026-03-04T00:10:21Z
- **Tasks:** 3
- **Files modified:** 5

## Accomplishments
- Fixed ManageAssessment header buttons: removed `@if (activeTab == "assessment")` conditional rendering so `header-assessment-btns` is always in DOM; initial visibility set via inline style
- Added "Monitoring" cross-link button (btn-outline-success) to ManageAssessment header
- Added InProgress (bg-warning text-dark) and Abandoned (bg-dark) to statusBadge switch expression
- AssessmentMonitoring group title cells now render as clickable anchors to AssessmentMonitoringDetail (using pre-computed `detailUrl` variable)
- Verified AssessmentMonitoringDetail breadcrumb and interview form aspect name patterns
- Audited CreateAssessment and EditAssessment: all form fields, back/cancel links, ProtonTrack JS toggle, schedule warning — all correct

## Task Commits

Each task was committed atomically:

1. **Task 1: Fix ManageAssessment header buttons and add Monitoring cross-link** - `3aa944f` (fix)
2. **Task 2: Audit AssessmentMonitoring and AssessmentMonitoringDetail views** - `97f5e0e` (fix)
3. **Task 3: Audit CreateAssessment and EditAssessment views** - `4198545` (fix)

## Files Created/Modified
- `Views/Admin/ManageAssessment.cshtml` - Always-render header-assessment-btns; Monitoring cross-link; InProgress/Abandoned badges
- `Views/Admin/AssessmentMonitoring.cshtml` - Group title column wrapped in clickable anchor to detail page
- `Views/Admin/AssessmentMonitoringDetail.cshtml` - Breadcrumb verified with 90-review comment
- `Views/Admin/CreateAssessment.cshtml` - Audit verified with 90-review comment
- `Views/Admin/EditAssessment.cshtml` - Audit verified with 90-review comment

## Decisions Made
- header-assessment-btns always rendered in DOM; initial visibility via `style="@(activeTab != "assessment" ? "display:none" : "")"` so JS `shown.bs.tab` handler can always find it
- AssessmentMonitoring title anchor reuses the pre-computed `detailUrl` variable (defined just above the `<tr>`) — clean and DRY
- Category subtitle added below title link in monitoring table for better context (category badge still shows in its own column)

## Deviations from Plan

None - plan executed exactly as written. All 5 files audited; fixes applied only where issues found.

## Issues Encountered
- Build shows MSB3021/MSB3027 file locking errors (app process holding exe) — not compile errors; all code changes are syntactically valid Razor/C#.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- All Admin assessment view issues from phase 90 plan 02 fixed
- Phase 90 complete: Admin/Index role guards, ManageAssessment/AssessmentMonitoring views all audited and corrected
- Ready for next audit phase (CMP assessment pages if planned)

---
*Phase: 90-audit-fix-admin-assessment-pages-manageassessment-assessmentmonitoring*
*Completed: 2026-03-04*
