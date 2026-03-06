---
phase: 89-planidp-silabus-and-coaching-guidance-tabs-improvement
plan: 01
subsystem: ui
tags: [cdp, planidp, silabus, coaching-guidance, proton, viewbag, json]

# Dependency graph
requires:
  - phase: 83-master-data-qa
    provides: IsActive flag on ProtonKompetensi, soft-delete silabus pattern
  - phase: 83-master-data-qa
    provides: CoachingGuidanceFile model with Bagian/Unit/ProtonTrackId
provides:
  - CDPController.PlanIdp unified action serving all roles with ViewBag data for new 2-tab view
  - CDPController.GuidanceDownload action for any authenticated user to download guidance files
  - ViewBag.SilabusRowsJson: flat JSON array of Kompetensi/SubKompetensi/Deliverable rows
  - ViewBag.GuidanceGroupedJson: 4-level hierarchy (Bagian > Unit > TrackType > TahunKe) of guidance files
  - Coachee auto-filter: pre-fills bagian/unit/trackId from ProtonTrackAssignment via first active ProtonKompetensi
affects:
  - 89-02 (PlanIdp view rewrite which consumes all these ViewBag values)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Unified ViewBag data preparation pattern: controller populates JSON ViewBag values, view renders from them
    - Coachee auto-filter via ProtonTrackAssignment → first ProtonKompetensi for Bagian/Unit/TrackId derivation
    - 4-level LINQ group-by hierarchy for accordion data structure

key-files:
  created: []
  modified:
    - Controllers/CDPController.cs

key-decisions:
  - "[89-01]: PlanIdp unified action: removed old Coachee/PDF dual-path, single method serves all roles via ViewBag JSON"
  - "[89-01]: Coachee bagian/unit derived from first active ProtonKompetensi for assigned trackId — ProtonTrackAssignment does not store Bagian/Unit directly"
  - "[89-01]: GuidanceDownload added to CDPController (not ProtonDataController) with class-level [Authorize] only — accessible to all authenticated users"
  - "[89-01]: QuestPDF using directives retained — used by Dashboard PDF export action elsewhere in CDPController"
  - "[89-01]: guidanceGrouped loads ALL files grouped hierarchically (no filter) — accordion expands all data, no filter needed on Guidance tab"

patterns-established:
  - "ViewBag JSON pattern: controller serializes complex data to JSON strings, view renders from them via JS"
  - "Coachee pre-filter: null-coalesce assignment after ProtonKompetensi query so query string params always win over auto-fill"

requirements-completed: [PLANIDP-01, PLANIDP-02, PLANIDP-03, PLANIDP-04, PLANIDP-05]

# Metrics
duration: 15min
completed: 2026-03-03
---

# Phase 89 Plan 01: CDPController PlanIdp Rewrite Summary

**Unified CDPController.PlanIdp action replacing old Coachee-Proton/Admin-PDF dual-path with single method that populates SilabusRowsJson and 4-level GuidanceGroupedJson ViewBag for all roles**

## Performance

- **Duration:** 15 min
- **Started:** 2026-03-03T09:23:19Z
- **Completed:** 2026-03-03T09:38:00Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- Replaced old PlanIdp action (Coachee Proton deliverable path + Admin/HC PDF path) with single unified async action
- New signature `PlanIdp(string? bagian, string? unit, int? trackId)` — trackId is int? replacing old string? level
- Coachee auto-filter: queries ProtonTrackAssignment then first active ProtonKompetensi to derive bagian/unit/trackId
- Builds silabusRows flat JSON array (Kompetensi/SubKompetensi/Deliverable) filtered by all 3 params when present
- Builds guidanceGrouped 4-level hierarchy (Bagian > Unit > TrackType > TahunKe) from all CoachingGuidanceFiles
- Added GuidanceDownload action (class-level [Authorize] only) with content-type switch for common document types

## Task Commits

Each task was committed atomically:

1. **Task 1: Rewrite CDPController.PlanIdp and add GuidanceDownload** - `1117107` (feat)

**Plan metadata:** (docs commit below)

## Files Created/Modified
- `Controllers/CDPController.cs` - Replaced PlanIdp (lines 35-119 old) with unified 140-line action + GuidanceDownload action

## Decisions Made
- QuestPDF using directives kept: used by ExportProtonProgress action around line 1823 — verified before deciding
- guidanceGrouped is loaded for ALL files (not filtered by current bagian/unit/trackId) because the Coaching Guidance tab is an accordion tree showing all data; filter is done via expand/collapse UX, not by query
- Coachee auto-filter uses null-coalesce (`??=`) so explicit query string params always override the assignment-derived defaults
- GuidanceDownload placed in CDPController (not ProtonDataController) so the URL is `/CDP/GuidanceDownload` which is consistent with the PlanIdp view's context

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- CDPController.PlanIdp is ready for the new Views/CDP/PlanIdp.cshtml view rewrite (Plan 89-02)
- All ViewBag keys that 89-02 needs are populated: SilabusRowsJson, GuidanceGroupedJson, OrgStructureJson, AllTracks, IsCoachee, HasFilter, HasAssignment, AssignedTrackId, Bagian, Unit, TrackId, UserRole
- GuidanceDownload endpoint at `/CDP/GuidanceDownload?id=X` available for download buttons in new view

---
*Phase: 89-planidp-silabus-and-coaching-guidance-tabs-improvement*
*Completed: 2026-03-03*
