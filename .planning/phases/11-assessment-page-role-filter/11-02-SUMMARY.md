---
phase: 11-assessment-page-role-filter
plan: 02
subsystem: ui
tags: [csharp, aspnetcore, razor, rbac, assessment, bootstrap5, viewbag]

# Dependency graph
requires:
  - phase: 11-assessment-page-role-filter
    plan: 01
    provides: ViewBag.ManagementData (all assessments, paginated) and ViewBag.MonitorData (Open+Upcoming, flat, schedule-asc) set by Assessment() controller action
provides:
  - Assessment.cshtml with role-branched rendering: workers get Training Records callout + Open/Upcoming tabs; HC/Admin get Management + Monitoring tabs
  - Worker Training Records callout linking to /CMP/Records (completed assessments destination)
  - HC/Admin Management tab with CRUD card grid (Edit, Questions, Delete, Regen Token) iterating ViewBag.ManagementData with pagination
  - HC/Admin Monitoring tab with read-only Bootstrap table iterating ViewBag.MonitorData
  - filterCards() JS guarded against missing #assessmentTabs (prevents errors on HC/Admin manage view)
affects:
  - Phase 12 (Dashboard consolidation) — no direct dependency on Assessment view

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Role-branched Razor view: single @if (viewMode == "manage" && canManage) / else splits HC/Admin and worker rendering paths
    - HC/Admin tabs use Bootstrap 5 native data-bs-toggle="tab" without custom JS (no filterCards needed)
    - Worker tabs use custom filterCards() JS guarded with getElementById null check
    - ViewBag cast pattern inside @if block: var x = ViewBag.X as List<T> ?? new List<T>()

key-files:
  created: []
  modified:
    - Views/CMP/Assessment.cshtml

key-decisions:
  - "Tasks 1 and 2 combined into a single atomic commit — splitting would leave an intermediate broken state with neither worker nor HC/Admin view correctly structured"
  - "Worker callout placed in else branch (not manage view) — HC/Admin in personal mode also gets callout, which is correct (they too can check Training Records)"
  - "Completed tab li removed from DOM entirely for worker view, not hidden with CSS — matches plan specification"
  - "filterCards() JS wrapped in workerTabs null check — prevents console errors when HC/Admin is on manage view where #assessmentTabs does not exist"
  - "Razor @{ } inside @if block replaced with bare variable declarations — required by Razor parser (RZ1010 error fix)"

patterns-established:
  - "Null-guard for worker-only JS: var workerTabs = document.getElementById('assessmentTabs'); if (workerTabs) { ... }"
  - "ViewBag cast inside @if: var data = ViewBag.Data as List<T> ?? new List<T>() — no @{} wrapper needed inside @if block"

# Metrics
duration: ~8min
completed: 2026-02-18
---

# Phase 11 Plan 02: Assessment View Role-Branched Layout Summary

**Assessment.cshtml restructured with role-branched Bootstrap 5 tab layout: workers see Training Records callout + Open/Upcoming tabs only; HC/Admin see Management tab (CRUD card grid with pagination from ViewBag.ManagementData) and Monitoring tab (read-only table from ViewBag.MonitorData)**

## Performance

- **Duration:** ~8 min
- **Started:** 2026-02-18T12:47:10Z
- **Completed:** 2026-02-18T12:55:00Z
- **Tasks:** 2 (implemented as one atomic file rewrite)
- **Files modified:** 1

## Accomplishments
- Worker view now shows only Open and Upcoming tabs — Completed tab `<li>` fully removed from DOM (not hidden), matching controller filter from Plan 01
- Training Records callout (`alert-info` with `Url.Action("Records", "CMP")` link) inserted above worker tabs so workers have a clear path to completed assessment history
- HC/Admin manage view replaced with Bootstrap 5 two-tab layout: Management tab iterates `ViewBag.ManagementData` with full CRUD card grid (Edit, Questions, Delete, Regen Token actions) and pagination; Monitoring tab iterates `ViewBag.MonitorData` in a read-only table (Title, Category, Status, Schedule, Assigned To, Duration)
- filterCards() JavaScript guarded with `document.getElementById('assessmentTabs')` null check — no JS errors when HC/Admin is on manage view
- All existing CRUD JS functions preserved: openTokenModal, verifyToken, startStandardAssessment, copyToken, regenerateToken, confirmDelete

## Task Commits

Tasks 1 and 2 were implemented as a single atomic rewrite:

1. **Task 1+2: Worker callout + Open/Upcoming tabs + HC/Admin Management+Monitoring tabs** - `3542f9b` (feat)

_Note: Tasks 1 and 2 modify the same file in a tightly coupled way — implementing Task 1 (worker view) without Task 2 (HC/Admin view) would leave the manage branch broken. Combined into one atomic commit._

**Plan metadata:** (docs commit follows)

## Files Created/Modified
- `Views/CMP/Assessment.cshtml` - Complete role-branched restructure: worker path (callout + 2 tabs + card grid from Model), HC/Admin path (Management tab from ViewBag.ManagementData + Monitoring tab from ViewBag.MonitorData), JS guard for filterCards

## Decisions Made
- Tasks 1 and 2 committed together — same file, tightly coupled, intermediate state would be broken
- Worker callout placed in else branch — HC/Admin in personal mode correctly also sees it (they may use Training Records too)
- Razor `@{}` inside `@if` block removed — Razor parser (RZ1010) forbids `@{}` inside an existing code block; bare variable declarations work correctly

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed Razor RZ1010 error: @{} inside @if block**
- **Found during:** Task 2 (HC/Admin view)
- **Issue:** Plan showed `@{ var managementData = ...; var monitorData = ...; }` inside the `@if (viewMode == "manage" && canManage)` block. Razor parser throws RZ1010 error for `@{}` inside an existing `@if {}` code block.
- **Fix:** Removed the `@{` and `}` wrapper — Razor already has C# context inside `@if {}`, so bare `var x = ...;` declarations work directly.
- **Files modified:** Views/CMP/Assessment.cshtml
- **Verification:** `dotnet build` — 0 errors, 0 warnings (only pre-existing nullable warnings from unrelated files)
- **Committed in:** `3542f9b` (combined task commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - Bug)
**Impact on plan:** Minor syntax fix required by Razor parser. No functional or structural scope change.

## Issues Encountered
- Initial build failed with `RZ1010: Unexpected "{" after "@" character` on the `@{ var managementData = ... }` block inside `@if`. Fixed by removing the `@{}` wrapper — Razor already has code context inside `@if {}` blocks.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Phase 11 is fully complete: Assessment() controller (Plan 01) provides role-filtered data; Assessment.cshtml (Plan 02) renders role-appropriate tab layouts
- Phase 12 (Dashboard consolidation) can proceed — no dependency on Assessment view
- Pre-implementation checklist for Phase 12 remains valid (grep for "ReportsIndex" in .cshtml files, re-declare [Authorize] on CDP Assessment Analytics content)

---
*Phase: 11-assessment-page-role-filter*
*Completed: 2026-02-18*
