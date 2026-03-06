---
phase: 83-master-data-qa
plan: "09"
subsystem: ui
tags: [soft-delete, silabus, protondata, bootstrap, jquery, ajax]

# Dependency graph
requires:
  - phase: 83-07
    provides: SilabusDeactivate/SilabusReactivate POST endpoints, showInactive filter in ProtonDataController, IsActive column on ProtonKompetensi
  - phase: 83-08
    provides: ManageWorkers soft delete UI pattern (toggle anchor, fetch-based deactivate/reactivate, table-secondary row styling)
provides:
  - ProtonData/Index.cshtml Tampilkan Inactive toggle (anchor-link GET pattern)
  - Nonaktifkan button per saved active silabus row (fetch POST SilabusDeactivate)
  - Aktifkan Kembali button per inactive silabus row (fetch POST SilabusReactivate)
  - Inactive row greying with table-secondary + text-muted CSS classes
  - Browser-verified sign-off on all 7 DATA requirements (DATA-01 to DATA-07)
affects: [84-assessment-flow-qa, 85-coaching-proton-qa]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Soft-delete UI: toggle via anchor GET (?showInactive=true), deactivate/reactivate via fetch POST with antiforgery token, toast feedback + window.location.reload()"
    - "Conditional row action buttons: IsNew rows use hard-delete; saved active rows use Nonaktifkan; saved inactive rows use Aktifkan Kembali"
    - "Inactive row styling: table-secondary + text-muted class on <tr> when IsActive === false"

key-files:
  created: []
  modified:
    - Views/ProtonData/Index.cshtml

key-decisions:
  - "Silabus soft delete UI mirrors ManageWorkers pattern from 83-08 — anchor-link toggle, fetch-based action buttons, table-secondary row greying"
  - "IsNew rows retain hard-delete (btn-delete-row) to allow removing unsaved rows from the editor without a POST; only saved rows get soft delete controls"
  - "All 7 DATA requirements (DATA-01 to DATA-07) verified in browser by user — Phase 83 gap closure confirmed complete"

patterns-established:
  - "Soft-delete toggle pattern: anchor-link GET with showInactive query param; no form post needed"
  - "Fetch-based soft delete: POST JSON body { KompetensiId } with X-Antiforgery-Token header, toast on success, reload after 800ms"

requirements-completed: [DATA-03, DATA-05, DATA-06, DATA-07, DATA-01, DATA-02, DATA-04]

# Metrics
duration: ~30min (including checkpoint wait)
completed: 2026-03-03
---

# Phase 83 Plan 09: Silabus Soft Delete UI + Phase QA Verification Summary

**ProtonData/Index silabus editor gains Tampilkan Inactive toggle, per-row Nonaktifkan/Aktifkan Kembali buttons with inactive row greying; all 7 DATA requirements verified in browser confirming Phase 83 gap closure complete**

## Performance

- **Duration:** ~30 min (including browser checkpoint wait)
- **Started:** 2026-03-03
- **Completed:** 2026-03-03
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments

- Added Tampilkan Inactive / Sembunyikan Inactive toggle to ProtonData/Index silabus editor using anchor-link GET pattern
- Added conditional per-row action buttons: Nonaktifkan (active saved rows) and Aktifkan Kembali (inactive rows), replacing hard-delete for saved rows
- Inactive silabus rows render with table-secondary + text-muted styling to visually distinguish them
- Browser-verified all 5 flows: Worker soft delete, login block for inactive users, Export Status column, ImportWorkers PerluReview/Reaktivasi, and Silabus soft delete — all passing
- All 7 DATA requirements (DATA-01 through DATA-07) confirmed satisfied; Phase 83 master data QA gap closure complete

## Task Commits

Each task was committed atomically:

1. **Task 1: Update ProtonData/Index.cshtml with Tampilkan Inactive toggle and soft delete buttons** - `16cd54e` (feat)
2. **Task 2: Browser verify all Phase 83 gap closure flows** - checkpoint approved by user (no code commit)

## Files Created/Modified

- `Views/ProtonData/Index.cshtml` — Added Tampilkan Inactive toggle (Razor anchor link), updated JS silabus row renderer to check `row.IsActive` and emit conditional Nonaktifkan/Aktifkan Kembali buttons, added JS event handlers for both fetch POST actions, inactive row greying

## Decisions Made

- Silabus soft delete UI mirrors ManageWorkers pattern from 83-08: anchor-link toggle, fetch POST buttons, table-secondary row greying — same pattern reduces learning curve and maintenance
- IsNew (unsaved) rows keep hard-delete btn-delete-row since they have no DB record to deactivate; only rows with a real Id get soft delete controls
- All 7 DATA requirements verified in browser by user approval — no automated test harness needed for this QA phase

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 83 master data QA is complete. All DATA-01 through DATA-07 requirements satisfied.
- Soft delete infrastructure (IsActive on ApplicationUser + ProtonKompetensi, backend endpoints, UI controls) is fully operational.
- Phase 84 (Assessment Flow QA) can proceed — no blockers from Phase 83.
- Phase 85 (Coaching Proton Flow QA) can proceed — CDPController already filters inactive silabus (from 83-07).

---
*Phase: 83-master-data-qa*
*Completed: 2026-03-03*
