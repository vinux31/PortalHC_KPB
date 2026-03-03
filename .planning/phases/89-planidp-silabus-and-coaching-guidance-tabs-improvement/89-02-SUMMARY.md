---
phase: 89-planidp-silabus-and-coaching-guidance-tabs-improvement
plan: 02
subsystem: ui
tags: [cdp, planidp, silabus, coaching-guidance, proton, accordion, viewbag, json, bootstrap-tabs]

# Dependency graph
requires:
  - phase: 89-planidp-silabus-and-coaching-guidance-tabs-improvement
    plan: 01
    provides: CDPController.PlanIdp unified action with all ViewBag keys
provides:
  - Views/CDP/PlanIdp.cshtml complete rewrite as 2-tab layout (Silabus + Coaching Guidance)
  - Silabus tab: cascading filter + read-only rowspan-merge table rendered from SilabusRowsJson
  - Coaching Guidance tab: 4-level accordion rendered from GuidanceGroupedJson
  - Download buttons linking to /CDP/GuidanceDownload endpoint
affects:
  - /CDP/PlanIdp page (all roles see new unified 2-tab view)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - JSON data island pattern: script type=application/json for JS consumption of server-side data
    - Rowspan merge algorithm adapted from ProtonData/Index for read-only silabus display
    - 4-level nested Bootstrap accordion with independent collapse behavior via data-bs-parent
    - Cascading dropdown (Bagian -> Unit) using OrgStructureJson data island

key-files:
  created: []
  modified:
    - Views/CDP/PlanIdp.cshtml

key-decisions:
  - "[89-02]: All JS uses data islands (script type=application/json) — no inline Razor in JS loops for clean separation"
  - "[89-02]: Lihat Semua link for coachee goes to /CDP/PlanIdp without params — coachee gets manual filter mode instead of auto-fill"
  - "[89-02]: No @model directive — view uses ViewBag exclusively as specified in plan"
  - "[89-02]: Accordion data-bs-parent at each nesting level ensures independent collapse behavior per level"

patterns-established:
  - "JSON data island: script[type=application/json] + JSON.parse for ViewBag data access in JS"
  - "Rowspan merge: Kompetensi outer loop, SubKompetensi inner loop, Deliverable innermost — k===i and k===j guards for first-row cells"

requirements-completed: [PLANIDP-01, PLANIDP-02, PLANIDP-03, PLANIDP-04, PLANIDP-05]

# Metrics
duration: 7min
completed: 2026-03-03
---

# Phase 89 Plan 02: PlanIdp View Rewrite Summary

**Complete rewrite of Views/CDP/PlanIdp.cshtml as unified 2-tab Bootstrap layout with Silabus cascading filter + rowspan-merge table and 4-level Coaching Guidance accordion, all rendered from ViewBag JSON data islands**

## Performance

- **Duration:** 7 min
- **Started:** 2026-03-03T10:07:00Z
- **Completed:** 2026-03-03T10:14:00Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- Completely replaced 570-line old dual-path view (Coachee deliverable table + Admin/HC PDF card) with 315-line unified 2-tab layout
- Silabus tab implements cascading Bagian > Unit > Track filter (GET form submission) with rowspan-merge read-only table
- Coaching Guidance tab implements 4-level nested Bootstrap accordion (Bagian > Unit > TrackType > TahunKe) with file Download buttons
- All JS operates on JSON data islands (script[type=application/json]) — zero inline Razor in JavaScript loops
- Coachee no-assignment guard renders alert and returns early
- Coachee Lihat Semua link resets to /CDP/PlanIdp for manual filter mode
- Build verified: 0 errors, 54 warnings (pre-existing, out of scope)

## Task Commits

Each task was committed atomically:

1. **Task 1: Rewrite Views/CDP/PlanIdp.cshtml as unified 2-tab view** - `2485c31` (feat)

**Plan metadata:** (docs commit below)

## Files Created/Modified
- `Views/CDP/PlanIdp.cshtml` - Complete rewrite: 532 lines removed, 315 lines new content

## Decisions Made
- JSON data islands (`script[type=application/json]`) chosen over inline Razor in JS for clean separation of concerns and easier debugging
- Lihat Semua link goes to `/CDP/PlanIdp` without params — coachee enters manual filter mode instead of auto-fill reset
- No `@model` directive as specified — view consumes data exclusively via ViewBag
- Each accordion level has its own `data-bs-parent` pointing to its direct parent accordion ID for independent collapse behavior

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Views/CDP/PlanIdp.cshtml is fully implemented and build-verified
- All 5 PLANIDP requirements (PLANIDP-01 through PLANIDP-05) fulfilled by Plans 89-01 and 89-02 together
- Phase 89 is complete — ready for browser verification

## Self-Check: PASSED

- FOUND: Views/CDP/PlanIdp.cshtml
- FOUND commit: 2485c31 (feat — view rewrite)
- Build: 0 errors, 54 warnings (pre-existing)
- No old references (IsProtonView, ProtonPlanViewModel, PdfFileName, pdf-header): 0 matches
- GuidanceDownload present: 1 match
- Accordion present: 31 matches
- nav-tabs present: 1 match

---
*Phase: 89-planidp-silabus-and-coaching-guidance-tabs-improvement*
*Completed: 2026-03-03*
