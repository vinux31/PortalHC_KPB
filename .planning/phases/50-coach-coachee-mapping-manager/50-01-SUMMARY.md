---
phase: 50-coach-coachee-mapping-manager
plan: "01"
subsystem: Admin Portal - Coach-Coachee Mapping
tags: [admin, coach-coachee, mapping, scaffold, read-only, pagination, bootstrap-collapse]
dependency_graph:
  requires: []
  provides: [CoachCoacheeMapping-GET-action, CoachCoacheeMapping-view-scaffold]
  affects: [AdminController, Views/Admin]
tech_stack:
  added: []
  patterns: [ViewBag grouped data, Bootstrap collapse, dynamic LINQ grouping, pagination-over-groups]
key_files:
  created:
    - Views/Admin/CoachCoacheeMapping.cshtml
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/Index.cshtml
decisions:
  - Sequential loop counter (idx++) used for collapse HTML IDs — GUID hyphens break CSS selectors
  - AllUsers loaded once as anonymous projection to avoid N+1 queries
  - Grouping and pagination done in memory after filter — mapping table is small enough for this approach
  - EligibleCoachees excludes users who already have an active mapping (1 coach per coachee constraint)
  - Modal submit stubs use console.log with "Wired in Plan 02" comment — write endpoints deferred to Plan 02
metrics:
  duration: "~8 minutes"
  completed_date: "2026-02-27"
  tasks_completed: 2
  files_modified: 3
---

# Phase 50 Plan 01: Coach-Coachee Mapping Scaffold Summary

**One-liner:** Read-only /Admin/CoachCoacheeMapping page with Bootstrap collapse groups, section/search/showAll filters, pagination over 20 coach groups, and Assign/Edit/Deactivate modal HTML skeletons ready for Plan 02.

## What Was Built

### Task 1: AdminController GET action + Admin/Index card activation
**Commit:** `a1fbd04`

- Added `CoachCoacheeMapping` GET action to `AdminController` with:
  - Single-query user load (N+1 avoided via dictionary projection)
  - Active-only filter with `showAll` toggle
  - In-memory search (coach/coachee name, NIP) and section filter
  - GroupBy CoachId with active coachee count, sorted by coach name
  - Pagination over coach groups (20 per page)
  - ViewBag data: GroupedCoaches, CurrentPage, TotalPages, TotalCount, ShowAll, SearchTerm, SectionFilter, Sections, EligibleCoaches, EligibleCoachees, AllUsers, ProtonTracks
- Admin/Index card already activated from phase context commit (verified: `href="@Url.Action("CoachCoacheeMapping", "Admin")"`, no Segera badge, no opacity)

### Task 2: CoachCoacheeMapping.cshtml view (456 lines)
**Commit:** `518d84d`

- Breadcrumb: Kelola Data > Coach-Coachee Mapping
- Page header with total count badge, Export Excel link (stub href), Tambah Mapping button
- Filter form: section dropdown (auto-submit on change), text search, showAll checkbox (auto-submit on change), page reset logic
- Grouped-by-coach Bootstrap collapse table:
  - `table-primary` coach header row with chevron, coach name, section, active coachee count badge
  - Coachee rows: Name, NIP, Section, Position, Status badge (green/secondary), StartDate, Actions
  - Inactive rows styled with `table-light text-muted`
  - Edit button calls `openEditModal()`, Deactivate calls `confirmDeactivate()`, Reactivate calls `reactivateMapping()`
- Empty state message with icon when no groups
- Bootstrap pagination nav preserving all filter query params
- Assign Modal: coach select, section filter for coachees, scrollable coachee checklist with `data-section` attrs, ProtonTrack select, StartDate date input
- Edit Modal: read-only coachee name label, coach select, ProtonTrack select, StartDate, hidden MappingId
- Deactivate Confirmation Modal: coachee name, session info placeholder, Nonaktifkan button
- JS section: `filterCoacheesBySection()`, `openEditModal()`, `confirmDeactivate()`, `reactivateMapping()`, `submitAssign/Edit/Deactivate()` — all stubs with "Wired in Plan 02" comments
- `@Html.AntiForgeryToken()` at view top

## Deviations from Plan

None — plan executed exactly as written.

Note: Task 1 (AdminController + Index card) was already committed in a prior context session before this plan execution began (`a1fbd04`). The view was confirmed absent and created fresh in this session.

## Self-Check

**Files verified:**
- `Views/Admin/CoachCoacheeMapping.cshtml` — FOUND (456 lines)
- `Controllers/AdminController.cs` contains `public async Task<IActionResult> CoachCoacheeMapping` — FOUND
- `Views/Admin/Index.cshtml` links to `CoachCoacheeMapping` — FOUND

**Commits verified:**
- `a1fbd04` feat(50-01): AdminController CoachCoacheeMapping GET + Admin/Index card activation — FOUND
- `518d84d` feat(50-01): CoachCoacheeMapping.cshtml view with grouped table, filters, modals, pagination — FOUND

**Build:** 0 errors, 32 pre-existing warnings (all in CDPController/CMPController, unrelated to this plan)

## Self-Check: PASSED
