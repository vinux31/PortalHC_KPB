---
phase: 70-kelola-data-hub-reorganization
plan: 01
subsystem: ui
tags: [razor, aspnetcore, admin-hub, navigation, rbac]

# Dependency graph
requires:
  - phase: 69-manageworkers-migration-to-admin
    provides: Manajemen Pekerja card already in Admin/Index Section A; AdminController [Authorize] pattern for HC role
  - phase: 51-proton-silabus-coaching-guidance-manager
    provides: ProtonDataController and ProtonData/Index endpoint that Section A Silabus card links to
  - phase: 52-deliverableprogress-override
    provides: ProtonData/OverrideList endpoint that Deliverable Progress Override card activates
provides:
  - Admin/Index.cshtml restructured into 3 domain sections (A: Data Management, B: Proton, C: Assessment & Training)
  - 4 stale Kelengkapan CRUD placeholder cards removed from hub
  - Deliverable Progress Override card activated (no opacity-75, href=/ProtonData/OverrideList, no Segera badge)
  - Manage Assessments moved from Section A to Section C
  - HC role added to navbar Kelola Data visibility condition
affects: [phase-71, phase-72, phase-73, all-admin-hub-users]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Admin/Index.cshtml uses section-header badge pattern (bg-primary/warning/success) for domain grouping"
    - "Navbar role condition uses || operator for multi-role visibility: userRole == 'Admin' || userRole == 'HC'"

key-files:
  created: []
  modified:
    - Views/Admin/Index.cshtml
    - Views/Shared/_Layout.cshtml

key-decisions:
  - "[Phase 70-01]: HC role added to navbar Kelola Data condition using || — ASP.NET Core [Authorize] ANDs attributes but Razor @if conditions use normal boolean logic"
  - "[Phase 70-01]: Coaching Session Override and Final Assessment Manager kept as opacity-75 placeholder cards (Segera badge) — honest representation of upcoming features"
  - "[Phase 70-01]: Deliverable Progress Override moved from placeholder to active card — Phase 52 built the endpoint; hub now reflects reality"

patterns-established:
  - "Hub section header uses badge + h5 heading pattern; badge color encodes domain (primary=data, warning=proton, success=assessment)"
  - "Placeholder cards use opacity-75 + badge bg-secondary Segera to distinguish upcoming from active features"

requirements-completed: [USR-04]

# Metrics
duration: 15min
completed: 2026-02-28
---

# Phase 70 Plan 01: Kelola Data Hub Reorganization Summary

**Admin/Index.cshtml rewritten into 3 domain sections (Data Management, Proton, Assessment & Training) with 4 stale CRUD placeholders removed and HC navbar access enabled**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-02-28T05:40:00Z
- **Completed:** 2026-02-28T05:55:44Z
- **Tasks:** 2 (1 auto + 1 checkpoint)
- **Files modified:** 2

## Accomplishments
- Admin/Index.cshtml fully rewritten: 3 labelled domain sections with 4+3+2 card layout replacing the old 5+4+4 layout
- 4 stale "Kelengkapan CRUD" placeholder cards removed (Question Bank Edit, Package Question Edit/Delete, ProtonTrack Edit/Delete, Password Reset)
- Deliverable Progress Override card activated: opacity-75 removed, Segera badge removed, href set to /ProtonData/OverrideList
- Manage Assessments card moved from Section A to Section C where it belongs
- _Layout.cshtml navbar "Kelola Data" link now visible to HC users (userRole == "Admin" || userRole == "HC")
- Browser UAT confirmed: Admin and HC users see correct hub; ProtonData/OverrideList endpoint returns valid JSON (empty data expected)

## Task Commits

Each task was committed atomically:

1. **Task 1: Rewrite Admin/Index.cshtml and update navbar HC visibility** - `22ca2b2` (feat)
2. **Task 2: Browser verify restructured hub and HC navbar access** - checkpoint approved by user (no code commit)

**Plan metadata:** TBD (docs commit)

## Files Created/Modified
- `Views/Admin/Index.cshtml` - Fully rewritten: 3 domain sections (A=4 cards, B=3 cards, C=2 cards), stale Section C removed
- `Views/Shared/_Layout.cshtml` - Navbar Kelola Data condition extended from Admin-only to Admin || HC

## Decisions Made
- HC role added to navbar condition using `||` operator in Razor `@if` — this is view logic, not ASP.NET Core attribute logic, so there's no AND-ing issue
- Placeholder cards (Coaching Session Override, Final Assessment Manager) kept with opacity-75 and Segera badge to signal upcoming features honestly
- Deliverable Progress Override activated because Phase 52 already built the endpoint — the card was the only thing blocking HC/Admin from finding the feature

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None. User confirmed during checkpoint review that ProtonData/OverrideList returning `{"success":true,"coachees":[],"deliverableHeaders":[]}` is expected behavior (API works, empty data because no coach-coachee mappings have been overridden yet).

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness
- Kelola Data hub reorganization complete; USR-04 satisfied
- HC users now have full Kelola Data hub access via navbar
- Phases 71-73 (LDAP Auth) can proceed independently; no hub changes needed
- v2.3 remaining phases 53, 54, 60, 61 can use the updated hub as their navigation target

## Self-Check: PASSED

- Views/Admin/Index.cshtml: FOUND
- Views/Shared/_Layout.cshtml: FOUND
- 70-01-SUMMARY.md: FOUND
- Commit 22ca2b2: FOUND

---
*Phase: 70-kelola-data-hub-reorganization*
*Completed: 2026-02-28*
